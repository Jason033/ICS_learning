# 教材改版紀錄：從熟悉概念到讀懂系統

更新日期：2026-10-05

## 專案要解決的問題

使用者是新進軟體工程師，資工系常見概念大致理解；主要需要把概念連到實際通訊流程、程式責任和故障證據。教材的目標是讓讀者遇到程式或設備流程中斷時，能沿資料和狀態的交接順序提出可驗證的故障位置，閱讀並有限度地修改既有程式。

教材不把讀者當成第一次聽過 IP、Socket 或位元組，也不假設能熟練應用它們。主要讀碼語境是 C#／.NET；C# 語法在實際用到時補解釋。硬體內容說明軟體維護所需的角色、介面、訊號方向、限制及可觀察資訊，不要求硬體設計。公司系統與具體根因必須依實際程式、文件、版本和觀測確認，不能從通用教材推定。

## 目前課程範圍

全站有 16 個主題、126 篇已開放教材。78 篇已完成新版，另 48 篇仍是舊版參考資料。新版由兩部分組成：28 篇網路、Wireshark、Socket、RS-232 參照教材，以及六個核心系列的 50 篇。最初的「約 40 篇」是估量；逐篇按實際學習任務劃分後是 50 篇。作者和獨立審查檢查了相鄰責任，沒有發現應為符合估量而合併的重複篇章。篇數是描述現況，不是教材品質指標。

| 系列 | 新版篇數 | 學習主問題 | 審查紀錄 |
| --- | ---: | --- | --- |
| Ozeki 本機音訊控制 | 7 | 麥克風擷取、媒體串接／處理、錄音與喇叭播放的本機程式生命週期；SIP／RTP只作音訊來源或輸出的邊界。 | [作者檢查](../ozeki-audio-review.md)、[Ozeki／VoIP 獨立審查](../cross-topic-review.md) |
| PTT | 8 | 發話意圖、准入、設備控制、音訊時序、放開與恢復之間的狀態交接。 | [作者檢查](../ptt-review.md)、[獨立審查](../ptt-peer-review.md) |
| VoIP | 9 | SIP呼叫控制如何連到SDP媒體協商、RTP收送與品質診斷。 | [作者檢查](../voip-review.md)、[Ozeki／VoIP 獨立審查](../cross-topic-review.md) |
| 無線電通訊 | 8 | 軟體資料如何經收發路徑成為可觀測的鏈路狀態；不教 RF 硬體設計。 | [作者檢查](../radio-review.md) |
| 廠商 API 整合 | 9 | 依版本文件和程式理解呼叫、事件、狀態、錯誤及資源責任。 | [作者檢查](../integration-review.md)、[獨立審查](../integration-peer-review.md) |
| 可靠性與並行 | 9 | 用順序、期限、佇列、重試、重連與紀錄分析持續運作問題。 | [作者檢查](../reliability-review.md)、[獨立審查](../reliability-peer-review.md) |

50 篇是完整教材庫，不是要求每個人照順序讀完的必修量。一般閱讀主線約 46 篇；4 篇依實際系統條件選讀：只有本機音訊要接入電話媒體才讀 Ozeki 的「電話媒體邊界」，只有設備使用介面盒才讀 PTT 介面盒，只有系統使用 SIP REGISTER 才讀 VoIP 註冊，無線電 RF 鏈與天線章本來就是硬體觀測的選讀。目錄已標出前 3 篇的條件，後續可按手邊工作跳讀。

六個系列各自獨立，必要時連結到網路或工具參照，不強迫把無關領域串成一條假想公司流程。其他 Windows／Linux 觀測、衛星、DSP／SDR、資安、通用故障分析與概念地圖仍保留入口，但未包含在本輪新版範圍。

完整的課程分工、哪些內容可跳讀以及共同前置如何補，見[課程架構](curriculum-plan.md)。

## 怎麼驗收、目前能下什麼結論

這一輪完成了三種不同的核對：

1. **作者檢查：**逐篇核對流程、程式、練習答案、假設和來源。
2. **獨立技術／銜接審查：**各系列由另一位審查者抽查或全文檢查；發現事項修正後讀回。審查紀錄分列在上表。
3. **可重跑的資料檢查：**JavaScript 語法、16 個主題與 126 篇路由／頁面互動、JSON 結構，以及教學 PCAP 格式和所列案例。

最新檢查命令在 [verification.json](verification.json)。本次執行結果：`node --check docs/app.js`、`node tests/smoke.cjs`、`python3 tools/audit_content.py`、`python3 tests/verify_labs.py`、`python3 docs/assets/labs/wireshark-maintenance-verify.py` 與 `git diff --check` 均通過；結構警戒為 0。這代表資料和列明的合成案例符合檢查，不代表教學已被讀者學會。

本次環境沒有 Playwright／Chromium、Wireshark／TShark，也無法啟動 .NET SDK，因此沒有本輪瀏覽器畫面、原生 dissector 或新建置 C# 教材程式的結果。舊的 .NET 測試紀錄是歷史驗證，不能代替本輪重跑。使用者本人尚未連續試讀；公司程式、部署、Ozeki 實際版本和設備亦未提供。這些是後續驗收要補的證據，並非教材可以代替的假設。

目前公開網址仍是 [GitHub Pages](https://jason033.github.io/ICS_learning/)，但本輪六系列更新尚未發布。GitHub 主分支最後確認仍在舊版；本機已備妥新版提交，唯目前環境沒有可用的 GitHub 寫入憑證。[publication-verification.json](publication-verification.json) 保留的是 2026-10-03 三篇試行教材的歷史快照，不可當成本輪發布證據。取得寫入權限並發布後，必須以本輪 commit 和公開頁面檢查結果更新該檔及 [verification.json](verification.json)。

## 重要檔案

| 檔案 | 用途 |
| --- | --- |
| [curriculum-plan.md](curriculum-plan.md) | 讀者起點、共同流程圖、系列責任邊界與選讀原則 |
| [authoring-standard.md](authoring-standard.md) | 教材撰寫、來源、練習與品質審查規格 |
| [verification.json](verification.json) | 本輪可重跑檢查的日期、命令、結果和限制 |
| [reader-review.md](reader-review.md) | 2026-10-04 首批28篇參照教材的專項審查；不是本輪78篇的總報告 |
| [recalibration.md](recalibration.md) | 使用者起點、範圍及教學取捨 |
| [wireshark-audit.md](wireshark-audit.md) | Wireshark 教材分工、素材與驗收界線 |
| [core-series-quality-followup.md](../core-series-quality-followup.md) | 六個核心系列的獨立複核發現、修正和驗證界線 |
| `*-review.md`、`*-peer-review.md` | 各系列的作者檢查及獨立技術審查 |
| `publication-verification.json` | 舊版三篇試行教材的公開核對快照；新版發布後需更新 |

Markdown 說明前因後果和結論，JSON 保存逐項檢查資料；只看測試數字不能代替這份說明。
