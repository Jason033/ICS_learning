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
    } else assert.equal(chapter.status, 'planned');
  }
}
assert.equal(catalog.topics.length, 16);
assert.equal(readyCount, 6);

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
  assert.doesNotMatch(element('#main').innerHTML, /#\/lesson\/wireshark\/filters/);

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

  console.log(`通過：${catalog.topics.length} 個獨立主題、${readyCount} 篇教材，以及首頁／主題／章節／進度／規劃中頁面的互動檢查。`);
})().catch((error) => { console.error(error); process.exitCode = 1; });
