# 教材第二版撰寫與審稿標準

這次重編涵蓋已發布的十個主題、83 篇。使用者選擇 C#／.NET 為主要語言、Python 為輔助工具，重點是公司程式閱讀、系統維護和除錯。各主題仍獨立，保留章節 ID／網址。此標準不把閱讀等同於取得職稱；目標是可在陌生案例中追程式、解釋機制、提出候選原因、查證、修正並驗證。

## 最新需求修正：正文基礎是核心

使用者補充：不是實作過程缺少，而是最基礎內容根本不足。所有作者先把正文的概念閉環完成：為何需要、定義、機制如何逐步運作、概念之間的關係、前提與邊界、工程後果。不能用更多程式、實作流程、資源或篇幅補缺；不能只列名詞或反覆提醒未知。讀者單靠正文就應能解釋核心機制。例題與程式只作驗證與深化。以下實作與練習要求保留為輔助，不是本次改善的主因。

## 所有作者共同遵守

- 重寫整篇，不能用附加相同模板、名詞表、提醒或反覆「仍未知」堆字數。以自己的教學推導、具體數據、程式和案例教透機制。
- 先補本章所需的前置概念，再介紹機制、完整例題、程式／資料判讀、維護取捨、實作、故障鑑別和練習。語氣是對剛入門的人解釋工程，而非對同行列名詞。
- 每章正文通常需約 4,500–7,500 中文字元（不含練習與來源），至少 3,500；這只是防止再次交出摘要的警戒值，不能取代人工審查。廣泛章節按需求更長。避免為篇幅加入離題內容。
- 至少 2 個 worked-example（完整輸入→規則／程式行→中間值／狀態→結論）、1 個 lab（環境／資料→操作→預期→變因→故障→復原→驗收）、1 個 diagnostic-case（同症狀至少兩個原因，給後續證據直到有限修正與驗證）。最終章以多案例整合，不要先把答案寫在案例敘述。
- 練習至少 8 題：基礎、應用、診斷三層；至少 4 題需處理正文沒有給過的新數據／程式／時間線。解答需有證據→規則→推理→結論／驗證，通常 3–5 段或步驟，不能只列一句名詞。涉及算式列中間步驟，涉及修正列回歸驗證。
- 公司實際拓撲、RCSCall／iCallAPI 的依賴及具體 SDK 版本仍未知；不要把假設寫成事實。教學 C# 模型必須明示為自行建立的 Lab 型別；不可把虛構 API 當 Ozeki API。
- C# 教學工具以 .NET 10 Console project 為可重跑目標；說明這是獨立練習環境，舊公司 .NET Framework 不一定有同樣 API。只使用標準函式庫。不呼叫公司設備。SDK 實際範例可採來源核對／人工追碼，清楚標示未在實際 SDK 編譯。
- 來源以官方標準、廠商、官方工具文件為主，必須透過 web 核對關鍵技術主張及版本。原創推導與案例為主，避免大段轉述單一來源。每章 sources 至少 3 個有關的具體頁／標準段落。

## 資料與檔案格式

每位作者只能修改被指派 topic 的 `docs/content/lessons/<topic>--*.json`、自己名稱前綴的 `docs/assets/labs/`、`docs/assets/diagrams/`、`revision/<topic>-review.md`。不要改 catalog、app、styles、README、tests 或其他 topic。不得刪既有資源，維持原 URL。

章節沿用 intro/goals/sections/exercises/recap/sources/resources，並加：

- `contentVersion: 2`
- `prerequisites: [白話前置概念與本章補充位置]`
- `studyTime: {reading:[低,高], practice:[低,高], exercises:[低,高], basis: "實作與作答任務的時間估計依據；尚待使用者計時"}`。閱讀範圍由主代理最終按實際正文統一重算，不沿用舊50分鐘。practice不得憑空估算，必須對应可完成的任務。
- section `kind` 可為 `concept`、`mechanism`、`worked-example`、`code-reading`、`lab`、`diagnostic-case`、`maintenance`。正文段落為純文字，Markdown 標記不會解析。code 用 `{title,text}`；table 用 `{caption,headers,rows}`；steps/bullets 是字串陣列。複雜例題可拆成多個 section。
- exercise `level` 為 `foundation`、`application` 或 `diagnosis`；`answer` 可為多段字串陣列（優先）或含空行的字串；可附 `answerSteps`、`answerCode:{title,text}`。question 給足起始資料，未讀解答前可獨立作答。
- lab 資源仍用 resources `{path,label,description}`。可用 .py/.pcap/.cs/.json/.csv/.zip/.txt/.md，下載包檔名小寫英數破折號；專案內可保留 Program.cs／README.md 等標準檔名。C# 專案放在 `docs/assets/labs/<topic>-maintenance-lab/`，以 parent 最後產生的同名 zip 供下載（直接子檔可下載 .cs，但 .csproj 不放 resource）。不得改現有 Python 工作示範，新增替代即可。

每個 topic 提交 `revision/<topic>-review.md`：章節→維護能力→例題與實作產物→故障機制→核對来源／驗證狀態的對照；說明實際已驗證和仍需設備／SDK的項目。主代理與另一個 agent 審查代表章與所有章節結構，任意 topic 未達標須補寫，不能靠篇幅 pass 宣稱教學深度。

語文校正需人工核對，不能只依簡繁轉換工具：例如 coding rate 是無單位比例，bitrate 才有 bit/s；類比訊號與模擬實驗、concurrency 與 parallelism 都不能混為同一概念。修詞後須重讀原段因果與單位。
