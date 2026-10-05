# 知識航線：系統維護與除錯學習網站

[開啟網站](https://jason033.github.io/ICS_learning/) · [GitHub 原始碼](https://github.com/Jason033/ICS_learning)

這是給新進軟體工程師的個人教材，目標是把熟悉的資工概念接成可用的系統流程，練習讀既有程式、判斷故障停在哪個交接點，以及驗收有限修改。各主題可以獨立選讀；遇到前置缺口再補參照，不必先修完一整套共同基礎。

## 目前教材

全站有 126 篇教材：78 篇完成新版，48 篇仍保留為舊版參考資料。新版包含 28 篇可按需補讀的網路、Wireshark、Socket 與 RS-232 教材，以及六個核心系列的 50 篇。首頁會把六個核心系列放在一起；舊版入口另有標示。

| 核心系列 | 篇數 | 主要學習任務 |
| --- | ---: | --- |
| [Ozeki 本機音訊控制](https://jason033.github.io/ICS_learning/#/topic/ozeki) | 7 | 追麥克風、媒體串接、錄音與喇叭播放的程式生命週期；SIP／RTP只作為音訊進出通話的邊界。 |
| [PTT](https://jason033.github.io/ICS_learning/#/topic/ptt) | 8 | 從按下發話追到准入、設備控制、音訊時序、放開與故障恢復。 |
| [VoIP](https://jason033.github.io/ICS_learning/#/topic/voip) | 9 | 串起 SIP 呼叫、SDP 媒體協商、RTP 傳送與通話品質診斷。 |
| [無線電通訊](https://jason033.github.io/ICS_learning/#/topic/radio) | 8 | 從軟體資料、收發路徑到鏈路品質觀測；硬體只講維護需要的介面與邊界。 |
| [廠商 API 整合](https://jason033.github.io/ICS_learning/#/topic/integration) | 9 | 依版本文件和程式追呼叫、事件、狀態、錯誤與資源責任，不假設未知公司的實作。 |
| [可靠性與並行](https://jason033.github.io/ICS_learning/#/topic/reliability) | 9 | 用執行順序、期限、重試、佇列與紀錄分析長時間運作問題。 |

網路、Wireshark、Socket、RS-232 四個參照主題共 28 篇新版，遇到相關工作再選讀。其餘 48 篇舊版仍可查閱，但尚未依新版標準逐篇改寫。這些數量說明目前範圍，不代表讀者已具備相應能力。

50 篇核心教材是可查閱的完整內容庫，不是線性必修清單。一般主線約 46 篇；Ozeki 電話媒體、PTT 介面盒、VoIP 註冊及無線電 RF 硬體鏈，按手邊系統條件選讀。三篇目錄已標明選讀條件，RF 硬體鏈也標示為選讀。

## 教材怎麼讀

從你遇到的工作選主題，再照該主題建議順序閱讀。每篇會交代流程中各程式和元件的責任、資料如何交接、哪些證據能支持哪些判斷，並附上練習與解答。題目不是常見問題清單，也不假設教材中的合成案例就是公司架構。

C#／.NET 是主要讀碼環境；Python只用在可重跑的教學工具。硬體內容著重軟體可見的角色、介面、限制與觀測，不要求設計硬體或推導硬體數學。閱讀進度只存在目前瀏覽器，不會自動同步。

## 本機預覽與維護

網站是靜態 HTML、CSS、JavaScript 和 JSON，GitHub Pages 從 `main/docs` 發布。本機預覽可執行：

```bash
python3 -m http.server 8000 --directory docs
```

再開啟 `http://localhost:8000/`。新增章節時，在 `docs/content/lessons/` 加入教材 JSON，並於 `docs/content/catalog.json` 登記相同 ID；保留既有 ID 可維持原有章節連結和本機閱讀紀錄。

| 路徑 | 用途 |
| --- | --- |
| `docs/content/catalog.json` | 主題、章節順序、標題和教材版本 |
| `docs/content/lessons/` | 每篇教材的正文、練習、解答和來源 |
| `docs/assets/diagrams/`、`docs/assets/labs/` | 示意圖、教學程式與合成資料 |
| `docs/app.js`、`docs/styles.css` | 網站呈現、導覽及樣式 |
| `revision/learning-redesign/` | 學習目標、課程分工、教材規格與驗收紀錄 |
| `tools/audit_content.py` | 唯讀檢查資料結構；不估計閱讀時間，也不替教材打品質分數 |
| `tests/smoke.cjs`、`tests/browser.py` | 網站資料與互動檢查；後者需要 Playwright／Chromium |

## 驗收範圍與限制

目前的 JavaScript 語法、網站路由／互動、126 篇教材資料結構與合成 PCAP 檢查通過；教材結構檢查有 0 項警戒。這些檢查不能證明讀者已學會。本次環境沒有 Playwright／Chromium 或 Wireshark／TShark，因此沒有聲稱通過瀏覽器畫面或原生 Wireshark 驗收；.NET 教材程式也沒有在本次環境重新建置。每個系列另有作者檢查與獨立技術／銜接審查紀錄。

尚未取得公司原始碼、部署圖、實際 Ozeki 套件版本及設備資訊，所以教材說明一般維護方法，不宣稱知道公司系統的流程或根因。下一步的品質驗收應由讀者選一篇連續試讀，指出哪個流程交接仍不清楚，再針對卡點修正。

完整設計取捨與檢查證據見[改版紀錄](revision/learning-redesign/README.md)、[核心教材複核](revision/core-series-quality-followup.md)及[驗收資料](revision/learning-redesign/verification.json)。
