# PTT 第三版作者審查：將發話控制連成可維護的流程

2026-10-05 更新。新版讀者一般資工概念大致理解，主要需要把意圖、准入、設備控制、內容交接及結束串起來，能閱讀既有程式與判斷故障。正文採工程同事語氣；硬體設計、音訊格式及協定百科不當前置課。

本系列先前已有三篇第三版，本次全文重寫其餘五篇，並局部修正首篇／Busy 與 Grant 的責任分界及來源。八篇均 `contentVersion: 3`，沒有 `studyTime`；原檔名、網址及下載資源保留。不以篇幅或題數宣稱能力已達標。

## 順序與各篇用途

以下是系列內的概念順序建議，未修改全站目錄。具體維護問題可以直接進入相關篇章，再回查必要前文。

| 順序／lesson ID | 工程問題及閱讀成果 | 接續責任 |
|---|---|---|
| 1 `press-to-tx` | 按鍵到設備控制與內容交接；判斷成功證據止於哪裡。 | 整體交接需要哪些狀態。 |
| 2 `state-machine` | Held、Grant、Ready、Epoch 的合法次序；阻止舊事件啟動新操作。 | 誰決定准入和許可。 |
| 3 `busy-and-grant` | 本地 Busy 政策與遠端 Grant；名稱不等於設備語義。 | 介面與工作模式如何產生這些狀態。 |
| 4 `interface-box` | 語義要求、SDK 寫入、極性、模式與回報；辨識雙反向及錯誤模式對映。 | 控制就緒後的內容窗口。 |
| 5 `audio-timing` | 內容產生／交付時間，首段丟棄、暫存、有界尾段及代價。 | Release 結束內容與控制。 |
| 6 `release-and-recovery` | 取消等待、解除控制、隔離舊清理、保留停止 Unknown。 | 沿生命週期建立紀錄。 |
| 7 `observability-and-logs` | 收到／採用、來源／目前身份、期望／最後觀測；分段等待。 | 有身份的證據用於綜合案例。 |
| 8 `ptt-troubleshooting` | Keying 無內容與停止失敗兩份合成資料：缺口、可反駁檢查、修正驗收。 | 回到實際程式契約。 |

第三篇遠端 Grant 是條件性路徑，沒有仲裁服務時不硬套；第四篇實體介面、極性及去抖只對相關接口適用。第七、八篇可直接作維護入口，不需先學全套遠端協定或硬體。

## 技術界線及官方來源

PTT 保留准入、就緒閘門、身份、清理及停止核對。本機採集、裝置選擇與媒體處理由 Ozeki／音訊深入；SIP／RTP 及封包證據由 VoIP／Wireshark 深入；RF 品質由 Radio 深入。可靠性深入通用超時、重試與取消，PTT 只教它們如何作用於本次發話及設備 owner。

2026-10-05 重新讀取官方資料並限縮範圍：

- [ETSI TS 24.380 V18.6.0](https://www.etsi.org/deliver/etsi_ts/124300_124399/124380/18.06.00_60/ts_124380v180600p.pdf)：特定 MCPTT floor-control。首篇與 Busy 篇只借用請求不等於許可、許可有持有者及生命週期；其訊息、計時器及狀態不直接映射公司 SDK。兩篇已移除 BFCP 繞路。
- [Zetron Model 6 025-9157M.1](https://partner.zetron.com/wp-content/uploads/2024/08/025-9157-M6-Station-Prod.pdf)：PDF 第13–14、74頁常規／集群模式及 PTT、COR 角色。網址上傳日期不代表手冊版本；僅作模式對映例子，未複製針腳配置或推定公司型號。
- [Motorola DTR600/700 指南](https://www.motorolasolutions.com/content/dam/msi/docs/business/_documents/user_guides/mn004869a01-aa_enus_dtr_600_dtr700_limited_keypad_portable_radio_user_guide.pdf)：等待 Talk Permit Tone 結束再說話的產品規則；不推廣成所有設備的提示契約。
- [RFC 8855 §10.2](https://www.rfc-editor.org/rfc/rfc8855#section-10.2)：結束篇保留待處理請求取消、已授予資源釋放與請求身份的可遷移機制。它是會議資源協定，不是 PTT 線路或 RF 停止確認；綜合篇僅引用同一機制背景。
- Microsoft [Cancel](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancel?view=net-10.0)、[Timer.Elapsed](https://learn.microsoft.com/en-us/dotnet/api/system.timers.timer.elapsed?view=net-10.0)、[Stopwatch](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-10.0)、[C# 布林運算](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators)：取消只是要求、停止後已排隊回呼仍可到達、經過時間及極性演算。不藉 .NET 文件宣稱外部設備已停止。

所有時間線、ID、對映與故障資料為原創合成例子；設備動作欄位逐一標明觀測來源，不在 bool 上自行增加物理意義。

## 全文自讀、修正及完整性

作者已按建議順序讀回八篇；本次五篇正文及全部答案全文自讀。這是作者檢查，不代替另一位審查者或實際讀者試讀。

自讀修正了四項會改變判斷的問題：60ms 交付塊含40–60ms內容，必須標明整塊准入假設；單刪 Ready 條件仍可能被 AudioOpen／State 阻止，故錯誤示範改成整段只查 Ptt；Release 後案例改成新的 Press，避免暗示同一意圖仍有效；NotReady 報告改成目前沒有本次有效 Ready，避免混用不同操作證據。

介面篇刪除驅動電路與額定計算，保留方向、極性及相容條件；內容篇不重教 PCM／RTP。結束篇補足舊 finally 可能誤停新 owner，以及先比較身份、稍後寫全域停止仍有競態。觀測篇指出 Lab 沒保存傳入的舊 epoch，不能由該日誌重建來源。綜合篇以完整紀錄、guard 及兩種可區分根因串起修正，不僅提供矩陣。

題目所需極性、持續窗口、准入、服務速率、時間基準、身份、Unknown 與 owner 條件都在正文交代。答案解釋推理與限制，不要求猜公司架構。

## 本輪驗證與限制

本輪做八篇 JSON／結構、資源路徑、版本、表格欄數與範圍化 diff 檢查，重算：27−8=19／28−8=20ms；Ready50 下前兩塊丟40ms內容；固定服務20ms的三塊需60ms、40ms只完成兩塊；Lab timing准入160／丟320；10→40→90→100 分段30／50／10ms；計數增量320／160與 UI等待78ms。完整性由交接及條件是否連起來判斷，不由結構、字數或題數代替。

`ptt-maintenance-lab` 原始碼未修改，八模式仍是單執行緒、純記憶體、呼叫者給虛擬時間。2026-10-03 編譯紀錄屬前版驗證；本輪 `/tmp` SDK 不存在，**沒有重新編譯／執行**。整合者另留當次重跑與頁面驗證紀錄。綜合篇 guard 是原檔局部摘錄，不是獨立程式。

即使執行環境完整，仍未知的產品條件包括：公司 SDK／DLL 版本、回傳契約、Grant／Ready 是否存在、回報身份及新鮮度、內容交接邊界、型號／模式／極性／額定、停止確認及冪等性、斷線保護、重連與自動重新發話政策。模型沒有音訊、網路、GPIO、RF、首段緩衝、有界尾段或背景看門。FrameSent、Idle、計數或 PASS 不能證明設備發射、停止或對端聽見。

待整合：另一作者獨立審查、決定目錄順序、同步 catalog 版本、重跑模型及頁面驗收。作者未改全站 catalog、應用程式、測試、其他主題或發布狀態。
