#!/usr/bin/env python3
"""Check lesson data shape and report size metrics as descriptive evidence.

Counts are descriptive only and never a claim of teaching quality. This tool is read-only.
No dependencies. Run from any directory. Does not access company files or the network.
"""
import argparse
import collections
import json
from pathlib import Path
import re
import statistics

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"


def nonspace(text):
    return len(re.sub(r"\s+", "", str(text)))


def section_text(section):
    parts = list(section.get("paragraphs", []))
    if section.get("note"):
        parts.append(section["note"])
    for field in ("bullets", "steps", "flow"):
        parts.extend(section.get(field, []))
    table = section.get("table", {})
    parts.extend(table.get("headers", []))
    for row in table.get("rows", []):
        parts.extend(str(cell) for cell in row)
    return "\n".join(parts)


def metrics(topic, chapter, lesson):
    sections = lesson["sections"]
    answers = ["\n".join(q["answer"]) if isinstance(q["answer"], list) else q["answer"] for q in lesson["exercises"]]
    texts = [lesson["intro"], *map(section_text, sections)]
    texts.extend(s.get("code", {}).get("text", "") for s in sections)
    return {
        "topic": topic["id"], "chapter": chapter["id"], "title": chapter["title"],
        "content_version": lesson.get("contentVersion", 1),
        "body": sum(map(nonspace, texts)),
        "paragraphs": sum(len(s.get("paragraphs", [])) for s in sections),
        "sections": len(sections), "exercises": len(answers),
        "answer_chars": sum(map(nonspace, answers)),
        "section_kinds": dict(collections.Counter(s.get("kind", "untyped") for s in sections)),
        "exercise_levels": dict(collections.Counter(q.get("level", "untyped") for q in lesson["exercises"])),
        "sources": len(lesson.get("sources", [])), "resources": len(lesson.get("resources", [])),
    }


def validate(row, lesson):
    issues = []
    if row["content_version"] < 2:
        issues.append("仍是第一版")
    for field in ("intro", "prerequisites", "goals", "sections", "exercises", "recap", "sources"):
        if field not in lesson:
            issues.append(f"缺少{field}")
    if not isinstance(lesson.get("intro"), str) or not lesson.get("intro", "").strip():
        issues.append("intro不是非空文字")
    for field in ("prerequisites", "goals", "sections", "exercises", "recap", "sources"):
        if not isinstance(lesson.get(field), list):
            issues.append(f"{field}不是陣列")
    if "studyTime" in lesson:
        issues.append("仍含未校準的studyTime估計")
    for section in lesson.get("sections", []) if isinstance(lesson.get("sections"), list) else []:
        if not section.get("heading") or not any(section.get(key) for key in ("paragraphs", "image", "code", "table", "steps", "bullets", "flow", "note")):
            issues.append("段落標題或內容缺漏")
    for exercise in lesson.get("exercises", []) if isinstance(lesson.get("exercises"), list) else []:
        if not isinstance(exercise.get("question"), str) or not exercise["question"].strip():
            issues.append("練習缺少題目")
        answer = exercise.get("answer")
        if not (isinstance(answer, str) and answer.strip()) and not (isinstance(answer, list) and answer and all(isinstance(p, str) and p.strip() for p in answer)):
            issues.append("練習缺少可讀解答")
    for source in lesson.get("sources", []) if isinstance(lesson.get("sources"), list) else []:
        if not source.get("label") or not isinstance(source.get("url"), str) or not source["url"].startswith("https://"):
            issues.append("來源缺少標籤或HTTPS連結")
    return issues


def save(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, help="save complete measurement evidence")
    args = parser.parse_args()
    catalog_path = DOCS / "content/catalog.json"
    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    rows, failures = [], []
    for topic in catalog["topics"]:
        for chapter in topic["chapters"]:
            if chapter["status"] != "ready":
                continue
            path = DOCS / f"content/lessons/{topic['id']}--{chapter['id']}.json"
            lesson = json.loads(path.read_text(encoding="utf-8"))
            row = metrics(topic, chapter, lesson)
            row["structural_issues"] = validate(row, lesson)
            rows.append(row)
            if row["structural_issues"]:
                failures.append(f"{topic['id']}/{chapter['id']}: {', '.join(row['structural_issues'])}")
    if args.output:
        save(args.output, rows)
    print(f"{len(rows)}篇；舊版{sum(r['content_version'] == 2 for r in rows)}篇，新版{sum(r['content_version'] >= 3 for r in rows)}篇；正文總量{sum(r['body'] for r in rows):,}，中位{statistics.median(r['body'] for r in rows):,.0f}字元")
    print(f"結構警戒{len(failures)}篇。數量通過不代表內容已經人工審查。")
    for failure in failures:
        print(failure)


if __name__ == "__main__":
    main()
