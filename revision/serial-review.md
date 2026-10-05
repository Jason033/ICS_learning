# RS-232 與設備介面：課程範圍與驗收

這個主題有五篇核心路徑和兩篇依工作需要選讀的參考。目標是讓軟體工程師能沿程式追一筆設備命令、知道各層證據代表什麼，並判斷下一個應查的程式、文件或觀測位置。硬體章不要求電路設計或計算線路參數。

|閱讀方式|篇章|讀完後能做什麼|
|---|---|---|
|核心1|[RS-232、串列設定與命令是三件事](../docs/content/lessons/serial--serial-layers.json)|從C#字串和實際bytes追到設備解析與回覆，分清Open、Write和設備成功。|
|核心2|[UART參數與字元傳送](../docs/content/lessons/serial--uart-settings.json)|核對程式最後生效的設定，估算資料時間，區分字元錯誤、flow等待和接收積壓。|
|核心3|[資料格式、檢查碼與逾時](../docs/content/lessons/serial--frame-and-timing.json)|沿buffer狀態追蹤分段或連續訊息，判斷長度、校驗和交易期限。|
|核心4|[串列工具與軟體回環](../docs/content/lessons/serial--tools-and-loopback.json)|用實際讀取數和單一接收者判斷資料在哪個讀取或解析環節。|
|核心5|[串列通訊整合除錯](../docs/content/lessons/serial--serial-troubleshooting.json)|把設定、bytes、訊息、時間和業務回覆組成一份可驗證的故障判斷。|
|依設備選讀|[電氣介面與接線文件](../docs/content/lessons/serial--electrical-and-wiring.json)|問題落到轉接器、方向或控制線時，會查型號文件並說明還缺什麼證據。|
|依設備選讀|[RS-485、SPI、I²C與通訊橋](../docs/content/lessons/serial--other-interfaces.json)|只在程式使用這些介面時，追蹤方向控制、API完成條件和橋接責任。|

五篇核心課程不是公司專案規格；不同章可單獨查閱。選讀篇只解釋軟體維護需要的介面差異，不要求讀者做線路設計。所有教材中的設備名、資料和命令皆為明確標示的教學案例。

## 技術與讀者審查

獨立技術核對未發現實質錯誤。審查後修正了三點容易誤讀的地方：baud明確定義為每秒符號數，並限定本教材的二值UART每符號承載1bit；I²C仲裁限於多控制器情境，clock stretching標示為可選能力；所有Device-Lab-A交易時間統一使用20ms設備處理時間，請求、回覆和整筆下界一致為39.7917ms。`SerialPort.Read`回傳數以.NET 10.0.0原始碼佐證，DataReceived事件行為連到Microsoft文件。

獨立讀者審查確認五篇核心順序從命令資料流走到設定、parser、讀取與診斷；兩篇選讀清楚標示進入時機。為符合使用者的讀碼起點，正文不重教bit／byte定義，改用它們解讀實際bytes；補上`record`、null-forgiving `!`、`AsSpan(...).ToArray()`和`Take(...).ToArray()`在範例中的用途。電氣公式、反射、終端與電流計算已從學習必需內容移除。

七篇共有57題，各題解答列出推理依據。題數只說明練習分布，不能證明學習成效。

## 模型和實際驗收

[.NET 10教學專案](../docs/assets/labs/serial-maintenance-lab/README.md)使用一般C#程式及固定輸入，不引用`System.IO.Ports`、不開COM、不接裝置。專案以SDK 10.0.401建置成功，0警告、0錯誤；十次正常／故障模型執行符合預期，涵蓋資料編碼、UART時間、分段parser、deadline、通知次數、半雙工交易時間與整合診斷。`expected-output.txt`保存本輪輸出。另有未修改的Python軟體loopback練習，舊有執行紀錄通過；它只測軟體回顯，沒有驗證線路或設備。

這一輪沒有接實體設備，也沒有測轉接器、接頭pinout、線路電壓、USB驅動或bus波形。網站的瀏覽器畫面沒有在目前環境重跑。使用者本人尚未試讀，公司程式及設備型號也未提供，因此不能宣稱已驗證公司架構、修復或學習成效。新版只存在工作區；公開GitHub Pages仍是舊版。
