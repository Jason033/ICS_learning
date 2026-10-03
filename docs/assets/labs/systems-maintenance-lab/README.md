# 作業環境觀念與程式維護追讀

這個專案用固定資料理解程序、服務、端點、裝置、時間與資源壓力。它不查私人程序、不修改服務、路由、權限、系統時間或驅動，不錄音、不連公司設備。教學 Lab 型別不是 Windows/Linux API，也不是廠商 SDK。

安裝 .NET 10 SDK，解壓縮後在此資料夾執行 `dotnet run -- all`；每個模式也能單獨執行，例如 `dotnet run -- time`。NuGet.Config 清除外部套件來源，只使用標準函式庫。Program.cs 是完整來源；expected-output.txt 是本次 Linux 下實際執行 all 的輸出，失敗斷言會印 FAIL 並非零退出。

| 模式 | 要理解的機制 | 固定基準 |
| --- | --- | --- |
| evidence | 按因果層檢查缺觀察和負向事實 | 程序確定缺席、後層未知仍是NoProcess |
| ownership | 正常/例外的擁有者清理 | 兩次Open都Close；FD五分鐘350 |
| startup | 設定、存取、就緒與有限等待 | 930ms首次看見Ready；2000ms資料超期限 |
| endpoints | 單表最長前綴和協定/位址匹配 | 140走LAB、70走VPN；回環不接LAN |
| devices | 緊密打包PCM幀/byte/ms及開啟條件 | 960frames=3840byte=20ms；缺資料30ms |
| time | offset符號、誤差區間、實例檢查 | 校正差22–30ms；不同世代不可相減 |
| pressure | CPU口徑與持續速率差 | 1核心當量=整機25%；20/120秒滿 |
| diagnose | 多層病例與錯誤分支 | 端點/資源界線、underflow15ms、時鐘校正 |

先手算正文例題，再追函式與斷言；只改一個條件，預測差異，再執行。正文亦提供錯誤注入，例如把double除法改整数或先按metric選路，應造成斷言FAIL，還原才完成驗收。模型固定速率和邏輯時間不是排程效能保證。

實際 Windows 服務/帳號、Linux namespace、ETW/journal、實體 COM 及音訊驅動均未在此模型測試。Windows唯讀命令和合成輸出供人工判讀，不能以本專案PASS宣稱公司環境正常。路徑範例只用本平台的Path.GetTempPath/Path.Combine，不把Linux分隔符當Windows路徑結果。

FD模式只算完成後淨庫存斜率與餘量674，沒有模擬每次Open的峰值。正文另算先Open兩個再Close時第674次可能需要1025而失敗；不能把operations_to_limit當可保證完成次數。
