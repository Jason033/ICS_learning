const app = document.querySelector('#main');
let catalog = null;
let routeVersion = 0;
const completedKey = 'learning-site-completed-v1';

function escapeHtml(value) {
  return String(value ?? '').replace(/[&<>"']/g, (character) => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
  })[character]);
}

function safeAsset(value) {
  return /^\.\/assets\/[A-Za-z0-9/-]+\.(?:pcap|pcapng|svg|png|py|cs|json|csv|zip|txt|md)$/.test(value || '') ? value : '#';
}

function timeRange(range) {
  return Array.isArray(range) && range.length === 2 ? `${range[0]}–${range[1]} 分` : '尚待估計';
}

function chapterTimeLabel(chapter) {
  return chapter.studyTime
    ? `閱讀 ${timeRange(chapter.studyTime.reading)} · 操作 ${timeRange(chapter.studyTime.practice)} · 作答 ${timeRange(chapter.studyTime.exercises)}`
    : '教材重編中';
}

function renderAnswer(exercise) {
  const paragraphs = Array.isArray(exercise.answer) ? exercise.answer : String(exercise.answer || '').split(/\n\s*\n/);
  return paragraphs.map((text) => `<p>${escapeHtml(text)}</p>`).join('')
    + (exercise.answerSteps ? `<ol class="step-list">${exercise.answerSteps.map((step) => `<li>${escapeHtml(step)}</li>`).join('')}</ol>` : '')
    + (exercise.answerCode ? `<figure class="code-example"><figcaption>${escapeHtml(exercise.answerCode.title || '解答程式')}</figcaption><pre><code>${escapeHtml(exercise.answerCode.text)}</code></pre></figure>` : '');
}

function sectionLinks(lesson) {
  return (lesson.sections || []).map((section, index) => `<a class="toc-link" href="#section-${index + 1}">${escapeHtml(section.heading)}</a>`).join('')
    + '<a class="toc-link" href="#practice">練習與解答</a><a class="toc-link" href="#recap">重點複習</a>';
}

function safeLink(value) {
  try {
    const url = new URL(value);
    return ['https:', 'http:'].includes(url.protocol) ? url.href : '#';
  } catch {
    return '#';
  }
}

function getCompleted() {
  try {
    const value = JSON.parse(localStorage.getItem(completedKey) || '[]');
    return Array.isArray(value) ? new Set(value.filter((item) => typeof item === 'string')) : new Set();
  } catch {
    return new Set();
  }
}

function setCompleted(values) {
  try {
    localStorage.setItem(completedKey, JSON.stringify([...values]));
    return true;
  } catch {
    return false;
  }
}

function lessonKey(topicId, chapterId) {
  return `${topicId}/${chapterId}`;
}

function readyChapters(topic) {
  return topic.chapters.filter((chapter) => chapter.status === 'ready');
}

function topicHref(topic) {
  return `#/topic/${encodeURIComponent(topic.id)}`;
}

function lessonHref(topic, chapter) {
  return `#/lesson/${encodeURIComponent(topic.id)}/${encodeURIComponent(chapter.id)}`;
}

function breadcrumb(items) {
  return `<nav class="breadcrumb" aria-label="所在位置">${items.map((item, index) =>
    item.href ? `<a href="${escapeHtml(item.href)}">${escapeHtml(item.label)}</a><span aria-hidden="true">/</span>`
      : `<span aria-current="page">${escapeHtml(item.label)}</span>`
  ).join('')}</nav>`;
}

function statusBadge(status) {
  return status === 'ready'
    ? '<span class="status-badge is-ready">可閱讀</span>'
    : '<span class="status-badge is-planned">規劃中</span>';
}

function topicCard(topic, index, completed) {
  const ready = readyChapters(topic);
  const done = ready.filter((chapter) => completed.has(lessonKey(topic.id, chapter.id))).length;
  return `<a class="topic-card tone-${index % 6}" href="${topicHref(topic)}">
    <span class="topic-card-top"><span class="topic-icon" aria-hidden="true">${escapeHtml(topic.icon)}</span>${ready.length ? statusBadge('ready') : statusBadge('planned')}</span>
    <span class="topic-card-title">${escapeHtml(topic.title)}</span>
    <span class="topic-card-summary">${escapeHtml(topic.summary)}</span>
    <span class="topic-card-foot">${ready.length ? `${done} / ${ready.length} 篇已完成` : '章節規劃中'}<span aria-hidden="true">↗</span></span>
  </a>`;
}

function renderHome() {
  const completed = getCompleted();
  const readyCount = catalog.topics.reduce((count, topic) => count + readyChapters(topic).length, 0);
  document.title = `${catalog.siteTitle}｜主題總覽`;
  app.innerHTML = `<div class="shell page-home">
    <section class="hero">
      <div class="hero-copy"><p class="eyebrow"><span class="pulse-dot"></span> 個人學習空間</p>
        <h1>從一個問題開始，<br><em>把知識學成自己的。</em></h1>
        <p class="hero-lead">每個主題都能獨立學習。從基礎觀念與運作原理，逐步讀懂程式、分析故障；需要時再走進相關主題，建立維護與除錯的判斷力。</p>
        <button class="primary-button" type="button" id="explore-topics">探索學習主題 <span aria-hidden="true">↗</span></button>
      </div>
      <div class="hero-visual" aria-hidden="true">
        <div class="orbit orbit-outer"></div><div class="orbit orbit-inner"></div>
        <span class="orbit-symbol symbol-a">◎</span><span class="orbit-symbol symbol-b">⇄</span><span class="orbit-symbol symbol-c">⌕</span>
        <div class="hero-center"><small>LEARN / OBSERVE / UNDERSTAND</small><strong>從概念<br>到判斷</strong><span>01 — ∞</span></div>
      </div>
    </section>
    <section class="catalog-section" id="topics" aria-labelledby="topics-title">
      <div class="section-heading"><div><p class="eyebrow">EXPLORE TOPICS</p><h2 id="topics-title">選一個主題，開始學</h2><p>各主題有自己的章節順序。標示「規劃中」的內容，會在後續逐步補上。</p></div><div class="catalog-count">${catalog.topics.length}<small>個主題 · ${readyCount} 篇可閱讀</small></div></div>
      <label class="search-box"><span aria-hidden="true">⌕</span><span class="sr-only">搜尋主題</span><input id="topic-search" type="search" placeholder="搜尋主題，例如 Wireshark、PTT、RS-232" autocomplete="off"></label>
      <div id="topic-grid" class="topic-grid">${catalog.topics.map((topic, index) => topicCard(topic, index, completed)).join('')}</div>
      <p id="empty-search" class="empty-search" hidden>找不到符合的主題，試試其他關鍵字。</p>
    </section>
  </div>`;
  document.querySelector('#explore-topics').addEventListener('click', () => {
    document.querySelector('#topics').scrollIntoView({ behavior: 'smooth' });
  });
  const search = document.querySelector('#topic-search');
  search.addEventListener('input', () => {
    const query = search.value.trim().toLocaleLowerCase('zh-Hant');
    let shown = 0;
    document.querySelectorAll('.topic-card').forEach((card, index) => {
      const topic = catalog.topics[index];
      const match = !query || `${topic.title} ${topic.summary} ${topic.chapters.map((chapter) => chapter.title).join(' ')}`.toLocaleLowerCase('zh-Hant').includes(query);
      card.hidden = !match;
      if (match) shown += 1;
    });
    document.querySelector('#empty-search').hidden = shown !== 0;
  });
}

function renderTopic(topic) {
  const completed = getCompleted();
  const ready = readyChapters(topic);
  const done = ready.filter((chapter) => completed.has(lessonKey(topic.id, chapter.id))).length;
  document.title = `${topic.title}｜${catalog.siteTitle}`;
  app.innerHTML = `<div class="shell page-topic">
    ${breadcrumb([{ label: '主題總覽', href: '#/' }, { label: topic.title }])}
    <header class="topic-hero"><div><p class="eyebrow">INDEPENDENT TOPIC · 獨立主題</p><div class="topic-hero-title"><span class="topic-hero-icon" aria-hidden="true">${escapeHtml(topic.icon)}</span><h1>${escapeHtml(topic.title)}</h1></div><p>${escapeHtml(topic.summary)}</p></div><div class="progress-panel"><strong>${done}<span> / ${ready.length}</span></strong><small>可閱讀章節已完成</small><div class="progress-track"><span style="width:${ready.length ? Math.round(done / ready.length * 100) : 0}%"></span></div></div></header>
    <div class="topic-body"><div class="topic-intro"><p class="eyebrow">CHAPTERS</p><h2>章節目錄</h2><p>依這個主題自己的順序學習。其他主題只有在內容確實相關時才會連結。</p></div>
    <ol class="chapter-list">${topic.chapters.map((chapter, index) => {
      const isReady = chapter.status === 'ready';
      const isDone = completed.has(lessonKey(topic.id, chapter.id));
      const inner = `<span class="chapter-number">${String(index + 1).padStart(2, '0')}</span><span class="chapter-copy"><strong>${escapeHtml(chapter.title)}</strong><small>${escapeHtml(chapter.summary)}</small></span><span class="chapter-state">${isDone ? '✓ 已完成 · ' : ''}${isReady ? `${chapter.contentVersion === 2 ? '工程教材 · ' : ''}${chapterTimeLabel(chapter)}` : '規劃中'}</span><span class="chapter-arrow" aria-hidden="true">${isReady ? '↗' : '·'}</span>`;
      return `<li>${isReady ? `<a class="chapter-row" href="${lessonHref(topic, chapter)}">${inner}</a>` : `<div class="chapter-row is-planned">${inner}</div>`}</li>`;
    }).join('')}</ol></div>
    <a class="text-link back-link" href="#/">← 回到所有主題</a>
  </div>`;
}

function renderSection(section, index) {
  return `<section class="lesson-section" id="section-${index + 1}"><div class="section-index">${String(index + 1).padStart(2, '0')}</div><div class="section-content"><h2>${escapeHtml(section.heading)}</h2>${section.kind ? `<p class="section-kind">${escapeHtml(({concept:"建立概念",mechanism:"機制推導","worked-example":"完整例題","code-reading":"程式追讀",lab:"維護實作","diagnostic-case":"故障鑑別",maintenance:"維護與取捨"})[section.kind] || "工程練習")}</p>` : ''}
    ${(section.paragraphs || []).map((paragraph) => `<p>${escapeHtml(paragraph)}</p>`).join('')}
    ${section.flow ? `<div class="flow-box" role="img" aria-label="流程：${escapeHtml(section.flow.join('，接著'))}"><div class="flow-title">流程圖 · 教學示意</div><div class="flow-steps">${section.flow.map((step) => `<span>${escapeHtml(step)}</span>`).join('<b aria-hidden="true">→</b>')}</div></div>` : ''}
    ${section.bullets ? `<ul class="content-list">${section.bullets.map((bullet) => `<li>${escapeHtml(bullet)}</li>`).join('')}</ul>` : ''}
    ${section.steps ? `<ol class="step-list">${section.steps.map((step) => `<li>${escapeHtml(step)}</li>`).join('')}</ol>` : ''}
    ${section.table ? `<div class="data-table-wrap" role="region" tabindex="0" aria-label="可左右捲動的表格：${escapeHtml(section.table.caption || section.heading)}"><span class="table-scroll-hint">表格可左右滑動</span><table class="lesson-table"><caption>${escapeHtml(section.table.caption || section.heading)}</caption><thead><tr>${section.table.headers.map((header) => `<th scope="col">${escapeHtml(header)}</th>`).join('')}</tr></thead><tbody>${section.table.rows.map((row) => `<tr>${row.map((cell) => `<td>${escapeHtml(cell)}</td>`).join('')}</tr>`).join('')}</tbody></table></div>` : ''}
    ${section.image ? `<figure class="lesson-image"><img src="${escapeHtml(safeAsset(section.image.src))}" alt="${escapeHtml(section.image.alt || '')}" loading="lazy"><figcaption>${escapeHtml(section.image.caption || '教學示意圖')}</figcaption></figure>` : ''}
    ${section.code ? `<figure class="code-example"><figcaption>${escapeHtml(section.code.title || '教學範例')}</figcaption><pre><code>${escapeHtml(section.code.text)}</code></pre></figure>` : ''}
    ${section.note ? `<aside class="note-box"><strong>留意這一點</strong><p>${escapeHtml(section.note)}</p></aside>` : ''}
  </div></section>`;
}

function renderLesson(topic, chapter, lesson) {
  const completed = getCompleted();
  const key = lessonKey(topic.id, chapter.id);
  const currentIndex = topic.chapters.findIndex((item) => item.id === chapter.id);
  const previous = [...topic.chapters.slice(0, currentIndex)].reverse().find((item) => item.status === 'ready');
  const next = topic.chapters.slice(currentIndex + 1).find((item) => item.status === 'ready');
  document.title = `${chapter.title}｜${catalog.siteTitle}`;
  app.innerHTML = `<div class="shell page-lesson">
    ${breadcrumb([{ label: '主題總覽', href: '#/' }, { label: topic.title, href: topicHref(topic) }, { label: chapter.title }])}
    <div class="lesson-layout"><aside class="lesson-aside" aria-label="本主題章節目錄"><div class="aside-sticky"><a class="aside-topic" href="${topicHref(topic)}"><span aria-hidden="true">${escapeHtml(topic.icon)}</span>${escapeHtml(topic.title)} <b aria-hidden="true">↗</b></a><p class="aside-label">本主題章節</p><ol>${topic.chapters.map((item) => `<li>${item.status === 'ready' ? `<a href="${lessonHref(topic, item)}" ${item.id === chapter.id ? 'aria-current="page"' : ''}>${escapeHtml(item.title)}</a>` : `<span class="aside-planned">${escapeHtml(item.title)} <small>規劃中</small></span>`}</li>`).join('')}</ol></div></aside>
    <article class="lesson-article"><header class="lesson-header"><p class="eyebrow">${escapeHtml(topic.title)} · ${String(currentIndex + 1).padStart(2, '0')} / ${String(topic.chapters.length).padStart(2, '0')}</p><h1>${escapeHtml(chapter.title)}</h1><div class="lesson-meta"><span>${escapeHtml(chapterTimeLabel(chapter))}</span><span>${lesson.contentVersion === 2 ? '原理 · 讀碼 · 除錯' : '教材重編中'}</span></div><p class="lesson-intro">${escapeHtml(lesson.intro)}</p></header>
    <details class="mobile-lesson-toc"><summary>本章內容與練習</summary><nav aria-label="本章內容">${sectionLinks(lesson)}</nav></details>
    <section class="goal-box" aria-labelledby="goal-title"><div class="goal-icon" aria-hidden="true">✳</div><div><h2 id="goal-title">學完這章，你能…</h2><ul>${(lesson.goals || []).map((goal) => `<li>${escapeHtml(goal)}</li>`).join('')}</ul></div></section>
    ${(lesson.resources || []).length ? `<section class="lesson-resources" aria-labelledby="resources-title"><h2 id="resources-title">本章練習檔</h2><ul>${lesson.resources.map((resource) => `<li><a href="${escapeHtml(safeAsset(resource.path))}" download>${escapeHtml(resource.label)} <span aria-hidden="true">↓</span></a><p>${escapeHtml(resource.description || '')}</p></li>`).join('')}</ul></section>` : ''}
    ${lesson.prerequisites?.length ? `<section class="prerequisite-box"><h2>本章會補足的前置概念</h2><ul>${lesson.prerequisites.map((item) => `<li>${escapeHtml(item)}</li>`).join('')}</ul></section>` : ''}
    ${lesson.studyTime ? `<section class="lesson-plan" aria-labelledby="plan-title"><h2 id="plan-title">閱讀與動手時間分開估計</h2><ol><li><span>閱讀、推導與追程式</span><strong>${timeRange(lesson.studyTime.reading)}</strong></li><li><span>操作、比較與修正</span><strong>${timeRange(lesson.studyTime.practice)}</strong></li><li><span>獨立作答與核對</span><strong>${timeRange(lesson.studyTime.exercises)}</strong></li></ol><p class="time-basis">各項都是預估範圍，尚未經個人計時校準；初次安裝工具另計。</p><details class="estimate-details"><summary>查看時間估計依據</summary><p>${escapeHtml(lesson.studyTime.basis)}</p></details></section>` : ''}
    ${(lesson.sections || []).map(renderSection).join('')}
    <section class="exercise-block" id="practice"><p class="eyebrow">PRACTICE</p><h2>想一想，再看解答</h2><p class="exercise-lead">依序檢查基本觀念、應用推導與故障判斷。先自己回答，再核對解答中的規則、中間步驟與結論。</p>${(lesson.exercises || []).map((exercise, index) => `<div class="exercise-card"><div class="exercise-question"><span>練習 ${String(index + 1).padStart(2, '0')} · ${escapeHtml(({foundation:"基礎理解",application:"應用推導",diagnosis:"故障判斷"})[exercise.level] || "理解練習")}</span><p>${escapeHtml(exercise.question)}</p></div><details><summary>查看解答與判斷過程 <span aria-hidden="true">↓</span></summary>${renderAnswer(exercise)}</details></div>`).join('')}</section>
    <section class="recap-block" id="recap"><p class="eyebrow">KEY TAKEAWAYS</p><h2>帶走這幾件事</h2><ul>${(lesson.recap || []).map((item) => `<li>${escapeHtml(item)}</li>`).join('')}</ul></section>
    ${(lesson.sources || []).length ? `<section class="sources"><h2>延伸閱讀與依據</h2><ul>${lesson.sources.map((source) => `<li><a href="${escapeHtml(safeLink(source.url))}" target="_blank" rel="noopener noreferrer">${escapeHtml(source.label)} <span aria-hidden="true">↗</span></a></li>`).join('')}</ul></section>` : ''}
    <div class="lesson-actions"><div><strong>讀完這章了嗎？</strong><p>完成紀錄只保存在目前這個瀏覽器。</p></div><button type="button" id="complete-button" class="complete-button ${completed.has(key) ? 'is-complete' : ''}" aria-pressed="${completed.has(key)}">${completed.has(key) ? '✓ 已標記完成' : '標記為已完成'}</button></div>
    <nav class="lesson-next" aria-label="章節導覽">${previous ? `<a href="${lessonHref(topic, previous)}"><small>← 上一篇</small><strong>${escapeHtml(previous.title)}</strong></a>` : '<span></span>'}${next ? `<a href="${lessonHref(topic, next)}"><small>下一篇 →</small><strong>${escapeHtml(next.title)}</strong></a>` : `<a href="${topicHref(topic)}"><small>返回 →</small><strong>${escapeHtml(topic.title)}目錄</strong></a>`}</nav>
    </article><aside class="lesson-toc" aria-label="本章段落"><div class="aside-sticky"><p class="aside-label">本章內容</p><a href="#section-1" class="toc-link">從第一段開始</a>${sectionLinks(lesson)}</div></aside></div>
  </div>`;
  document.querySelector('#complete-button').addEventListener('click', (event) => {
    const values = getCompleted();
    if (values.has(key)) values.delete(key); else values.add(key);
    if (!setCompleted(values)) {
      event.currentTarget.textContent = '無法儲存，請檢查瀏覽器設定';
      return;
    }
    const done = values.has(key);
    event.currentTarget.textContent = done ? '✓ 已標記完成' : '標記為已完成';
    event.currentTarget.classList.toggle('is-complete', done);
    event.currentTarget.setAttribute('aria-pressed', String(done));
  });
  document.querySelectorAll('.lesson-toc a, .mobile-lesson-toc a').forEach((link) => link.addEventListener('click', (event) => {
    event.preventDefault();
    document.querySelector(link.getAttribute('href'))?.scrollIntoView({ behavior: 'smooth' });
  }));
}

function renderAbout() {
  document.title = `使用說明｜${catalog.siteTitle}`;
  app.innerHTML = `<div class="shell page-about">${breadcrumb([{ label: '主題總覽', href: '#/' }, { label: '使用說明' }])}<div class="about-card"><p class="eyebrow">HOW TO USE</p><h1>照自己的問題，選一個主題。</h1><p>每個主題獨立安排章節。你可以從最感興趣的內容開始，再依章節順序讀圖文、做練習、展開解答。只有內容確實相關時，才需要跳到另一個主題。</p><div class="about-grid"><section><span>01</span><h2>選擇主題</h2><p>首頁列出所有主題。「規劃中」表示已列入範圍，教材還沒完成。</p></section><section><span>02</span><h2>練習與核對</h2><p>每章都把概念與判斷依據寫清楚。解答收在題目下方，先自己想，再展開閱讀。</p></section><section><span>03</span><h2>記下進度</h2><p>讀完可以標記完成。紀錄存在你目前的瀏覽器，不會自動同步到其他裝置。</p></section></div><div class="about-note"><strong>關於工作中的實際系統</strong><p>教材中的圖與案例會說明假設。公司設備與程式的具體做法，應以取得授權的文件、程式碼及實際觀察為準。</p></div><a class="primary-button" href="#/">前往主題總覽 <span aria-hidden="true">↗</span></a></div></div>`;
}

function renderMissing(message) {
  document.title = `找不到內容｜${catalog.siteTitle}`;
  app.innerHTML = `<div class="shell error-page"><span class="error-symbol" aria-hidden="true">⌁</span><h1>這頁暫時找不到</h1><p>${escapeHtml(message)}</p><a class="primary-button" href="#/">返回主題總覽 <span aria-hidden="true">↗</span></a></div>`;
}

async function renderRoute() {
  if (!catalog) return;
  const version = ++routeVersion;
  let route;
  try {
    route = decodeURIComponent(location.hash.slice(1) || '/').split('/').filter(Boolean);
  } catch {
    renderMissing('網址格式無法辨識，請從主題總覽重新選擇內容。');
    return;
  }
  if (route.length === 0) document.querySelector('#home-link').setAttribute('aria-current', 'page');
  else document.querySelector('#home-link').removeAttribute('aria-current');
  if (route[0] === 'about') document.querySelector('#about-link').setAttribute('aria-current', 'page');
  else document.querySelector('#about-link').removeAttribute('aria-current');
  if (route.length === 0) renderHome();
  else if (route.length === 1 && route[0] === 'about') renderAbout();
  else if (route.length === 2 && route[0] === 'topic') {
    const topic = catalog.topics.find((item) => item.id === route[1]);
    if (topic) renderTopic(topic); else renderMissing('這個主題目前不在清單中。');
  } else if (route.length === 3 && route[0] === 'lesson') {
    const topic = catalog.topics.find((item) => item.id === route[1]);
    const chapter = topic?.chapters.find((item) => item.id === route[2]);
    if (!topic || !chapter || chapter.status !== 'ready') return renderMissing('這篇教材尚未開放，請回主題目錄查看。');
    try {
      const response = await fetch(`./content/lessons/${encodeURIComponent(topic.id)}--${encodeURIComponent(chapter.id)}.json`);
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const lesson = await response.json();
      if (version !== routeVersion) return;
      renderLesson(topic, chapter, lesson);
    } catch (error) {
      if (version !== routeVersion) return;
      renderMissing('教材載入失敗。請確認網路連線，或使用本機伺服器開啟網站。');
    }
  } else renderMissing('請從主題總覽選擇內容。');
  window.scrollTo({ top: 0, behavior: 'auto' });
}

async function start() {
  try {
    const response = await fetch('./content/catalog.json');
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    catalog = await response.json();
    if (!Array.isArray(catalog.topics)) throw new Error('Invalid catalog');
    await renderRoute();
  } catch (error) {
    app.innerHTML = '<div class="shell error-page"><h1>網站資料載入失敗</h1><p>請確認 content/catalog.json 存在，並透過本機伺服器或 GitHub Pages 開啟網站。</p></div>';
  }
}

window.addEventListener('hashchange', renderRoute);
start();
