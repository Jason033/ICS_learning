# ICS／RCS 與廠商 API：新版路徑與驗收紀錄

更新日期：2026-10-05

本系列的任務是教新進工程師追一筆外部控制操作，從應用輸入一路找到 API 呼叫、回覆／事件及狀態更新；遇到沒有公司來源支持的交接時，能把未知清楚留下。內容不宣稱掌握公司內部架構，也不把同一系列寫成常見故障問答集。

## 教學路徑

九個既有 lesson ID 保留，依目錄順序形成一條可逐步深入的閱讀路徑。每篇仍可單獨查閱；前文建立的操作身分和責任圖會在後文擴充，不重教一般資工概念。

| 既有 lesson ID | 本篇在路徑中的責任 | 讀者完成後應能做什麼 |
| --- | --- | --- |
| `trace-an-action` | 以 D1／60 的操作把 UI、service、adapter、SDK、外部交接及反向結果連成圖。 | 指出目前能證明到哪個交接，以及哪一段仍未知。 |
| `layers-and-ownership` | 追組裝點、執行中的具體實例、共享、訂閱及釋放。 | 找出真正被呼叫的 adapter，畫出 owner 與借用者。 |
| `read-an-icd` | 對照 API reference、ICD、版本說明、程式和運行證據；讀一份明示虛構的 C# API 簽章。 | 從文件與程式形成可測契約，列出缺失語義而不猜。 |
| `commands-and-events` | 區分同步 receipt、非同步 callback／事件、狀態處理和 UI 派送。 | 以 request 身分配對回覆，分析早到、亂序、重複和延遲。 |
| `sessions-and-state` | 將操作關聯擴充到 session／generation 和重連前後的狀態。 | 拒絕過期回報，避免把 Connected 或畫面綠燈當全面就緒。 |
| `audio-and-device-boundaries` | 說明控制 API 回覆、設備結果、可獨立觀測效果與連續媒體是不同責任邊界。 | 知道證據在哪一層停止，並將音訊／VoIP／PTT／RF 細節交回各自主題。 |
| `errors-and-recovery` | 依失敗階段區分拒絕、本地例外、逾時、明確失敗與未知。 | 依 API 契約決定查詢、等待或安全重試，不因逾時盲目重送。 |
| `correlate-evidence` | 對齊 app request、SDK operation ID、session、來源和時間基準。 | 寫出含來源、可支持結論及未知項目的交接紀錄。 |
| `integration-troubleshooting` | 用完整案例找出 Pending 狀態最早偏離預期的交接。 | 提出有限修正、可反駁假說與能區分故障機制的回歸。 |

此系列選擇保留九個 URL，因為呼叫路徑、物件生命週期、契約、非同步結果、session、外部效果、恢復與證據各有不同維護責任；把它們塞進少數大章會讓每章在錯誤、版本或事件問題間跳轉。重複的共同情境改由 D1／60 和相同 operation ID 串接，而不是在每篇重新教一遍架構。

## 合成案例和未知界線

主要例子為明示虛構的 D1／60 `SetLevel` 操作；合成 C# SDK 簽章標成 `IInventedVendorSdk`，可執行 Lab 則使用 `LabController`、`LabAdapterPort` 和 `LabScriptedAdapter`。兩者都只是教學契約：簽章與測試能示範呼叫、欄位、receipt、事件和 request/session 對應，不能代表公司採用相同類別、API、通訊協定或硬體。

公司原始碼、DLL、廠商產品與版本、傳輸方式、設備狀態語義及並行保證仍未知。實際映射時，先從 UI／service 呼叫者追到建立和注入點，確認部署中的 SDK 版本，再用相符 API 文件／ICD 解讀參數、回傳和事件，最後用該次執行的紀錄／trace／可用設備讀回驗證。沒有來源支持的連線、序列埠、網路或內部排程不畫成已確認架構。

## 證據和來源範圍

每篇將結論限制在證據所在的位置：

| 證據 | 能支持的問題 | 不能單獨證明的問題 |
| --- | --- | --- |
| 版本匹配的 API reference／ICD | 型別、欄位、單位、操作條件、回覆語義及文件化錯誤。 | 公司部署了該版本，或某次執行遵守文件。 |
| Release notes、專案引用與部署資訊 | 版本差異及實際選用的套件／DLL。 | 未記載的完整 API 行為或外部效果。 |
| 公司 adapter 實作 | 目前程式的參數轉換、分支、SDK 呼叫與事件映射。 | 黑箱 SDK 內部交換，除非另有證據。 |
| 帶身分的 runtime log／trace／設備讀回 | 某次操作走到哪個可觀測點，或讀回時回報什麼。 | 沒記錄的步驟未發生，或一個 SDK receipt 等於實體成功。 |

文中通用 C#、事件、DI、例外、Activity／logging、Stopwatch 和 JSON 行為引用 Microsoft 官方文件；介面責任與 ICD 閱讀引用 NASA 系統工程文件；規範用語和 JSON 格式引用 RFC Editor 的標準原文；重試風險另引用 AWS Builders' Library 與 Google SRE 原著。這些是通用工程依據，不是公司產品來源。因產品及版本尚未提供，本系列沒有引用任何特定廠商 SDK 來推斷公司行為。

正文主要來源：

- [NASA：Interface Management](https://www.nasa.gov/reference/6-3-interface-management/)
- [NASA：Interface Requirements Document Outline](https://www.nasa.gov/reference/appendix-l-interface-requirements-document-outline/)
- [Microsoft：C# delegates and events](https://learn.microsoft.com/en-us/dotnet/csharp/delegates-overview)
- [Microsoft：.NET dependency-injection guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines)
- [Microsoft：distributed tracing concepts](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-concepts)
- [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119.html) 與 [RFC 8259](https://www.rfc-editor.org/rfc/rfc8259.html)
- [AWS Builders' Library：Making retries safe with idempotent APIs](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/)
- [Google SRE：Addressing cascading failures](https://sre.google/sre-book/addressing-cascading-failures/)

## 主題交接

本系列的 `audio-and-device-boundaries` 僅說明「控制回覆不替另一條資料路徑作證」及證據應停在哪個 API 邊界：本機麥克風／喇叭與 media handler 交 Ozeki；SIP／SDP／RTP 交 VoIP；PTT 發話條件和狀態交 PTT；空中鏈路與 RF 品質交無線電。這裡不重講 PCM、SIP/RTP 封包、PTT Grant／Ready 或 RF 公式。若故障跨兩條鏈，先保留共同 operation 身分，再到各專題沿自身責任鏈查證。

介面文件中的網路 byte framing 與 RS-232 內容只用於指出文件要回答的問題；詳細 Socket、Wireshark、網路及 RS-232 操作分別留在已重寫的對應系列。

## 跨系列重複與篇章合併判斷

本輪對照 Ozeki／ICS、VoIP、PTT、無線電主題後，Integration 章節保留的是通用軟體維護責任，沒有搬入其專有機制：

| 接近的內容 | Integration 的範圍 | 交給相應系列的內容 | 判斷 |
| --- | --- | --- | --- |
| 通用操作呼叫與設備狀態 | 追 service、wrapper、SDK 邊界，記錄回傳與狀態來源。 | PTT 如何取得發話權、Grant/Ready 和釋放狀態。 | 通用 request/session 不能取代 PTT 流程；僅在共同操作圖標示交接。 |
| 控制與連續資料路徑 | 判斷一個 API receipt 不替另一條路徑作證。 | Ozeki 的本機媒體元件與生命週期、VoIP 的 SIP/SDP/RTP、無線電鏈路與品質。 | Integration 只建立路徑界線；音訊章保留為「證據在哪個邊界停下」的獨立診斷任務。 |
| 狀態與恢復 | 通用 client/session 世代、逾時後未知結果、owner 生命週期。 | PTT 設備狀態機及實際發話順序；radio repeater／RF 行為。 | 機制細節不重寫；不同責任需要不同事件、身分和觀測。 |
| 觀測與故障定位 | API 版本、程式映射、callback 身分和證據範圍。 | Wireshark 封包判讀及 radio 測量／鏈路診斷。 | Integration 不教 dissector、RTP stream 或 RF 測量操作。 |

九個既有篇章間也有刻意的相鄰分工：首篇只建立全路徑；第二篇追實例和 owner；第三篇查契約；第四、五篇分別處理事件處理與狀態世代；第六篇界定外部效果證據；第七篇決定恢復政策；第八篇保存可關聯證據；末篇整合診斷。它們共用 D1／60，是用同一組身分和交接逐步深入，不是每篇重講同一份 FAQ。

第六篇表面上與 Ozeki/VoIP 相鄰，但學習任務是跨產品通用的「SDK Accepted 是否足以證明效果」；若併回首篇，正常流程、API 契約、callback 關聯和物理效果會擠進同一入口，讀者難以建立每一層證據範圍。經縮限到邊界與跨主題交接後保留其 URL。獨立 cross-topic reviewer 仍需檢查完整六系列是否有可移除重複；本紀錄只判 Integration 範圍，不據此宣稱其他系列也已通過合併審查。

## 自查及目前驗證狀態

- 九個既有 lesson ID／檔案路徑保留；九份正文全部升為 `contentVersion: 3`，移除 `studyTime`。本次沒有修改 `docs/content/catalog.json`。
- 九篇共 70 道練習，每題附判斷說明；表格欄數與 JSON 結構檢查通過。合成 SDK 簽章和 Lab 的範圍及彼此差異已明示；音訊章縮回責任邊界與跨主題導覽，不再重教媒體機制。
- 全站 `python3 tools/audit_content.py` 通過：126 篇、0 結構警戒。這是結構檢查，不宣稱學習成效。
- 在 `/tmp` 暫存副本中，按各正文版本同步 catalog 後跑 `node tests/smoke.cjs` 通過：16 個主題、126 篇及首頁／主題／完整教材／進度／規劃頁互動檢查。工作區原始 catalog 目前仍將本系列標為 v2，因此直接跑全站 smoke 會在 catalog／lesson 版本相等斷言失敗；整合者須在最終目錄整合時同步改為 v3，再重跑原工作區測試。
- 本次沒有變更整合 Lab 程式。當前工作階段找不到 `dotnet` 執行檔，未重新執行該專案；既有前版審查紀錄曾以 .NET SDK 10.0.401 驗證 Lab 情境，但不能視為本次新跑結果。
- 真實廠商版本、公司程式映射、設備效果、瀏覽器實際畫面及學習者理解尚未由此子任務驗收；網站發布與 GitHub Pages 更新也不在本子任務內。
