# 故障分析方法：作者驗收

本主題的六章從方法出發，而非再列一次各通訊主題的故障名稱。目標是讓入門讀者知道一條證據怎樣支持結論、實驗如何排除候選，以及修正為甚麼需要保留拒絕條件。全部系統契約與資料都是教學自建，不推定公司的 RCSCall、iCallAPI、SDK 或硬體關係。

## 內容與維護能力

| 章節 | 正文建立的必要概念 | 可重建推導與練習產物 | 需要區分的機制 |
|---|---|---|---|
| evidence | 症狀/觀察/推論/假設，必要與充分條件，證據獨立性，身份/時間窗/分母與守恆 | 三種零輸出、觀測器關閉兩例；audio/observer 計數表 | 真正未收到與缺紀錄，收到與接受，播放呼叫與非零資料 |
| reproduce-and-control | 輸入與初始狀態，基準/正常對照，操作變因/混雜/控制，交互作用，觀測干預與零失敗解釋 | 2×2 成本20/30/35/55ms；兩版第二通對照；概率0.9^n | 版本、負載、觀測成本與第二通歷史不能混為一因 |
| timeline-and-causality | 時間源/起點/單位，偏移與誤差，完整身份，因果與部分順序，媒體時間/本地時刻 | offset −50±5→延遲25–35；合法結果Receive65/Apply130/Display160 | 外部慢、本地套用慢、舊會話串線；時間近不等於互相導致 |
| one-way-audio | 方向，PCM格式/端序，RMS探針位置，播放期限，第一偏離與多重原因 | 320 bytes/20ms/960 bytes，三幀晚10ms；五種無非零輸出 | 錯路由、舊session、全零、超期、靜音；非零樣本不等真人聽見 |
| cross-layer-cases | 層間契約，命令/狀態/資料不同生命週期，准入/Release，鏈路epoch與切換階段 | Ready80/Release110 sender與device窗口；primary1→backup2 | 來源晚啟動、sender提早、播放晚到；舊結果不更新當前鏈路 |
| fix-and-regression | 緩解/原因修復/契約變更，前後置/不變量，owner，正面/負面/相鄰邊界，故障注入 | 舊session修正與buffer40→60取捨；五項regression斷言 | 症狀改善但隔離受損，測試靠別道guard通過，性能改善但紀錄丟失 |

每章八題，三層且至少四題為未見的新資料。題目給出單位、身份與規則，解答列計算、證據和驗收。公式與答案已按其契約手算；例如同軸截止時刻明寫 t=100ms，offset 明寫 B−A，媒體 T0/arrival 明寫同一本地相對軸。

## 實際執行與故障注入

`troubleshooting-maintenance-lab` 使用 .NET 10 標準庫。作者在 `/tmp` 副本以 SDK 10.0.401 實際 build，0 warnings / 0 errors；`all` 執行六模式，24 項斷言及末尾總結通過。實際標準輸出保存於專案 `expected-output.txt`，驗證明細與 source hash 保存在 [troubleshooting-author-verification.json](troubleshooting-author-verification.json)。

七種刻意破壞均實際編譯並得到預期失敗：移除session、移除mute、把log成本10×load改10、clock符號反轉、身份只比request、移除Release、移除Link/epoch。每種記錄非零退出狀態及指定 FAIL 名稱，復原後 all 通過且輸出與預期檔相同。這證明測試能抓這些指定失效，不聲稱窮盡所有錯誤。

PCM真的以 `BinaryPrimitives` 編解碼並平方平均計算RMS。LastRms在session/期限後、靜音前，是最後一幀通過此探針的值；全late時初始0不證明已解碼全零。PTT sender時點與設備模型接受時點分開；不把sender列表報成實體TX。ControlAccepted是案例給定前提，不是實際SIP測試。

## 來源與視覺核對

已透過 web 開啓 Google SRE troubleshooting/monitoring/postmortem，Microsoft logging/tracing/Stopwatch/events/Dispose/BinaryPrimitives/WAVEFORMATEX，NASA interface management，IETF RFC 5905 / RFC 3550。來源只支持通用工具與標準機制；案例、公式成本與角色均為原創教學定義。各章列相關具體來源，沒有以公開文件證明公司採用該架構。

兩份原創SVG是同症狀不同責任邊界，以及clock換軸。採430px直向畫布、title/desc、alt/caption；手機縮放後仍保留短句與數字。不用圖推定公司拓撲。

根審第一章指出RMS定義與探針位置不足，已新增平方平均開根號及±1000例；另補LastRms的初始值含義。根審時間題指出相對期限歧義，已寫絕對截止 t=100ms及T0/arrival同軸。繁簡轉換後已人工修正「只」、局部、支持、多執行緒等語義詞，未將coding rate等不同概念合併。

同儕審閱補正：invariant首次定義為必須維持的條件（不變量）；fix-and-regression的Q3與測試機制說明加入完整key查找/歷史保留前提。request文字存在不足以證明只測session，必須證明其他拒絕路徑不會遮蔽缺陷。LabAudio沒有request字典，old-session保持route/期限/樣本條件成立，故既有七種模型驗證不受此正文修訂影響。evidence Q3改為同一接收窗口，容許其中含不同世代的到達資料。

## 尚未驗證的層與接下來

未連公司SDK、真實網路、NTP、音訊裝置、射頻/衛星或實體PTT。模型單執行緒且成本為宣告公式，不量測機器效能；非零PCM不證明可懂語音或真人聽見。網站最終結構、ZIP、瀏覽與同儕交叉審查由主代理整合，本作者不代稱那些檢查已完成。
