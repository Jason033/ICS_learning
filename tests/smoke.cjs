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
      assert.equal(lesson.contentVersion, 2, `${file} 尚未重編`);
      assert.equal(chapter.contentVersion, 2);
      assert.deepEqual(chapter.studyTime, lesson.studyTime);
      assert(!('minutes' in chapter), `${file} 不應沿用固定總分鐘`);
      assert(!lesson.plan, `${file} 不應沿用舊的學習分鐘表`);
      for (const field of ['intro', 'prerequisites', 'goals', 'sections', 'exercises', 'recap']) assert(lesson[field]?.length, `${file} 缺少 ${field}`);
      for (const field of ['reading', 'practice', 'exercises']) {
        const range = lesson.studyTime[field];
        assert.equal(range.length, 2);
        assert(range.every(Number.isFinite) && range[0] > 0 && range[0] <= range[1]);
      }
      assert(lesson.studyTime.basis, `${file} 缺少估計依據`);
      assert(lesson.sources.length >= 3);
      for (const source of lesson.sources || []) assert.match(source.url, /^https:\/\//);
      for (const resource of lesson.resources || []) {
        assert.match(resource.path, /^\.\/assets\/labs\/[A-Za-z0-9/-]+\.(?:pcap|pcapng|py|cs|json|csv|zip|txt|md)$/);
        assert(fs.existsSync(path.join(docs, resource.path.slice(2))), `${file} 缺少練習檔 ${resource.path}`);
      }
      assert(lesson.exercises.length >= 8);
      for (const exercise of lesson.exercises) {
        assert(['foundation', 'application', 'diagnosis'].includes(exercise.level));
        assert(exercise.question?.length && exercise.answer?.length);
        const paragraphs = Array.isArray(exercise.answer) ? exercise.answer : exercise.answer.split(/\n\s*\n/);
        assert(paragraphs.length >= 2 && paragraphs.every(p => typeof p === 'string' && p.trim()), `${file} 解答缺少分段說明`);
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
assert.equal(readyCount, 83);
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

  for (const topic of catalog.topics) {
    context.location.hash = `#/topic/${topic.id}`;
    await vm.runInContext('renderRoute()', context);
    assert(element('#main').innerHTML.includes(topic.title));
    for (const chapter of topic.chapters.filter(chapter => chapter.status === 'ready')) {
      context.location.hash = `#/lesson/${topic.id}/${chapter.id}`;
      await vm.runInContext('renderRoute()', context);
      const html = element('#main').innerHTML;
      assert(html.includes(chapter.title), `無法呈現 ${topic.id}/${chapter.id}`);
      assert.match(html, /閱讀與動手時間分開估計/);
      assert.match(html, /本章會補足的前置概念/);
      assert.match(html, /mobile-lesson-toc/);
      assert.match(html, /練習 08/);
      assert.match(html, /查看解答與判斷過程/);
      assert(!html.includes('href="#" download'), `下載連結被拒絕 ${topic.id}/${chapter.id}`);
      assert(!html.includes('[object Object]'), `解答陣列未正確呈現 ${topic.id}/${chapter.id}`);
    }
  }

  context.location.hash = '#/lesson/ozeki/read-a-call';
  await vm.runInContext('renderRoute()', context);
  const button = element('#complete-button');
  assert(button.listeners.click, '完成按鈕未綁定');
  button.listeners.click({ currentTarget: button });
  assert.deepEqual(JSON.parse(storage.get('learning-site-completed-v1')), ['ozeki/read-a-call']);
  context.location.hash = '#/topic/ozeki';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /<strong>1<span> \/ 7<\/span><\/strong>/);
  context.location.hash = '#/lesson/ozeki/read-a-call';
  await vm.runInContext('renderRoute()', context);
  element('#complete-button').listeners.click({ currentTarget: element('#complete-button') });
  assert.deepEqual(JSON.parse(storage.get('learning-site-completed-v1')), []);

  storage.set('learning-site-completed-v1', 'broken-json');
  assert.equal(vm.runInContext('getCompleted().size', context), 0);
  storage.set('learning-site-completed-v1', '{"invalid":true}');
  assert.equal(vm.runInContext('getCompleted().size', context), 0);
  assert.equal(vm.runInContext('safeAsset("./assets/labs/../../private.txt")', context), '#');
  assert.equal(vm.runInContext('safeLink("javascript:alert(1)")', context), '#');
  assert.equal(vm.runInContext('safeAsset("./assets/labs/networking-maintenance-lab.zip")', context), './assets/labs/networking-maintenance-lab.zip');
  const rendered = vm.runInContext('renderAnswer({answer:["<script>bad()</script>", "a & b"]})', context);
  assert(rendered.includes('&lt;script&gt;') && rendered.includes('a &amp; b'));
  assert(!rendered.includes('<script>'));

  // An older, slower fetch must not replace the chapter the reader selected later.
  const normalFetch = context.fetch;
  let releaseOld;
  context.fetch = relative => relative.includes('networking--one-conversation')
    ? new Promise(resolve => { releaseOld = async () => resolve(await normalFetch(relative)); })
    : normalFetch(relative);
  context.location.hash = '#/lesson/networking/one-conversation';
  const oldRoute = vm.runInContext('renderRoute()', context);
  context.location.hash = '#/lesson/ozeki/read-a-call';
  await vm.runInContext('renderRoute()', context);
  await releaseOld();
  await oldRoute;
  assert(element('#main').innerHTML.includes(catalog.topics.find(t => t.id === 'ozeki').chapters[0].title));
  context.fetch = normalFetch;
  context.location.hash = '#/%broken-encoding';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /網址格式無法辨識/);

  context.location.hash = '#/about';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /照自己的問題/);
  context.location.hash = '#/topic/no-such-topic';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /這個主題目前不在清單中/);

  context.location.hash = '#/lesson/systems/find-evidence';
  await vm.runInContext('renderRoute()', context);
  assert.match(element('#main').innerHTML, /這篇教材尚未開放/);

  console.log(`通過：${catalog.topics.length} 個獨立主題、${readyCount} 篇教材，以及首頁／主題／完整教材／進度／規劃中頁面的互動檢查。`);
})().catch((error) => { console.error(error); process.exitCode = 1; });
