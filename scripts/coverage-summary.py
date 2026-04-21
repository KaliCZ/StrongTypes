#!/usr/bin/env python3
"""Render a project/folder-grouped Markdown coverage summary from a merged Cobertura XML.

Cobertura emits one ``<class>`` per constructed generic type, and each one
carries a full copy of the source file's ``<line>`` entries. Summing naively
multiplies physical lines by the number of instantiations (e.g. a 200-line
file materialized as 700 closed generics becomes 140,000 "lines"). We dedupe
by (filename, line number) so each physical line is counted once, and take
the max of branch coverage across instantiations so a branch exercised by any
instantiation is credited at the source level.
"""
from __future__ import annotations

import argparse
import re
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import dataclass


ROOT_FOLDER = "(root)"
GENERATED_FOLDER = "generated"


def repo_relative(path: str) -> str:
    path = path.replace("\\", "/")
    marker = "/src/"
    idx = path.find(marker)
    if idx >= 0:
        return path[idx + 1:]
    return path.lstrip("/")


_ARITY_RE = re.compile(r"`(\d+)")


def _format_arity(match: re.Match[str]) -> str:
    n = int(match.group(1))
    params = "T" if n == 1 else ",".join(f"T{i}" for i in range(1, n + 1))
    return f"<{params}>"


@dataclass
class Row:
    project: str
    folder: str
    filename: str   # display basename (arity-normalized for generated code)
    rel: str        # repo-relative path used for change-set matching
    lines_cov: int
    lines_total: int
    branches_cov: int
    branches_total: int


def classify(rel: str) -> tuple[str, str, str]:
    """Split a repo-relative path into (project, folder, display filename).

    Normal files live under ``src/<project>/<folder>/<file>``. Source-generator
    output lives under ``src/<project>/obj/<config>/<tfm>/<generator>/<file>.g.cs``;
    we bucket those into a ``generated`` folder under the same project with a
    compacted filename, since the long obj-path prefix is noise in a table.
    """
    parts = rel.split("/")
    if len(parts) < 2 or parts[0] != "src":
        return ("(other)", ROOT_FOLDER, rel)

    project = parts[1]
    remaining = parts[2:]

    if len(remaining) >= 2 and remaining[0] == "obj" and rel.endswith(".g.cs"):
        basename = remaining[-1]
        if basename.startswith("StrongTypes."):
            basename = basename[len("StrongTypes."):]
        basename = _ARITY_RE.sub(_format_arity, basename)
        return (project, GENERATED_FOLDER, basename)

    if len(remaining) == 1:
        return (project, ROOT_FOLDER, remaining[0])

    folder = "/".join(remaining[:-1])
    filename = remaining[-1]
    return (project, folder, filename)


def parse_cobertura(path: str) -> dict[str, dict[int, list]]:
    tree = ET.parse(path)
    root = tree.getroot()
    # entry schema: [any_hit, branches_covered_max, branches_total_max]
    per_file: dict[str, dict[int, list]] = defaultdict(
        lambda: defaultdict(lambda: [False, 0, 0])
    )

    for cls in root.iter("class"):
        filename = cls.get("filename")
        if not filename:
            continue
        file_lines = per_file[filename]
        for line in cls.iter("line"):
            num = int(line.get("number", "0"))
            hits = int(line.get("hits", "0"))
            entry = file_lines[num]
            if hits > 0:
                entry[0] = True
            if line.get("branch") == "true":
                cond = line.get("condition-coverage", "")
                if "(" in cond and "/" in cond:
                    frac = cond.split("(", 1)[1].rstrip(")")
                    covered_s, total_s = frac.split("/")
                    covered, total = int(covered_s), int(total_s)
                    if covered > entry[1]:
                        entry[1] = covered
                    if total > entry[2]:
                        entry[2] = total
    return per_file


def pct(covered: int, total: int) -> str:
    return f"{(covered / total * 100):.1f}%" if total else "n/a"


def load_changed_files(path: str | None) -> set[str] | None:
    if not path:
        return None
    try:
        with open(path, encoding="utf-8") as f:
            return {line.strip() for line in f if line.strip()}
    except FileNotFoundError:
        return None


def build_rows(per_file: dict[str, dict[int, list]]) -> list[Row]:
    rows: list[Row] = []
    for filename, lines in per_file.items():
        lines_total = len(lines)
        lines_cov = sum(1 for e in lines.values() if e[0])
        branches_cov = sum(e[1] for e in lines.values())
        branches_total = sum(e[2] for e in lines.values())
        rel = repo_relative(filename)
        project, folder, display = classify(rel)
        rows.append(Row(
            project=project,
            folder=folder,
            filename=display,
            rel=rel,
            lines_cov=lines_cov,
            lines_total=lines_total,
            branches_cov=branches_cov,
            branches_total=branches_total,
        ))
    return rows


def _row_cells(r: Row, path_override: str | None = None) -> str:
    label = path_override if path_override is not None else r.filename
    return (
        f"| `{label}` | {r.lines_cov} / {r.lines_total} ({pct(r.lines_cov, r.lines_total)}) | "
        f"{r.branches_cov} / {r.branches_total} ({pct(r.branches_cov, r.branches_total)}) |"
    )


def render(cobertura_path: str, output_path: str, changed_files: set[str] | None) -> None:
    rows = build_rows(parse_cobertura(cobertura_path))

    total_lc = sum(r.lines_cov for r in rows)
    total_lt = sum(r.lines_total for r in rows)
    total_bc = sum(r.branches_cov for r in rows)
    total_bt = sum(r.branches_total for r in rows)

    out: list[str] = []
    out.append("## Coverage")
    out.append("")
    out.append(
        f"**Lines:** {total_lc} / {total_lt} "
        f"({pct(total_lc, total_lt)}) &nbsp;&nbsp; "
        f"**Branches:** {total_bc} / {total_bt} "
        f"({pct(total_bc, total_bt)})"
    )
    out.append("")

    if changed_files is not None:
        changed_rows = sorted(
            (r for r in rows if r.rel in changed_files),
            key=lambda r: (r.project, r.folder, r.filename.lower()),
        )
        out.append("### Files changed in this PR")
        out.append("")
        if changed_rows:
            out.append("| File | Lines | Branches |")
            out.append("|---|---:|---:|")
            for r in changed_rows:
                # Full project-qualified path helps readers locate the file
                # when the table is flat across projects.
                label = f"{r.project}/{r.folder}/{r.filename}" if r.folder != ROOT_FOLDER else f"{r.project}/{r.filename}"
                out.append(_row_cells(r, path_override=label))
        else:
            out.append("_No coverage-instrumented files changed in this PR._")
        out.append("")

    projects: dict[str, list[Row]] = defaultdict(list)
    for r in rows:
        projects[r.project].append(r)

    for project in sorted(projects):
        proj_rows = projects[project]
        p_lc = sum(r.lines_cov for r in proj_rows)
        p_lt = sum(r.lines_total for r in proj_rows)
        p_bc = sum(r.branches_cov for r in proj_rows)
        p_bt = sum(r.branches_total for r in proj_rows)
        out.append(
            f"<details><summary><b>{project}</b> — "
            f"lines {pct(p_lc, p_lt)} ({p_lc}/{p_lt}), "
            f"branches {pct(p_bc, p_bt)} ({p_bc}/{p_bt})</summary>"
        )
        out.append("")

        folders: dict[str, list[Row]] = defaultdict(list)
        for r in proj_rows:
            folders[r.folder].append(r)

        for folder in sorted(folders):
            f_rows = sorted(folders[folder], key=lambda r: r.filename.lower())
            f_lc = sum(r.lines_cov for r in f_rows)
            f_lt = sum(r.lines_total for r in f_rows)
            f_bc = sum(r.branches_cov for r in f_rows)
            f_bt = sum(r.branches_total for r in f_rows)
            out.append(
                f"**{folder}** — "
                f"lines {pct(f_lc, f_lt)}, branches {pct(f_bc, f_bt)}"
            )
            out.append("")
            out.append("| File | Lines | Branches |")
            out.append("|---|---:|---:|")
            for r in f_rows:
                out.append(_row_cells(r))
            out.append("")

        out.append("</details>")
        out.append("")

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(out))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("cobertura")
    parser.add_argument("output")
    parser.add_argument(
        "--changed-files",
        help="Path to a newline-delimited list of repo-relative paths changed in this PR.",
    )
    args = parser.parse_args()
    render(args.cobertura, args.output, load_changed_files(args.changed_files))


if __name__ == "__main__":
    main()
