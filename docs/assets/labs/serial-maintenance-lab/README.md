# 串列概念與讀碼模型

原創.NET10標準函式庫Console，不使用SerialPort套件、不開COM、不接設備。它讓讀者推算UART時間、電氣比較、訊息累積、重同步、checksum及期限。它不能驗證真實接線、driver、電壓、USB latency、bus或設備。

在本資料夾執行`dotnet run -- frames`。模式：`layers`、`wiring`、`uart`、`frames`、`timeout`、`events`、`interfaces`、`diagnose`。`layers`、`frames`與`diagnose`可加`--fault`。先算再跑，再改一個變因及復原；詳細任務在各章。

LabParser的虛構框架：AA起點，LEN只算CMD+DATA，1–32bytes；末byte為LEN/CMD/DATA的XOR。它不是RS-232標準、更不是廠商協定。Parser以資料長度受限、錯框架丟一byte重找AA示範；沒有完整交易ID、硬體中斷、時間性重同步或安全保證。README和預期輸出供讀碼，模型PASS只證明限定輸入的指定結果。
