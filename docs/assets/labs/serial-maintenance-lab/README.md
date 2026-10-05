# 串列程式維護教學模型

這是原創的 .NET 10 Console 專案，使用標準函式庫；不開 COM 埠、不載入 `System.IO.Ports`、不接設備，也不模擬真實線路。它只用固定輸入說明程式如何形成命令、估算UART資料時間、累積訊息、處理逾時，以及區分讀取通知和完整訊息。

在本資料夾執行 `dotnet run -- frames`。可用模式為 `layers`、`uart`、`frames`、`timeout`、`events`、`interfaces` 和 `diagnose`。`layers`、`frames` 及 `diagnose` 支援 `--fault`，用來比較一個明確改變的條件。各章列出建議執行的模式和驗收內容。

`LabParser`使用自訂教學框架：`AA`起點、`LEN`只計`CMD+DATA`且範圍1–32 bytes、最後一個byte為`LEN/CMD/DATA`的XOR。這不是RS-232標準或任何廠商協議。解析器示範有界累積和錯誤後重新找起點，不提供交易關聯、硬體中斷、正式協議恢復策略或安全保證。

輸出中的 `PASS` 只表示本次固定輸入符合預期。要判斷公司程式，仍需沿實際呼叫、物件生命週期、設備文件和允許取得的觀測資料重建流程。
