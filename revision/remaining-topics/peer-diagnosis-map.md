# 除錯方法與概念地圖：獨立教材審查

這份審查回答的是：沒有公司系統背景的讀者，能否根據正文給出的定義、條件、數字和原始碼，自己走出相同的維護推理。審查者不是這十篇的作者。2026-10-03 已完成正文閱讀、全部80題可答性檢查、13組陌生輸入獨立推算，以及兩個 .NET 教學模型的隔離編譯與執行。發現一處「測到整條保護路徑，卻誤稱測到某一條guard」的教材論述；作者已修正正文、題目並新增能區分專用guard的真實案例。審查者讀回並重跑修正版與故障注入，全部結案。

## 閱讀範圍與判斷方法

除錯方法六篇均巡讀學習目標、段落安排、練習問題與全部48份解答；全文細讀首篇〈證據、主張與可證偽假設〉、核心〈單向語音〉、末篇〈有限修正、負面驗收與維護交接〉，並額外全文讀〈時鐘、身份與因果時間線〉。概念地圖四篇全部全文閱讀，包含首篇〈控制、資料與聲音〉、核心〈從入口追一項功能〉及末篇〈系統維護地圖〉；全部32題和解答逐項檢查。

每道陌生題先重新列出單位、分母、身份或事件順序，再算數值，最後與作者答案比對。讀碼時追蹤實際更新分支，例如 LastRms 何時會保留上次值，以及 Dictionary 使用完整 key 時哪些guard會互相遮蔽。完整 evidence JSON 是核算和執行附件，這份文件負責說明發現與意義。

## 正文能建立的基礎與維護觀念

除錯方法從觀察、推論、假設開始，解釋必要條件為何不是充分保證，再給出計數器位置、分類是否互斥與比例分母。音訊章節先定義 PCM、端序、聲道、取樣率、RMS 和媒體時鐘，再推導 byte 長度、期限與五種零輸出機制。沒有要求讀者先猜公司 codec 或裝置；由探針位置還能分清「未到RMS計算」與「已測全零」。這些定義實際用於下一個診斷分支，並非只列名詞。

概念地圖從程式與處理程序、型別與物件、參照、引數、事件與狀態講起。完整 Lab 角色卡交代當前 session、目標、輸入範圍、Pending 與 Send 的順序、回呼和資源所有權。同步 callback 案例示範為何「事件一定在Send返回後」會丟合法結果；共享 publisher 案例示範關閉 B 為何只能退訂 B，不能釋放 C 還在使用的 adapter。模型輸出與實體聽見有明確界線，也不依 RCSCall/iCallAPI 名字虛構公司依賴。

## 陌生題的獨立核算

| 題目與給定條件 | 審查者重新推得的結果 | 結論用途 |
| --- | --- | --- |
| 證據 Q3：收12、舊身份拒5、接受7；接受後晚2、靜音1、播放4 | 12=5+7；7=2+1+4；7/12=58.333%，2/7=28.571% | 兩個階段的累計不能任意相加，兩種比例分母不同。 |
| 單向語音 Q3：16kHz、雙聲道、16-bit、20ms | 每聲道320樣本，共1280 bytes；若當單聲道則40ms | 格式錯誤會改變時間解釋，即使 byte 長度完全一樣。 |
| 單向語音 Q4：8k clock、timestamp0/160/320、buffer35、arrival30/60/82 | 期限35/55/75ms；第一提前5，後兩晚5/7ms | 收到全部資料不保證期限內可用。 |
| 時間線 Q3：Send250、Receive238、offset=-35±7 | 校正接收266–280ms；延遲16–30ms | 必須先換同一時間軸，不能用原始值算負延遲。 |
| 跨層 Q3：幀0/25/50/75/100/125/150，Ready60、Release130 | 只查Release送6幀，裝置接受3；同查Ready直接送3 | 送出與裝置准入是不同計數。 |
| 重現 Q5：每次獨立成功機率0.8，五次皆成功 | 0.8⁵=0.32768 | 五次未重現不足以證明沒有20%風險。 |
| 邊界 Q4：UI37.25%、裝置0–200、中點遠離零 | 74.5取75 | 型別一致不代表單位與刻度一致。 |
| 控制 Q4：樣本[300,-300,300,-300] | RMS=√90000=300 | 舊LastRms600不能當被guard拒絕的本次輸出。 |
| 追碼 Q3：Send→Callback→建立Pending | Callback時字典尚無工作；最後Pending，Applied0 | 事件在方法內同步發生時，Pending必須先存在。 |
| 追碼 Q4：Pending收到三次Completed | Callback3，Applied1 | 本地套用一次不證明遠端副作用一次。 |
| 地圖 Q3：B/C/D三訂閱，B關兩次、C關一次 | 訂閱數3→2→2→1 | D仍使用時借用者不能釋放共用資源。 |
| 地圖 Q5：B/C兩handler收到C身份結果 | handler2，只有C Apply1 | handler入口次數不能代替業務應用次數。 |

以上包含除錯方法七項數值/分類核算（證據Q3另分守恆與比例）、概念地圖六項數值或狀態推演；13組結果均與解答一致。完整中間值在 `peer-diagnosis-map-evidence.json`。

## 模型實際驗證與證據範圍

將兩個專案各自複製至 `/tmp/ics-peer-diagnosis-map`，以 SDK10.0.401 執行 `dotnet run -- all`，不在教材來源產生 bin/obj。除錯模型24個機制斷言、地圖模型修正後19個斷言通過，stdout逐字符合各自 expected-output。這些模型真的產生 PCM bytes、解碼與計算RMS、過身份與期限分支、操作事件與字典，而不是列常數PASS。

另在分開的暫存副本做兩個故障注入。除錯模型移除 session guard，regression 以 `FAIL repair_stale_negative` 非零結束；地圖模型移除「只處理Pending」限制，trace 以 `FAIL duplicate_result_once` 非零結束。原教材 source 保持未改。這證明指定斷言確實能發現這兩類錯誤，沒有擴張為所有並行、裝置或SDK錯誤都被驗證。

## 發現、修正與讀回

1. **專用session guard與完整key檢查被混成一個驗證。** 地圖〈邊界與契約〉原例題令R2存在但使用session1，實際字典key為(R2,2,B)。即使刪除專用session guard，TryGetValue(R2,1,B)仍查不到而拒絕。本審查在隔離副本實際移除 `value.Key.Session!=Session`，contracts仍全通過、exit0，證實該用例驗的是整條過期身份隔離，不能單獨證明專用guard。已請作者澄清冗餘保護與測試範圍，或補「歷史完整key確實存在」的可鑑別案例。〈有限修正〉Q3同樣需說明單獨改session在完整key字典中可能仍被後續擋住。作者已明確區分原用例只能驗整條隔離，新增歷史(R1,1,B)仍存在Pending的案例：重連後其他前置都成立，只有session guard能拒絕。修正版all實跑19條件通過、stdout符合expected；再僅移除session guard，contracts以 `FAIL historical_pending_session_guard` 非零結束。修正版題目還明示需查實際lookup與歷史保留政策。**已修正並讀回結案。**
2. **基礎術語的字面錯誤。** 〈控制、資料與聲音〉goal原為「程式、程式」，正文其實正確分出program與process；要求goal同步改為「程式、處理程序」。〈有限修正〉使用「不變數」稱invariant，要求改為「必須維持的條件／不變量」，避免誤認程式常數。**已修正並讀回結案。**
3. **題目範圍的措辭。** 證據Q3說「同session共收到12」，接著有舊session拒5，容易讓初學者誤以為身份自相矛盾；建議寫同一接收窗口。數值推理本身成立。**已修正並讀回結案。**

## 官方資料抽查與限制

本次獨立再次開啟 [Microsoft C# events](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/events/) 核對publisher/subscriber與同步handler；開啟 [WAVEFORMATEX](https://learn.microsoft.com/en-us/windows/win32/api/mmreg/ns-mmreg-waveformatex) 核對取樣率、通道、位深與block alignment；[C4 container](https://c4model.com/diagrams/container) 核對container尺度不是Docker產品名；[Google SRE除錯章](https://sre.google/sre-book/effective-troubleshooting/) 核對根據觀察建立候選並用受控改變區分。教材是原創完整Lab推導，未把來源內容大量翻譯當正文。

所有實跑結果止於單執行緒記憶體模型與邏輯時間，沒有操作公司程式、廠商DLL、RTP/SIP網路、RF或真人音訊。完成這些章節能建立追讀、取證與有限修正的方法，但公司系統的owner、回呼執行緒、協定和裝置驗收仍須各自查證；這些界線在正文已清楚教讀者如何繼續，而非用未知替代基礎講解。
