#!/usr/bin/env python3
"""Render a file-grouped Markdown coverage summary from a merged Cobertura XML.

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


def display_path(path: str) -> str:
    """Compact noisy paths for readability in the summary table.

    Source-generator output lives under ``src/<proj>/obj/<config>/<tfm>/<generator>/…``.
    Those long prefixes push the table too wide, so collapse them to
    ``generated/`` and translate ``Foo`1`` → ``Foo<T>`` so the filename
    doesn't contain a stray backtick that breaks out of the markdown code span.
    """
    rel = repo_relative(path)
    if "/obj/" in rel and rel.endswith(".g.cs"):
        basename = rel.rsplit("/", 1)[-1]
        if basename.startswith("StrongTypes."):
            basename = basename[len("StrongTypes."):]
        basename = _ARITY_RE.sub(_format_arity, basename)
        return f"generated/{basename}"
    return rel


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
            entries = {line.strip() for line in f if line.strip()}
    except FileNotFoundError:
        return None
    return entries


def render(cobertura_path: str, output_path: str, changed_files: set[str] | None) -> None:
    per_file = parse_cobertura(cobertura_path)

    rows = []
    for filename, lines in per_file.items():
        lines_total = len(lines)
        lines_cov = sum(1 for e in lines.values() if e[0])
        branches_cov = sum(e[1] for e in lines.values())
        branches_total = sum(e[2] for e in lines.values())
        rows.append((
            display_path(filename),
            repo_relative(filename),
            lines_cov,
            lines_total,
            branches_cov,
            branches_total,
        ))
    rows.sort(key=lambda r: r[0])

    total_cov = sum(r[2] for r in rows)
    total_lines = sum(r[3] for r in rows)
    total_bcov = sum(r[4] for r in rows)
    total_branches = sum(r[5] for r in rows)

    out: list[str] = []
    out.append("## Coverage")
    out.append("")
    out.append(
        f"**Lines:** {total_cov} / {total_lines} "
        f"({pct(total_cov, total_lines)}) &nbsp;&nbsp; "
        f"**Branches:** {total_bcov} / {total_branches} "
        f"({pct(total_bcov, total_branches)})"
    )
    out.append("")

    if changed_files is not None:
        changed_rows = [r for r in rows if r[1] in changed_files]
        out.append("### Files changed in this PR")
        out.append("")
        if changed_rows:
            out.append("| File | Lines | Branches |")
            out.append("|---|---:|---:|")
            for disp, _rel, cov, tot, bcov, btot in changed_rows:
                out.append(
                    f"| `{disp}` | {cov} / {tot} ({pct(cov, tot)}) | "
                    f"{bcov} / {btot} ({pct(bcov, btot)}) |"
                )
        else:
            out.append("_No coverage-instrumented files changed in this PR._")
        out.append("")

    out.append("<details><summary>All files</summary>")
    out.append("")
    out.append("| File | Lines | Branches |")
    out.append("|---|---:|---:|")
    for disp, _rel, cov, tot, bcov, btot in rows:
        out.append(
            f"| `{disp}` | {cov} / {tot} ({pct(cov, tot)}) | "
            f"{bcov} / {btot} ({pct(bcov, btot)}) |"
        )
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
