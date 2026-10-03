# PTT 意圖、狀態與時序模型

本專案讓你讀 C# 程式並理解：按鍵意圖、PTT 控制、發話權、電臺就緒與音訊准入是不同事實。所有型別以 Lab 開頭；沒有裝置 SDK、GPIO、網路、音效卡或 RF。

安裝 .NET 10 SDK，在此資料夾執行 `dotnet run -- all`。可單獨執行 `press`、`interface`、`state`、`grant`、`timing`、`release`、`evidence`、`troubleshooting`。所有時間是呼叫者給的單調虛擬毫秒；不使用實際等待。

模型政策：Busy 只阻擋新的 Press；RequireGrant=true 時先等待同 Epoch 的 Grant，再 Keying，直到同 Epoch 的 Ready 才允許音訊。Frame 以四個旗標及 Tx 狀態共同准入。Release 立即關音訊／PTT、取消意圖、進 Idle 並改世代。MaxHoldMs 從 PressAccepted 起算，不是原廠 TOT；到期進 Fault，必須 Release 後新 Press 才可重啟。計數器累積整個物件壽命，非每次按鍵重設。

對照方法：先預測旗標／狀態與樣本數，再執行情境。一次改一個 guard 或次序，看指定斷言 FAIL；復原後 all PASS。判斷標準是當前意圖與許可權有效才准入、舊結果不能重啟、結束後模型輸出皆關閉。

它只能驗證軟體模型。Ptt=true 是期望輸出，RadioReady 由測試注入，不證明真實電臺 TX、RF 或對端聽到。電氣極性、電壓、驅動能力、針腳、忙碌語意、實體回饋與釋放故障須由特定硬體檔案及授權量測核對。

`Program.cs` 放模型與八情境，`.csproj` 設定標準函式庫 .NET 10；教材章內提供另外兩個逐步程式案例。新主題若要擴充，新增明確事件及契約，避免偷偷把 Lab 假設當實際裝置行為。

本模型的所有入口必須由同一執行緒依序呼叫，now 不得倒退。它未模擬並發、背景計時、網路確認、尾音排空、真實取樣或設備狀態查詢。Tick 進 Fault 後重複檢查不再新增故障世代；計時數字僅由呼叫者注入。

已在獨立臨時副本用 .NET SDK 10.0.401 編譯並跑八模式，`expected-output.txt` 保存實際輸出。程式中共有十九項正常斷言和重複逾時的額外邊界檢查。刻意把 Frame 只以 Ptt 准入會使 `readiness_gates_audio` 失敗；復原後 all 全部通過。這驗證本地规则，不是設備相容性驗證。
