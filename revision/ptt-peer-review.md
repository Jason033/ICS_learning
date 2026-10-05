# PTT 八篇教材獨立審查

審查日期：2026-10-05。範圍是 PTT 主題八篇 v3、練習答案與 `ptt-maintenance-lab`。依各篇明示的教學契約、程式模型及引用文件檢查流程推理；不把章節數、題數或文字長度當成品質證明。初審後按審查意見，在 PTT 入口篇補上來源到下游的通用交接路徑及 Ozeki 可選連結，也修正介面盒對映表一處錯字；其他教材未改。

## 審查結論

八篇都能把 PTT 控制、裝置回報與內容准入分開，並反覆標出哪些是教學模型、哪些必須查產品文件。MCPTT 例子有清楚限制在特定標準流程；Busy、Grant、Ready、Tx、Release 等狀態也沒有被宣稱為所有設備通用。練習大多能由本文模型或題目明示的合成資料推出，核對的時間與計數答案一致。

初審發現 PTT 入口的內容路徑從「語音交接」才開始，沒有把來源 callback／內容區塊如何到 PTT 閘門與下游 sink 說出來。已在〈按下 PTT 之後，如何確認真的發射〉補上一條不依賴特定 SDK 的來源至 sink 路徑和可選 Ozeki 直達連結，並把該篇故障練習答案補成逐段追查。這只補交接橋樑，沒有重教 Ozeki API。另修正介面盒對映表的小 typo。除此之外沒有發現需要改寫整篇的高優先問題。

## 逐篇判斷

| 篇章 | 判斷與已核對內容 | 高優先問題／必要最小修正 |
|---|---|---|
| [按下 PTT 之後，如何確認真的發射](../docs/content/lessons/ptt--press-to-tx.json) | 入口流程涵蓋按鍵、意圖、准入、可選許可、設備控制與回報、內容交接、對端觀測；把「方法返回」「設備 TX」和「對端聽到」分成不同證據。新增來源／擷取端 → callback／佇列內容區塊 → PTT gate → 下游 sink 的通用路徑，並在正文及延伸閱讀連到 Ozeki 來源章。MCPTT 明確只作特定標準的例子。 | P1 已處理。練習答案也補上沿來源、gate、sink 逐段核對的步驟；沒有引入特定公司 SDK 契約。 |
| [Ready、Busy、TX、RX 狀態](../docs/content/lessons/ptt--state-machine.json) | 狀態、事件、guard、副作用及操作 Epoch 連在一條流程上；有 Grant/Ready 晚到、Release 交錯及多執行緒競態。本文說明單執行緒 Lab 不代表公司程式安全；練習答案可由 Lab 的狀態契約推出。 | 未發現需修改的高優先問題。沒有強迫所有實機採用 Lab 狀態名稱，並提醒真實程式可能另有 Releasing、Unknown 或 Fault。 |
| [通道忙碌與發話權](../docs/content/lessons/ptt--busy-and-grant.json) | 清楚區分本地 Busy Lockout 與可選的遠端 Grant；請求方法返回、Pending、Granted、Denied、逾時各自代表什麼有走完。MCPTT 被限縮為 floor-control 例子，Zetron 例子也標明是特定機型／模式。 | 未發現需修改的高優先問題。練習沒有把 Busy=false 當作授權，也沒有把無結果的逾時誤寫成遠端拒絕。 |
| [介面盒、COR／COS 與燈號](../docs/content/lessons/ptt--interface-box.json) | 軟體維護者需要的方向、asserted 與邏輯 high、極性、模式語義及觀測限制都有交代；明確不提供未知設備的接線方案。Zetron 常規與 trunking 模式的 COR 意義，和官方手冊相符。低有效練習和 20ms 合成取樣答案可逐步推出。 | 高優先硬體缺口為無。第 2 節對映表 typo 已修為「細節見音訊主題」。 |
| [PTT 與音訊時序](../docs/content/lessons/ptt--audio-timing.json) | 區分內容時間與牆鐘服務時間，說明 Ready 前丟棄、緩衝、Release 後尾段及跨機時鐘限制。四個 20ms 區塊、Ready=50ms 的整塊准入規則有明說；沒有把模型的 40ms 丟失推廣為實機必然值。 | 不必重複入口篇的來源到 sink 橋接；依本篇範圍從內容到達閘門的時間開始。 |
| [放開、逾時與重連後狀態](../docs/content/lessons/ptt--release-and-recovery.json) | 分別處理本地取消、遠端資源釋放、裝置停止及 Unknown；也涵蓋取消與 Grant 交錯、舊 `finally` 誤停新 owner、重連不等於停止確認。BFCP/RFC 8855 的取消與釋放例子確實屬會議 floor-control，文中也明說不是通用電台協定，並提醒依實際協定核對。 | 未發現高優先問題。RFC 例子與 MCPTT 不同，但範圍已標出，不應把 BFCP 訊息或欄位直接套到 PTT SDK。 |
| [把 PTT 紀錄寫成證據](../docs/content/lessons/ptt--observability-and-logs.json) | 事件收到與採用分開，operation/session/device 身份有不同用途；期望值、最後觀測與未知狀態也分開。跨機時間不在無同步證據時硬算。累積計數題的增量與 UI 延遲答案正確。 | 未發現需修改的高優先問題。首段交接明確不是對端播放；計數也明確不是 RF 或接收證明。 |
| [PTT 整合除錯練習](../docs/content/lessons/ptt--ptt-troubleshooting.json) | 兩個案例串接前面責任：舊會話 Ready 被拒，以及停止未知時舊清理可能影響新 owner。案例資料、時間及 Lab 能證明到哪裡都有標示；練習要求同時保留正向路徑與必要拒絕條件。 | 未發現需修改的高優先問題。案例中的 `Ptt=true`、`ContentAvailable`、`FrameDropped` 不足以定位 RF，推理與合成 Lab guard 一致。 |

## 音訊來源到交接：初審缺口與處理

初審時 PTT 已教內容何時被閘門准入或丟棄，卻未說明來源資料如何到達閘門。入口篇現在補成通用路徑：來源／擷取產生資料 → callback 收到或切出內容區塊 →（若有佇列）consumer 交付 → PTT gate 依本次狀態判斷 → 下游 media/device sink。正文也提醒實際型別可能不同，需沿相鄰方法與事件追蹤，並用同一 operation/session 關聯觀測。

入口篇的「延伸閱讀與依據」現有一個直接連到 GitHub Pages 的 Ozeki〈從本機來源追到播放端：資料流與元件責任〉連結；該章補來源、格式及路由細節，仍是可選閱讀，PTT 不依賴它作前置。若要深入音訊格式與資料量，也可讀[格式與資料量的維護判讀](../docs/content/lessons/ozeki--registration.json)。

## 數值與練習答案核對

| 核對項目 | 正文／Lab 條件 | 核對結果 |
|---|---|---|
| 介面盒穩定視窗 | true@0、false@4、true@8；穩定需 20ms，只有呼叫 `Sample` 才更新 | 27ms 距最後候選值 19ms，仍 false；28ms 滿 20ms，轉 true。答案正確。 |
| Ready 前首段 | 20、40、60、80ms 交付 4 塊，每塊含 20ms；Ready=50ms；整塊以交付時間判定 | 20、40ms 兩塊共丟 40ms；60、80ms 兩塊准入。答案也指出 60ms 區塊含 Ready 前樣本但模型不裁切，正確。 |
| 尾段服務 | 3 塊 × 20ms 內容；只有額外指定每塊服務需 20ms、無其他等待才可換算 | 40ms 尾窗最多完成 2 塊，第三塊需截斷或記未完成；不把內容時長誤當牆鐘服務時間。 |
| PTT Lab 計數 | `Frame` 每次輸入 160；Ready 前一筆、Ready 後一筆；`SentSamples`/`DroppedSamples` 為物件生命週期累計 | 合法輸出為 `160/160`；Timing 案例 1 筆准入、2 筆丟棄，為 `160/320`。與 `Program.cs` 及 expected output 一致。 |
| 時間分段 | 按下 10ms、Grant 40ms、Ready 90ms、首筆交接 100ms | 30ms + 50ms + 10ms = 到本地交接 90ms。未把它稱為對端延遲。 |
| 累積計數差值 | 開始 Sent=160/Dropped=320，結束 480/480 | 本次增量為 Sent=320、Dropped=160。 |
| UI 延遲 | Ready 收到 90ms、採用 92ms、UI 170ms | 接收→採用 2ms；採用→UI 78ms。算術正確，責任切分清楚。 |

## 查核的一手／官方資料

- [ETSI / 3GPP TS 24.380 V18.6.0, Release 18 (2024-07)](https://www.etsi.org/deliver/etsi_ts/124300_124399/124380/18.06.00_60/ts_124380v180600p.pdf)：標題及流程章確認這是 MCPTT 媒體平面控制規格；文件明列 floor request、pending、granted/denied、PTT release 與媒體事件。教材把它作特定標準範例並限制不可直接套用，恰當。
- [RFC 8855 §10.2, RFC Editor](https://www.rfc-editor.org/rfc/rfc8855#section-10.2)：BFCP FloorRelease 可用於取消待處理 floor request 或釋放已授予 floor，並使用請求識別及交易關聯；教材描述符合規格，且將其定位為會議資源控制。
- [Zetron Model 6 Station Product Manual 025-9157, M.1](https://partner.zetron.com/wp-content/uploads/2024/08/025-9157-M6-Station-Prod.pdf)：手冊說常規模式下 COR 指示載波／通道忙；trunking 模式以 COR 輸入表示 channel grant，邏輯意義隨模式改變。教材沒有將其泛化。
- [Motorola Solutions DTR 600/700 使用手冊](https://www.motorolasolutions.com/content/dam/msi/docs/business/_documents/user_guides/mn004869a01-aa_enus_dtr_600_dtr700_limited_keypad_portable_radio_user_guide.pdf)：Talk Permit Tone 是該機型在 PTT 後的特定提示，手冊要求提示結束後再說話。教材將它標示為產品限定例子。
- [Microsoft C# `lock` 文件](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)：共享程式區段必須共同遵守同一把鎖；教材也正確說鎖不保護外部設備或未使用該鎖的 callback。
- [.NET 10 `CancellationTokenSource.Cancel`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancel?view=net-10.0) 與 [`System.Timers.Timer.Elapsed`](https://learn.microsoft.com/en-us/dotnet/api/system.timers.timer.elapsed?view=net-10.0)：分別支持「Cancel 是合作式取消要求」及「已排隊的 Elapsed 可能在停止後仍執行」的維護提醒。
- [.NET 10 `Stopwatch`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-10.0)：用於教材所說的本地經過時間量測；跨設備時鐘仍須另查同步誤差。

本次來源核對只驗證教材引用的通用或產品限定事實，不能替代公司專案使用的設備、韌體、SDK 版本或接線契約查證。
