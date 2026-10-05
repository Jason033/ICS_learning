# 可靠性第三版獨立審查

2026-10-05。審查結論：九篇的核心機制與端到端責任連接成立，並非只有名詞或診斷矩陣。等待逾時保留遠端未知，重試保留同一意圖，恢復重新建立狀態來源，再用佇列、版本範圍、同步和派送責任約束結果。發布前仍需修正幾處會影響 C# 執行順序判斷及變因練習的細節，下列建議不要求整套重寫。

審查者未修改 lesson、Lab、catalog 或應用程式。本次讀完九篇的 intro、前置、目標、每節正文、所有程式／表格及全部題解，並對照 `reliability-maintenance-lab/Program.cs`。數值與執行順序以靜態推演核對，沒有在本輪宣稱重新編譯、執行模型或測試 WPF。這份紀錄與作者自檢分開，不代替真正讀者試讀或公司的設備驗收。

## 逐篇判斷與銜接

| 篇章 | 原理及 C# 推導的判斷 | 題解與相鄰責任／需修項 |
|---|---|---|
| `timeouts` | 入隊、送出、接受、完成與畫面分開；三個遠端世界能解釋同一 timeout；總期限與單次預算計算完整。 | 題目可由正文推出，沒有把逾時當未執行。WaitAsync 的「新 Task」措辭需修；變更 deadline 的模型文字需同步。 |
| `retries-and-idempotency` | 意圖與 attempt 區分清楚；Dictionary 單執行緒／崩潰空窗都有交代；退避窗的溢位及 Random 上界有必要說明。 | 24次、510ms、剩240ms等答案成立。正確承接未知結果，沒有宣稱帶ID就能去重；正文錯誤判讀依契約，不應改成所有 Exception 都重試。 |
| `liveness-and-recovery` | TCP、應用探測、業務 Ready 層次與最後觀測新鮮度完整；心跳相位、恢復遲滯、快照空窗、備援所有權都有機制。 | 4.5秒及舊Pong題成立。對應 timeout 的證據界線與 ordering 的世代／快照。沒有把連上當完全恢復，不需增加全套分散式演算法。 |
| `queues-and-backpressure` | 有來源／消費者、waiting／in-flight、容量政策、停止及 Little’s Law 的範圍限制。Channel成功語意有正確限縮。 | 入7→立即限10→出4，剩3/6/6/6、累計丟6正確；沒有混用正文另一種先服務再限容量的模型。環形緩衝「行動資料」需修詞。 |
| `event-ordering` | 來源、操作、事件、世代、版本分工完整；完整快照與增量缺口、回繞及命令順序的責任都有區分。 | 模差9／128、跨來源不可比與快照先後題成立。consumer與await那句需補條件；volatile反例避免對long作不合法宣告。 |
| `concurrency` | 參考共享及閉包先定位物件，再教讀改寫、同鎖快照、owner、Semaphore、Monitor及等待圖。完整類別與tuple有解釋。 | 4→5而非6、未取得許可不能Release、兩consumer醒來須while，均可由正文推出。第一goal若要承諾並行／平行差別，應補一句或收窄goal，毋須新增一堂課。 |
| `async-event-loops` | async前段／未完成await／已完成await、Task、UI脈絡與同步死結完整；事件轉Task、WhenAll／Any及停止也有責任界線。 | 300/310、等待260/210與UI互等題正確；已完成await不必暫停的答案有官方支持。500→50變因的硬編碼Check需處理。 |
| `observability-and-load` | 成功分母、時窗、counter差、百分位、錯誤鏈、重試attempt與負載模式逐步建立，並非只列監控工具。 | 50/25/115ms、P90=9／平均14.5成立。病例A的新意圖量與attempt量需明確區分，避免數字與容量推論看似矛盾。 |
| `reliability-troubleshooting` | 同一操作鏈、競爭假說、第二輪資料、有限修正、反向邊界與舊未知對賬完整；三資料包能整合前篇機制。 | 12秒36件、兩不同EventId的lost update與UI死結答案成立。「可關聯絡統記錄」需修為可關聯的系統紀錄。 |

## 發布前優先處理的具體問題

### R1：不要讓 await 變成「同一consumer自動取下一件」

位置：`event-ordering`〈命令順序與狀態更新如何在程式中守住〉首段。原句「一個consumer依序取工作，也可能在await間啟動其他工作」缺少決定條件，可能讓不熟 C# 的讀者以為直接 await 消費迴圈也會把多筆工作啟動出去。

最小修正：明示同一迴圈若 `await ProcessAsync(item)`，會等待這筆完成纔到下一輪；只有未等待所啟動的 Task、另一個consumer或其他事件入口重入，纔可能重疊。也可用兩個兩行對照片段補強，不需展開新的框架。await 是暫停目前方法，不會自行推進它的下一輪；這與 async 篇已有的說明一致。[Microsoft await reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/await)

### R2：健康變因仍會撞上舊數值的驗收式

位置：`async-event-loops` 最後一節，指示將long-handler成本500改50；Lab原碼卻仍 `Check(availableAt == 503, ...)`。照新資料推導，status從50執行到52，heartbeat從100到101，最終應101，因此改善情境仍會失敗。這是靜態推演發現，未聲稱實際執行過此變因。

最小修正：保留原基準503的檢查，明示變因應另以101作預期結果；或讓模型依參數產生驗收上下文。不要指示刪掉所有Check讓它通過。相同問題的較輕例子：timeout篇要求deadline改280，Lab最後文字卻固定「expired at 250」；改為輸出deadline變數即可。

### R3：量的是新意圖還是實際入隊嘗試，要在病例首輪標清

位置：`observability-and-load`〈由第二輪證據定位故障〉。病例A稱到達率5升20、服務仍30ms而queue增長；之後才說重試把attempt放大三倍。

若同一consumer每筆固定30ms且無額外成本，理想容量約1000/30=33.3件/s，20件/s本身不能證明長期過載；第二輪的60attempt/s才足以形成速率差。最小修正：首輪明示5→20是「新意圖」量，不是queue實際流入量；第二輪明示attempt變60/s及consumer條件，再用60>33.3解釋積壓。保留兩輪證據設計，不需換案例。

## 技術精確性與最小補修

1. `timeouts` 第7節稱 WaitAsync 產生「新 Task」。官方 API 明定回傳可能就是原實例；改成「取得代表受期限／取消條件控制之等候的 Task，可能與原 Task 同一實例」。等待者逾時不保證底層終止的主結論正確，應保留。[Task.WaitAsync .NET 10](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync?view=net-10.0)
2. `event-ordering` 的 Lab `Version` 是long，但C#不允許 `volatile long`，property也不能直接加該修飾詞。反例題可改成「即使每個欄位讀寫各自已原子化，整段檢查與更新是否一致？」保留原答案的跨欄位機制，不要求教完整volatile型別清單。[Microsoft volatile reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile)
3. `queues-and-backpressure`「減少行動資料的成本」改「減少搬移資料的成本」；末章「可關聯絡統記錄」改「可關聯的系統紀錄」。這些是誤轉詞，外行會讀成另一個概念。
4. 並行篇goal承諾「區分並行處理、平行處理」，正文已教不必同時也可交錯，但未明說平行。可把goal收窄成「追出可能重疊的執行路徑與共享物件」，或補一句平行是實際同時執行；不要為對齊goal重教多頁概論。

## Microsoft 來源與版本界線覆核

本輪核對以下官方來源；API頁使用`.NET 10`視圖，實際公司程式仍以目標框架、語言版本及套件為準。既有前置已標明WaitAsync為.NET 6以上、Channels需查目標框架及套件、WPF不等同Console；不應刪掉這些界線。

| 官方來源 | 本輪核對的語意／教材判斷 |
|---|---|
| [Task.WaitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync?view=net-10.0) | 回傳代表等待的Task，可能同一實例；等待逾時／取消與原工作結果不同。需修正「新 Task」措辭。 |
| [await](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/await)、[C# asynchronous programming](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/) | 已完成工作可立即續行；未完成則暫停目前方法。async例題兩種順序成立，consumer句需補條件。 |
| [Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) | Wait滿時TryWrite即時false，WriteAsync可等待；Drop模式含義不同，容器安全不等於業務順序。正文正確。官方亦指出.NET Core 3.0以上共享框架包含，其他目標須查套件。 |
| [lock](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)、[volatile](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/volatile) | object鎖提供同一鎖的互斥及同步，lock內不能await；volatile非複合原子化。本文object鎖不要求改成.NET9／C#13才引入的System.Threading.Lock。 |
| [Monitor.Wait](https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor.wait?view=net-10.0)、[SemaphoreSlim](https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=net-10.0) | Wait釋放／重新取得相同鎖後重查條件；Semaphore不強制Task／執行緒身份，呼叫者負責取得與歸還配對。正文while與try/finally範圍正確。 |
| [TaskCompletionSource](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource-1?view=net-10.0)、[TaskCreationOptions](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcreationoptions?view=net-10.0) | TrySet終態與呼叫安全不代替外層操作身份；RunContinuationsAsynchronously限制內聯續體，非業務排序。舊.NET Framework須留意該選項從4.6起可用。 |
| [WhenAll](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall?view=net-10.0)、[WhenAny](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenany?view=net-10.0) | 一組全完成與第一個完成不同；第一個可能fault／cancel，其餘不自行取消。正文沒有把WhenAny寫成最先成功，不需改。 |
| [WPF Threading Model](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model) | Dispatcher有執行緒歸屬和優先序，同步等待其Task可死結。模型明示無優先序的邏輯派送，不冒充WPF實測。 |
| [Exception-handling statements](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/exception-handling-statements)、[Random.Next](https://learn.microsoft.com/en-us/dotnet/api/system.random.next?view=net-10.0) | throw;保留堆疊、Random上界不包含。觀測與退避正文正確，來源列表可補這兩項精確API／語言主張。 |

不要求在所有篇章重貼一整套.NET版本表；使用到的新API、語言語法或宿主限制，在首次程式／前置短註並鏈結官方契約即可。

## 數據、題解及明確保留之處

用正文規則另外重算題中數據，沒有只以字數或JSON通過判斷：總期限340、單次120、退避40得到0/160/320三次起點、末次20ms；7入、限10、4出四輪得到3/6/6/6與丟6；模序250→3差9、250→122差128；十筆1至9加100總145、平均14.5、nearest-rank P90=9；UI300ms後A/B在300/310開始，等待260/210；每秒淨增3件、12秒36件。中間步驟與答案一致。

不需要為了「中級」增加不相關的硬體、協定細節或監控平臺安裝。Dictionary明確為記憶體單執行緒模型；StateGate明示完整快照、單owner、無回繞；Recovery是條件示範；async是邏輯派送而非WPF實測。這些假設完整且有助讀公司程式，應保留。

重複主要是各篇獨立入口與末章整合所需的Unknown、ID、世代及恢復邊界，尚未擠掉關鍵機制。可刪恢復篇「術語不是為了加深難度」這類不增加機制的自我說明；不必機械縮短所有篇或刪去必要反向邊界。把goal與recap重複作為網頁呈現欄位處理即可，不拿它當正文原理缺口。

目前沒有發現必須整章補寫才能理解的機制斷層。上述修正完成後，仍須由整合者讀回對應段落、重新驗證模型及變因、確認頁面／目錄。公司API、設備動作、取消保證、冪等保存期和真實排程仍未知，不能從本教材或Lab通過推廣為公司系統正確。
