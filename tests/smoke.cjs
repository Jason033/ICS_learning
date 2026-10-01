const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '..');
const docs = path.join(root, 'docs');
const catalog = JSON.parse(fs.readFileSync(path.join(docs, 'content/catalog.json'), 'utf8'));
const ids = new Set();
let readyCount = 0;
for (const topic of catalog.topics) {
  assert.match(topic.id, /^[a-z0-9-]+$/);
  assert(!ids.has(topic.id), `重複主題 ID：${topic.id}`);
  ids.add(topic.id);
  const chapterIds = new Set();
  for (const chapter of topic.chapters) {
    assert.match(chapter.id, /^[a-z0-9-]+$/);
    assert(!chapterIds.has(chapter.id), `重複章節 ID：${topic.id}/${chapter.id}`);
    chapterIds.add(chapter.id);
    const file = path.join(docs, 'content/lessons', `${topic.id}--${chapter.id}.json`);
    if (chapter.status === 'ready') {
      readyCount += 1;
      assert(fs.existsSync(file), `缺少可閱讀教材：${file}`);
      const lesson = JSON.parse(fs.readFileSync(file, 'utf8'));
      for (const field of ['intro', 'goals', 'sections', 'exercises', 'recap']) assert(lesson[field]?.length, `${file} 缺少 ${field}`);
      for (const source of lesson.sources || []) assert.match(source.url, /^https:\/\//);
      for (const resource of lesson.resources || []) {
        assert.match(resource.path, /^\.\/assets\/labs\/[a-z0-9-]+\.pcap$/);
        assert(fs.existsSync(path.join(docs, resource.path.slice(2))), `${file} 缺少練習檔 ${resource.path}`);
      }
      for (const section of lesson.sections) {
        if (!section.image) continue;
        assert.match(section.image.src, /^\.\/assets\/diagrams\/[a-z0-9-]+\.svg$/);
        assert(fs.existsSync(path.join(docs, section.image.src.slice(2))), `${file} 缺少示意圖`);
      }
      for (const section of lesson.sections) {
        if (!section.table) continue;
        assert(section.table.headers.length > 1, `${file} 表格缺少欄位`);
        for (const row of section.table.rows) assert.equal(row.length, section.table.headers.length, `${file} 表格欄數不一致`);
      }
    } else assert.equal(chapter.status, 'planned');
  }
}
assert.equal(catalog.topics.length, 16);
assert.equal(readyCount, 22);
assert.equal(catalog.topics[0].chapters.length, 10);
for (const chapter of catalog.topics[0].chapters) {
  assert.equal(chapter.contentType, 'full');
  const lesson = JSON.parse(fs.readFileSync(path.join(docs, 'content/lessons', `networking--${chapter.id}.json`), 'utf8'));
  assert.equal(lesson.plan.reduce((sum, part) => sum + part.minutes, 0), chapter.minutes);
  assert(lesson.sections.length >= 4, `${chapter.id} 教學段落不足`);
  assert(lesson.exercises.length >= 5, `${chapter.id} 練習不足`);
}

assert.equal(catalog.topics[1].chapters.length, 8);
for (const chapter of catalog.topics[1].chapters) {
  assert.equal(chapter.contentType, 'full');
  const lesson = JSON.parse(fs.readFileSync(path.join(docs, 'content/lessons', `wireshark--${chapter.id}.json`), 'utf8'));
  assert.equal(lesson.plan.reduce((sum, part) => sum + part.minutes, 0), chapter.minutes);
  assert(lesson.sections.length >= 5, `${chapter.id} 教學段落不足`);
  assert(lesson.exercises.length >= 5, `${chapter.id} 練習不足`);
  assert(lesson.resources.length >= 1, `${chapter.id} 缺少實作 PCAP`);
}

const elements = new Map();
function element(selector) {
  if (!elements.has(selector)) {
    const attributes = {};
    const listeners = {};
    elements.set(selector, {
      innerHTML: '', textContent: '', value: '', hidden: false,
      attributes, listeners,
      classList: { toggle() {} },
      setAttribute(key, value) { attributes[key] = value; },
      removeAttribute(key) { delete attributes[key]; },
      addEventListener(event, handler) { listeners[event] = handler; },
      scrollIntoView() {}
    });
  }
  return elements.get(selector);
}
const storage = new Map();
const context = vm.createContext({
  document: { querySelector: element, querySelectorAll: () => [] },
  location: { hash: '#/' },
  window: { addEventListener() {}, scrollTo() {} },
  localStorage: { getItem: (key) => storage.get(key) ?? null, setItem: (key, value) => storage.set(key, value) },
  fetch: async (relative) => {
    const file = path.join(docs, relative.replace(/^\.\//, ''));
    return fs.existsSync(file) ? { ok: true, json: async () => JSON.parse(fs.readFileSync(file, 'utf8')) } : { ok: false, status: 404 };
  },
  URL, console, Set
});
vm.runInContext(fs.readFileSync(path.join(docs, 'app.js'), 'utf8'), context);

(async () => {
  await new Promise(setImmediate);
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /選一個主題，開始學/);
  assert.match(element('#main').innerHTML, /Ozeki API/);
  assert.match(element('#main').innerHTML, /規劃中/);
  assert(element('#explore-topics').listeners.click, '首頁探索按鈕未綁定');

  context.location.hash = '#/topic/wireshark';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /第一次抓包/);
  assert.match(element('#main').innerHTML, /#\/lesson\/wireshark\/first-capture/);
  assert.match(element('#main').innerHTML, /#\/lesson\/wireshark\/filters/);
  assert.match(element('#main').innerHTML, /完整教材/);

  context.location.hash = '#/lesson/wireshark/first-capture';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /wireshark-first-capture\.pcap/);
  assert.match(element('#main').innerHTML, /wireshark-three-panes\.svg/);
  assert.match(element('#main').innerHTML, /練習 06/);
  context.location.hash = '#/lesson/wireshark/evidence-and-logs';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /練習 07/);

  const first = catalog.topics[0].chapters[0];
  const firstLesson = JSON.parse(fs.readFileSync(path.join(docs, 'content/lessons/networking--one-conversation.json'), 'utf8'));
  assert.equal(first.contentType, 'full');
  assert.equal(firstLesson.plan.reduce((sum, part) => sum + part.minutes, 0), first.minutes);
  assert(firstLesson.sections.length >= 10);
  assert(firstLesson.exercises.length >= 8);
  context.location.hash = '#/lesson/networking/one-conversation';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /建議學習節奏/);
  assert.match(element('#main').innerHTML, /<table class="lesson-table">/);
  assert.match(element('#main').innerHTML, /模擬封包摘要/);
  assert.match(element('#main').innerHTML, /練習 09/);
  assert.match(element('#main').innerHTML, /下一篇/);

  context.location.hash = '#/lesson/networking/network-troubleshooting';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /案例三：TCP 已建立/);
  assert.match(element('#main').innerHTML, /練習 07/);

  context.location.hash = '#/lesson/ozeki/read-a-call';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /Softphone/);
  assert.match(element('#main').innerHTML, /查看解答與判斷過程/);
  const button = element('#complete-button');
  assert(button.listeners.click, '完成按鈕未綁定');
  button.listeners.click({ currentTarget: button });
  assert.deepEqual(JSON.parse(storage.get('learning-site-completed-v1')), ['ozeki/read-a-call']);

  context.location.hash = '#/topic/ozeki';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /<strong>1<span> \/ 1<\/span><\/strong>/);
  assert.match(element('#main').innerHTML, /✓ 已完成/);

  context.location.hash = '#/lesson/ozeki/registration';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /這篇教材尚未開放/);

  console.log(`通過：${catalog.topics.length} 個獨立主題、${readyCount} 篇教材，以及首頁／主題／完整教材／進度／規劃中頁面的互動檢查。`);
})().catch((error) => { console.error(error); process.exitCode = 1; });
