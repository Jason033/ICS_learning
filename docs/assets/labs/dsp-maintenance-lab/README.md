# 訊號、DSP與SDR：獨立 C# 判讀模型

這個專案用小型、可手推的真計算建立資料契約與維護觀念。它不讀麥克風、音效卡、RF、公司 DLL或網路，不是通用調變解調器。`LabFir`是自行建立的教學型別，`Complex`是標準函式庫。

需要獨立 .NET 10 SDK，不要求升級公司 .NET Framework。解壓後在此目錄執行 `dotnet run -- all`，或指定下表模式。無外部套件，`NuGet.Config` 清除套件來源。先看章節定義、手推，再設中斷點與核對輸出。

| 模式 | 真正計算與要理解的結論 |
| --- | --- |
| spectrum | 四點[1,0,-1,0]直接DFT得到X1/X3=2、正逆還原；實數內部單側峰幅需要factor2。 |
| sampling | 16份1k與7k的8ksps cos逐份比較同值；Nyquist sine可全零；率轉換算式。 |
| filter | 三tap[.25,.5,.25]實際FIR；整段與連續chunk等價，重置後半首點0.75而非2。 |
| snr | 均方1/0.0625、SNR12.0412dB；只增signal而noise固定升6.02dB；削波error=y−s。 |
| coding | 實際三重複編碼、翻轉、解碼與逐bit比較；分散單錯可修，同碼字兩錯判錯。 |
| iq | 真complex旋轉、DFT與共軛；正負峰分別k1/k3；0.6+j0.8乘−j成0.8−j0.6。 |
| pipeline | 真tone→FIR→偶索引抽取→DFT，32/8k變16/4k，8點穩態視窗peak2=1k。 |
| diagnose | 分辨單／雙側尺度、錯用sample rate與跨chunk歷史。 |

`expected-output.txt` 是隔離副本在 .NET 10.0.401 的實際 `all` 輸出。29項機制斷言與總完成透過。每項都比較真數值或狀態，錯誤會非零退出。另在隔離副本把逆轉換的除N改除1，`spectrum`應失敗；來源維持正確。作者證據見 `revision/dsp-model-verification.json`。

章節提供一變因實驗、預期失敗與復原。不要刪Check掩蓋錯誤。例如每個chunk新建FIR，或把輸出4k軸誤寫8k，都會被對應驗收抓到。模型的浮點容忍值只對小型教學資料，不是任意生產數值的通用門檻。

明確限制：直接DFT不是最佳化FFT；三tap只展示狀態、響應和無其他帶外輸入的限定1k鏈，不能滿足任意抗混疊規格；三重複只是最小FEC示範，沒有真同步／軟解碼／通用收發器。`pipeline`的CPU1ms是給定數學比較，不是本機效能量測。所有振幅是正規化，沒有RF功率校準。

GNU Radio部分是官方檔案核對與可選純模擬flowgraph操作，這次沒有執行其GUI或硬體。C#驗證不能替代GUI庫尺度與裝置時鐘驗證。Python只作可選圖表輔助，圖的資料、單位與代表點由章節解釋。

重要檔案：`Program.cs`是八模式；`Lab.csproj`是獨立編譯目標；`NuGet.Config`限定標準庫；`expected-output.txt`是實跑基線。完整正文、例題、64題答案與圖在網站，作者審查在 `revision/dsp-review.md`。
