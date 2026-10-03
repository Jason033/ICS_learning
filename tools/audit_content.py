#!/usr/bin/env python3
"""Measure curriculum consistently; --finalize updates estimates/catalog after all authors finish.

Counts are an early warning for thin lessons, never a claim of teaching quality.
No dependencies. Run from any directory. Does not access company files or the network.
"""
import argparse
import collections
import json
import math
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


def reading_estimate(lesson):
    prose = "\n".join([lesson["intro"], *map(section_text, lesson["sections"])])
    chinese = len(re.findall(r"[\u3400-\u9fff]", prose))
    words = len(re.findall(r"[A-Za-z0-9]+", prose))
    code_lines = sum(sum(bool(line.strip()) for line in s.get("code", {}).get("text", "").splitlines()) for s in lesson["sections"])
    # ASCII words count as two reading units; code gets separate line-by-line time.
    units = chinese + 2 * words
    return [max(1, math.ceil(units / 300 + code_lines / 12)),
            max(2, math.ceil(units / 180 + code_lines / 6))]


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
        "study_time": lesson.get("studyTime"), "reading_estimate": reading_estimate(lesson),
    }


def validate(row, lesson):
    issues = []
    if row["content_version"] != 2:
        issues.append("仍是第一版")
    if row["body"] < 3500:
        issues.append("正文低於3500字元警戒值")
    kinds = row["section_kinds"]
    for kind, minimum in [("worked-example", 2), ("lab", 1), ("diagnostic-case", 1)]:
        if kinds.get(kind, 0) < minimum:
            issues.append(f"缺少{kind}結構")
    if row["exercises"] < 8:
        issues.append("練習不足8題")
    if not {"foundation", "application", "diagnosis"}.issubset(row["exercise_levels"]):
        issues.append("缺少分層練習")
    if row["sources"] < 3:
        issues.append("來源不足3筆")
    if not lesson.get("prerequisites"):
        issues.append("缺少前置說明")
    for name in ("reading", "practice", "exercises"):
        value = lesson.get("studyTime", {}).get(name)
        if not isinstance(value, list) or len(value) != 2 or not 0 < value[0] <= value[1]:
            issues.append(f"{name}時間格式不正確")
    return issues


def save(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--finalize", action="store_true", help="all 83 must pass structure checks before any write")
    parser.add_argument("--output", type=Path, help="save complete measurement evidence")
    args = parser.parse_args()
    catalog_path = DOCS / "content/catalog.json"
    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    rows, pending, failures = [], [], []
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
            pending.append((path, chapter, lesson, row))
    if args.finalize and failures:
        raise SystemExit("未修改任何檔案；先完成下列結構項目：\n" + "\n".join(failures))
    if args.finalize:
        for path, chapter, lesson, row in pending:
            study = lesson["studyTime"]
            study["reading"] = row["reading_estimate"]
            study["basis"] = (
                "閱讀為未經個人計時校準的估計：中文正文與英文詞換算閱讀量，以每分鐘180–300單位，"
                "另加逐行追碼每分鐘6–12行；不含練習答案。操作與作答依本章任務估計。 "
                + re.sub(r"^閱讀為未經個人計時校準的估計：.*?依本章任務估計。\s*", "", study.get("basis", ""))
            )
            chapter.update(contentVersion=2, contentType="full", studyTime=study)
            chapter.pop("minutes", None)
            lesson.pop("plan", None)
            save(path, lesson)
            row["study_time"] = study
        for topic in catalog["topics"]:
            for chapter in topic["chapters"]:
                if chapter["status"] == "planned":
                    chapter.pop("minutes", None)
        save(catalog_path, catalog)
    if args.output:
        save(args.output, rows)
    print(f"{len(rows)}篇；第二版{sum(r['content_version'] == 2 for r in rows)}篇；正文總量{sum(r['body'] for r in rows):,}，中位{statistics.median(r['body'] for r in rows):,.0f}字元")
    print(f"結構警戒{len(failures)}篇。數量通過不代表內容已經人工審查。")
    for failure in failures:
        print(failure)


if __name__ == "__main__":
    main()
