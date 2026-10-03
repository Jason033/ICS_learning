# VoIP 基礎計算模型

這個獨立 .NET 10 Console 專案把數字音訊的單位、封包化成本與播放期限變成固定輸入、可手算的結果。它使用標準函式庫，不呼叫公司SDK、不錄音、不建立網路連線、不操作聲卡。

在已安裝 .NET 10 SDK 的練習環境執行 `dotnet run -- /自己的/結果資料夾`。未給目錄則寫入系統臨時目錄下 `voip-maintenance-results`。不要把結果目錄設成正式公司資料位置。網站下載ZIP解壓後使用即可。

基準是8000Hz、單聲道、16bit PCM、一秒1000Hz正弦。`Program.cs`生成原tone、全零silence和增益5削波clipped WAV；PCM每frame2bytes、每秒16000bytes、20ms320bytes，三檔均16044bytes。WAV是最小44-byte PCM格式，並非泛用WAV parser。

封包化比較只算PCMU持續媒體：10/20/40ms的payload80/160/320，IP層分別96/80/72kbit/s。條件是RTP12、UDP8、IPv4基本20bytes，未含Ethernet、安全封裝、RTCP或其他流量。沒有執行實際PCMU/G729/Opus編碼。

播放期限使用seq10..14、timestamp0/160/320/480/640及arrival25/45/82/85/105ms。等待40ms時一份晚到，60ms時零，但多20ms延遲；`playout.csv`保存每筆期限。相同資料的RFC平滑jitter最後15.439453125時鐘單位，換算1.929931640625ms，不能當單程延遲或真人評分。

已用 .NET SDK10.0.401 在 `/tmp` 複本編譯執行；再以Python標準wave獨立核對三檔格式、sample數與長度，tone範圍±8000、silence全零、clipped邊界值2000筆通過。未測音訊裝置、真實通話、GUI、SDK或主觀品質。

重要檔案：`Program.cs`為完整模型與驗收，`.csproj`指定net10.0。`bin/obj`和產出WAV/CSV為中間結果，沒有放入公開原始教材資料夾。
