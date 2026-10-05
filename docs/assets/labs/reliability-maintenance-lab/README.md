# C# 可靠性觀念與維護追讀

這是自行建立的 Lab 模型，沒有 SDK 或設備依賴。以 .NET 10 SDK 執行 `dotnet run -- all`；也可把 `all` 換成 deadline、retry、health、queue、ordering、race、async、evidence。`dotnet run -- async 500` 會把合成的 long-handler 成本改為 500 個邏輯毫秒，其他工作不變，方便比較排隊延遲。

逐一讀 Program.cs 對應函式，先預測輸出，再執行比較。除 race 使用真實執行緒外，時間與外部行為都是固定邏輯模型，不是性能測試。async 是單執行緒派送的邏輯演算，Console 不含 WPF UI，不能用它實測畫面。

所有模式都有不變條件檢查，失敗會以非零結束。修改一個條件後重跑，需解釋為什麼不變條件被破壞；修正後不能只刪掉檢查。

此例的冪等記錄僅存於單執行緒記憶體，未實作交易或持久化，也不提供真實「恰好一次」承諾。公司 .NET Framework／SDK 的支援與執行緒規則需另核對。

每個模式的基準輸出見 expected-output.txt。固定亂數只用於退避示意，不代表真實負載分佈。首次執行會建立 bin／obj，這些是本機編譯檔，可刪除後重建。
