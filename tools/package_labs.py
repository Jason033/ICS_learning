#!/usr/bin/env python3
"""Build deterministic source-only ZIPs; link them from corresponding lessons.

Call after lesson authors finish. Includes no bin/obj, executable, device data or credentials.
"""
import argparse
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
LABS = ROOT / "docs/assets/labs"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("topics", nargs="*", help="topic IDs; default all maintenance-lab directories")
    args = parser.parse_args()
    for folder in sorted(LABS.glob("*-maintenance-lab")):
        topic = folder.name.removesuffix("-maintenance-lab")
        if args.topics and topic not in args.topics:
            continue
        files = [p for p in folder.rglob("*") if p.is_file()
                 and not {"bin", "obj", "__pycache__"}.intersection(p.relative_to(folder).parts)
                 and p.suffix.lower() in {".cs", ".csproj", ".config", ".md", ".txt", ".json", ".csv"}]
        if not any(p.suffix == ".csproj" for p in files):
            raise SystemExit(f"missing project: {folder}")
        zip_path = LABS / f"{folder.name}.zip"
        with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for file in sorted(files):
                entry = zipfile.ZipInfo(file.relative_to(LABS).as_posix(), date_time=(2026, 10, 3, 0, 0, 0))
                entry.compress_type = zipfile.ZIP_DEFLATED
                entry.external_attr = 0o644 << 16
                archive.writestr(entry, file.read_bytes())
        for lesson_path in (ROOT / "docs/content/lessons").glob(f"{topic}--*.json"):
            lesson = json.loads(lesson_path.read_text(encoding="utf-8"))
            if lesson.get("contentVersion") != 2:
                continue
            resources = lesson.setdefault("resources", [])
            relative = f"./assets/labs/{folder.name}.zip"
            if not any(resource["path"] == relative for resource in resources):
                resources.insert(0, {"path": relative, "label": "下載本主題完整 C# 教學專案",
                    "description": ".NET 10 原始碼與操作說明；解壓後依本章步驟追讀。自行建立的模型，與公司專案分開。"})
                lesson_path.write_text(json.dumps(lesson, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{zip_path.name}: {len(files)} source files")


if __name__ == "__main__":
    main()
