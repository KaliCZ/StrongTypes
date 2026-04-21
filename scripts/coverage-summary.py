#!/usr/bin/env python3
"""Render a file-grouped Markdown coverage summary from a merged Cobertura XML.

ReportGenerator's Markdown outputs group by class, which gets noisy for
extension-method-heavy code: compiler-generated closure types surface as
separate rows like `MaybeExtensions<A, B>`, `MaybeExtensions<A, R>`, …, all
coming from the same .cs file. Cobertura's `<class filename="…">` attribute
lets us collapse those back to one row per source file.
"""
from __future__ import annotations

import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict


LOW_COVERAGE_THRESHOLD = 50.0


def repo_relative(path: str) -> str:
    path = path.replace("\\", "/")
    marker = "/src/"
    idx = path.find(marker)
    if idx >= 0:
        return path[idx + 1 :]
    return path.lstrip("/")


def render(cobertura_path: str, output_path: str) -> None:
    tree = ET.parse(cobertura_path)
    root = tree.getroot()

    # filename -> [lines_covered, lines_total, branches_covered, branches_total]
    by_file: dict[str, list[int]] = defaultdict(lambda: [0, 0, 0, 0])

    for cls in root.iter("class"):
        filename = cls.get("filename")
        if not filename:
            continue
        bucket = by_file[filename]
        for line in cls.iter("line"):
            hits = int(line.get("hits", "0"))
            bucket[1] += 1
            if hits > 0:
                bucket[0] += 1
            if line.get("branch") == "true":
                cond = line.get("condition-coverage", "")
                # condition-coverage looks like "50% (1/2)"
                if "(" in cond and "/" in cond:
                    frac = cond.split("(", 1)[1].rstrip(")")
                    covered, total = frac.split("/")
                    bucket[2] += int(covered)
                    bucket[3] += int(total)

    rows = sorted(
        (
            (repo_relative(fname), cov, tot, bcov, btot)
            for fname, (cov, tot, bcov, btot) in by_file.items()
        ),
        key=lambda r: r[0],
    )

    total_cov = sum(r[1] for r in rows)
    total_lines = sum(r[2] for r in rows)
    total_bcov = sum(r[3] for r in rows)
    total_branches = sum(r[4] for r in rows)

    def pct(covered: int, total: int) -> str:
        return f"{(covered / total * 100):.1f}%" if total else "n/a"

    low = [r for r in rows if r[2] > 0 and (r[1] / r[2] * 100) < LOW_COVERAGE_THRESHOLD]

    lines: list[str] = []
    lines.append("## Coverage")
    lines.append("")
    lines.append(
        f"**Lines:** {total_cov} / {total_lines} "
        f"({pct(total_cov, total_lines)}) &nbsp;&nbsp; "
        f"**Branches:** {total_bcov} / {total_branches} "
        f"({pct(total_bcov, total_branches)})"
    )
    lines.append("")

    if low:
        lines.append(f"### Files below {LOW_COVERAGE_THRESHOLD:.0f}% line coverage")
        lines.append("")
        lines.append("Consider adding tests — these files drag the number down.")
        lines.append("")
        lines.append("| File | Lines | Branches |")
        lines.append("|---|---:|---:|")
        for fname, cov, tot, bcov, btot in low:
            lines.append(
                f"| `{fname}` | {cov} / {tot} ({pct(cov, tot)}) | "
                f"{bcov} / {btot} ({pct(bcov, btot)}) |"
            )
        lines.append("")
    else:
        lines.append(
            f"No files below {LOW_COVERAGE_THRESHOLD:.0f}% line coverage."
        )
        lines.append("")

    lines.append("<details><summary>All files</summary>")
    lines.append("")
    lines.append("| File | Lines | Branches |")
    lines.append("|---|---:|---:|")
    for fname, cov, tot, bcov, btot in rows:
        lines.append(
            f"| `{fname}` | {cov} / {tot} ({pct(cov, tot)}) | "
            f"{bcov} / {btot} ({pct(bcov, btot)}) |"
        )
    lines.append("")
    lines.append("</details>")
    lines.append("")

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(
            f"usage: {os.path.basename(sys.argv[0])} <cobertura.xml> <output.md>",
            file=sys.stderr,
        )
        sys.exit(2)
    render(sys.argv[1], sys.argv[2])
