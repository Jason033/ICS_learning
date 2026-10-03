# 知識航線：系統維護與除錯學習網站

[打開學習網站](https://jason033.github.io/ICS_learning/) · [GitHub原始碼](https://github.com/Jason033/ICS_learning)

這是給新進軟體工程師的個人學習網站。目標是從基礎建立原理與正常流程，逐步能閱讀既有程式、想到有理由的故障位置、查證並修改。各大主題獨立，可以自行選擇；遇到知識缺口時，另有規劃中的可選共同基礎。

## 目前可以讀什麼

**全站正在重新設計成循序教材。目前完成一組三篇連續的新版網路入門，其餘123篇舊版資料保留供參考，尚未按新版方式重編。** 原有網址與瀏覽器閱讀紀錄保留。

1. [兩個程式為什麼需要通訊](https://jason033.github.io/ICS_learning/#/lesson/networking/why-programs-communicate)：從一次狀態查詢理解兩端角色、請求、回覆及共同約定。
2. [訊息內容如何表示成資料](https://jason033.github.io/ICS_learning/#/lesson/networking/messages-and-bytes)：沿同一份回覆，逐步認識bit、byte、文字編碼及HEX的用途。
3. [一次本地通訊經過哪些地方](https://jason033.github.io/ICS_learning/#/lesson/networking/a-local-journey)：沿同一回合走過程式、作業系統、網卡、線路與交換器。

三篇教的是入門關係，還沒有教完整IP、Port、TCP／UDP或程式除錯。下一組再打開目的地址與交付的選擇。網站把新版與舊版的目錄、前後篇導航分開，避免讀完新手教材突然跳進舊版濃縮內容。解答可展開，讀完可標記「已讀」；紀錄只存在目前瀏覽器，不等於已掌握能力。

## 為什麼重新設計

之前的教材雖增加了篇幅、圖、工程問題與實作，仍過於濃縮，許多段落要求讀者已經知道未教的名詞。它能回答某些問題，卻不能可靠地帶目前起點的讀者學出完整觀念。先前的檔案數、題數、程式與發布測試不能證明教學已完成。

這次以已確認的工作目標和起點重排知識：先知道問題與角色，再教表示與正常運作，之後才接讀碼、故障及修正。C#／.NET為主，Python為輔助；不預設讀者已熟C#。硬體深度以軟體需要理解的角色、介面、訊號、限制及觀測為主，不要求硬體設計或深入公式。

16個主題仍涵蓋網路、Wireshark、Ozeki、RS-232、無線電、PTT、Socket、VoIP、廠商API、可靠性與並行處理、作業系統觀測、衛星、訊號／DSP／SDR、資安、故障分析及概念整理。完整教學範圍與先後關係見[新版教學方案](revision/learning-redesign/curriculum-plan.md)；[舊課程地圖](課程地圖.md)保留既有資料清單，不能當新版完成清單。

## 審查方法、結果與限制

新教材採同一狀態查詢作基準，逐篇只打開新的必要問題；練習再改文字表示或流程條件，檢查能否用已教內容推理。作者自審、另一位作者從允許的前置連读、整合方全文審查與圖文檢查各負責不同部分。網站測試檢查載入、手機呈現、解答、導航與進度，不代替學習驗收。

目前三篇已完成正文與分工審查，並發布至GitHub Pages；公開檔案一致性與瀏覽功能已驗證。詳細發現與驗證見[新版重編紀錄](revision/learning-redesign/README.md)。目標讀者的實際理解仍需閱讀確認；不能宣稱已培養中級維護能力。下一步先確認這組教材能跟得上，再循同一知識路線展開後續與其他獨立主題。兩輪舊版製作與驗證資料留在[歷史紀錄](revision/README.md)。

## 教學情境與公司實際系統

圖、訊息、封包和程式模型是明確假設的教學材料，不能當成公司設備配置。Ozeki相關真實API需核對實際DLL版本；既有.NET 10教學模型也不能直接貼回公司的.NET Framework程式。公司RCSCall／iCallAPI依賴、設備行為及部署關係仍需實際文件和程式確認。工作筆記`對話.txt`沒有加入公開儲存庫。

## 本機開啟與維護

網站只有靜態HTML、CSS、JavaScript及JSON，沒有登入、伺服器或付費服務。GitHub Pages發布`main`分支的`/docs`。

```bash
python3 -m http.server 8000 --directory docs
```

Windows可用`py -m http.server 8000 --directory docs`，再開`http://localhost:8000/`。直接雙擊HTML可能無法載入JSON。

| 重要檔案／資料夾 | 用途 |
| --- | --- |
| docs/content/catalog.json | 主題、章節順序、狀態、簡介及教材版本 |
| docs/content/lessons/ | 一篇一個JSON，存正文、例題、練習、解答與來源 |
| docs/assets/diagrams/ | 原創SVG圖；不能把教學圖當真實拓撲 |
| docs/assets/labs/ | 原始碼、合成封包、操作說明與可下載專案ZIP |
| docs/app.js、docs/styles.css | 共用呈現與版型，新增章節通常不用改它們 |
| 課程地圖.md | 舊版資料清單與歷史範圍 |
| revision/ | 新版教學設計、審稿與驗證；另保留舊版歷史證據 |
| tools/audit_content.py | 唯讀量測；--finalize在全部可閱讀章節結構通過後同步時間與目錄 |
| tools/package_labs.py | 打包原始碼，排除bin／obj與可執行檔 |
| tests/smoke.cjs、tests/browser.py | 資料／呈現互動檢查與選用的真實瀏覽器檢查 |

新增章節：先在catalog加入穩定ID與planned狀態，再依[新版教材規格](revision/learning-redesign/authoring-standard.md)撰寫`<topic>--<chapter>.json`。內容審查、相關模型與網站驗證通過後才改成ready。新增主題則建立自己的目錄，不必變更其他主題順序。

基本檢查：

```bash
node --check docs/app.js
node tests/smoke.cjs
python3 tools/audit_content.py
```

瀏覽器檢查需另安裝Playwright與Chromium，開本機伺服器後執行`python tests/browser.py http://localhost:8000/`。C#教學專案解壓後依各自README執行；網站閱讀本身不需安裝.NET。驗證時在暫存資料夾編譯，避免把bin／obj發布。

下一步是驗證新版連續教材的理解與銜接，再依教學方案重編全站。公司相關內容需要真實文件才能映射，不預先替未知系統定義架構。
