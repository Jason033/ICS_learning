#!/usr/bin/env python3
"""Optional real-browser verification; Playwright is a development tool, not a site dependency.

Start a local HTTP server, then: python tests/browser.py http://127.0.0.1:8000/
--screenshots points to a temporary preview directory, never the public lesson assets.
"""
import argparse
import json
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("url")
    parser.add_argument("--screenshots", type=Path)
    args = parser.parse_args()
    base = args.url.rstrip("/") + "/"
    catalog = json.loads((ROOT / "docs/content/catalog.json").read_text(encoding="utf-8"))
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 1000})
        errors = []
        page.on("pageerror", lambda error: errors.append(str(error)))
        page.goto(base)
        page.wait_for_selector(".topic-card")
        assert page.locator(".topic-card").count() == len(catalog["topics"])
        page.locator("#topic-search").fill("RS-232")
        assert page.locator(".topic-card:visible").count() == 1
        page.locator("#topic-search").fill("no-such-topic-987")
        assert page.locator("#empty-search").is_visible()
        page.locator("#topic-search").fill("")

        def lesson(topic, chapter):
            page.goto(f"{base}#/lesson/{topic}/{chapter}")
            expected_chapter = next(c for t in catalog["topics"] if t["id"] == topic
                                    for c in t["chapters"] if c["id"] == chapter)
            # A hash change can leave the previous lesson visible while fetch runs.
            # Wait for this chapter, rather than accepting any old section in the DOM.
            page.wait_for_function("title => document.querySelector('.lesson-header h1')?.textContent === title",
                                   arg=expected_chapter["title"])
            expected_lesson = json.loads((ROOT / f"docs/content/lessons/{topic}--{chapter}.json").read_text())
            assert page.locator(".exercise-card").count() == len(expected_lesson["exercises"])
            assert page.locator("a[download][href='#']").count() == 0
            # Force below-the-fold lazy diagrams to load, then verify SVG decoding
            # and that the large-image link opens the same original asset.
            page.evaluate("""async () => {
                await Promise.all([...document.querySelectorAll('.lesson-image')].map(async figure => {
                    const img = figure.querySelector('img');
                    img.loading = 'eager';
                    await img.decode();
                    if (!img.naturalWidth || figure.querySelector('.diagram-link')?.href !== img.src)
                        throw new Error('diagram decoding or original-image link failed');
                }));
            }""")
            assert not page.evaluate("document.documentElement.scrollWidth > innerWidth"), f"page overflow: {topic}/{chapter}"

        pilot = next(t for t in catalog["topics"] if t["id"] == "networking")
        pilot = [c for c in pilot["chapters"] if c.get("contentVersion", 0) >= 3]
        assert page.locator(".learning-preview li a").count() == len(pilot)
        for i, chapter in enumerate(pilot):
            page.locator(f".learning-preview a[href='#/lesson/networking/{chapter['id']}']").click()
            page.wait_for_function("title => document.querySelector('.lesson-header h1')?.textContent === title", arg=chapter["title"])
            assert page.locator(".legacy-notice").count() == 0
            assert page.locator(".lesson-plan").count() == 0
            check = page.locator(".concept-check details").first
            check.locator("summary").click()
            assert check.get_attribute("open") is not None
            assert check.locator("p").count() >= 1
            nav = page.locator(".lesson-next")
            if i + 1 < len(pilot):
                assert nav.locator("a").last.get_attribute("href") == f"#/lesson/networking/{pilot[i + 1]['id']}"
            else:
                assert "本組新版試讀到此" in nav.inner_text()
                assert nav.locator("a").last.get_attribute("href") == "#/topic/networking"
            assert page.locator(".lesson-aside a[href='#/lesson/networking/one-conversation']").count() == 0
            page.goto(base)
            page.wait_for_selector(".learning-preview")
        lesson("networking", "one-conversation")
        assert page.locator(".legacy-notice").is_visible()
        page.locator("#complete-button").click()
        assert page.locator("#complete-button").get_attribute("aria-pressed") == "true"
        page.reload()
        page.wait_for_selector("#complete-button")
        assert page.locator("#complete-button").get_attribute("aria-pressed") == "true"
        page.locator("#complete-button").click()
        first_answer = page.locator(".exercise-card details").first
        first_answer.locator("summary").click()
        assert first_answer.get_attribute("open") is not None
        assert first_answer.locator("p").count() >= 2
        if args.screenshots:
            args.screenshots.mkdir(parents=True, exist_ok=True)
            page.screenshot(path=str(args.screenshots / "desktop.png"))

        page.set_viewport_size({"width": 390, "height": 844})
        count = 0
        for topic in catalog["topics"]:
            for chapter in topic["chapters"]:
                if chapter["status"] != "ready":
                    continue
                lesson(topic["id"], chapter["id"])
                assert page.locator(".mobile-lesson-toc").is_visible()
                count += 1
        lesson("reliability", "concurrency")
        page.locator(".mobile-lesson-toc summary").click()
        page.locator(".mobile-lesson-toc a[href='#practice']").click()
        page.wait_for_function("Math.abs(document.getElementById('practice').getBoundingClientRect().top) < 160", timeout=5000)
        assert page.locator("#practice").evaluate("e => e.getBoundingClientRect().top") < 844
        assert "#/lesson/reliability/concurrency" in page.url
        if args.screenshots:
            page.screenshot(path=str(args.screenshots / "mobile.png"))
        unavailable = next((f"{t['id']}/{c['id']}" for t in catalog["topics"]
                            for c in t["chapters"] if c["status"] == "planned"),
                           "systems/no-such-chapter")
        page.goto(base + "#/lesson/" + unavailable)
        page.wait_for_selector(".error-page")
        assert "尚未開放" in page.locator("#main").inner_text()
        assert not errors, errors
        print(json.dumps({"browser": "Chromium", "mobile_lessons": count,
                          "widths": [1440, 390], "javascript_errors": errors,
                          "checks": ["search", "progress persistence", "answer disclosure", "mobile navigation", "overflow", "diagram decoding and original-image links", "unavailable content", "teaching preview entry and edition navigation", "inline understanding checks"]},
                         ensure_ascii=False, indent=2))
        browser.close()


if __name__ == "__main__":
    main()
