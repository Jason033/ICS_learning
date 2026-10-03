# 通訊系統概念地圖：作者驗收

四章是可選、可獨立學習的整合入口，從程式、處理程序、物件、事件與狀態開始。教材提供完整的自建角色與介面契約，讓初學者學會整理陌生公司程式，而不把缺乏證據的公司依賴畫成既定事實。沒有將 RCSCall、iCallAPI 或 Ozeki 硬湊成一條架構。

## 內容如何建立維護觀念

| 章節 | 核心概念與因果解釋 | 完整推導/產物 | 故障與驗收重點 |
|---|---|---|---|
| control-and-audio | program/process/thread、class/實例/參照、方法/參數/引數、事件、狀態、控制/媒體/實體分層 | control session1完成而media附著0拒絕；重綁1後RMS600；三種無可用數值 | Completed不等可用媒體，Output不等真人聽見；probe被拒不更新、舊值不屬本次 |
| boundaries-and-contracts | API/SDK/協定/格式/電氣介面差別，型別不保證單位，完整LabControl1.0卡 | 75.5%→76、150拒絕，0–255對照→193；完整target/session/result集合 | true只接受；錯target、舊世代、未知Result不套用；借用/owner責任明確 |
| trace-a-company-feature | 定義/參照/建構/訂閱與動態觀察差別，最小C#語法、呼叫堆疊與事件重入 | Submit→Callback→Apply→Send return；重複回呼但Applied1；來源索引表 | Pending須在Send前，終態不可無條件重置；實例、身份與時序三候選分開 |
| system-maintenance-map | 圖的尺度/圖例，C4的定義，呼叫/資料/事件/所有權，GC/Dispose/借用者 | 功能卡F1、證據E1–E5、B/C共享a；B關閉後剩一訂閱C仍工作 | wrong實例不回到a；借用者不釋放共享owner；退訂不能被closed guard替代 |

四章每篇八題，共32題，基礎/應用/診斷三層，至少四題新資料。手算確認RMS、單位刻度、範圍、訂閱數與身份。每題解答提供規則、推理與驗收，題目不依賴未知公司版本。章節前置由本章補齊，不要求全站必修。

## 可執行模型與驗證

`big-picture-maintenance-lab` 僅.NET10標準庫。作者將source複製至暫存目錄以SDK10.0.401實際build，0 warnings / 0 errors；四模式paths/contracts/trace/ownership與19項斷言通過。真實標準輸出保存在專案 `expected-output.txt`，source SHA-256及測試細節見 [big-picture-author-verification.json](big-picture-author-verification.json)。

九種刻意破壞均編譯並觸發指定FAIL：移除歷史Pending controller session檢查、移除media session、移除Ready、錯誤0–1刻度、允許未知Result、終態重複套用、未退訂、借用者釋放共用adapter、Pending移到Send後。分別保存失敗輸出和非零退出狀態，復原後all通過並逐字吻合預期輸出。測試不是只對固定結果喊PASS，而由真實物件、委派清單、字典、回呼順序及樣本計算產生結果。

完整身份字典與顯式guard互補；驗收的是合約效果，不能將另一guard擋下的案例當作已獨立驗證被刪guard。正文與README已修正原先把(R2,1,B)誤稱為隔離專用guard的論述：其完整key不在字典，只能驗整條拒絕效果。新增重連前仍存在且Pending的(R1,1,B)，令查找/目標/結果都成立、只有session不符；只移除session guard便使歷史狀態改Completed、Applied變1，指定斷言實際FAIL。復原後歷史與當前均Pending、Applied0，當前R2合法完成，避免教出假陽性的回歸方法。

## 來源、圖與人工修訂

已web開啟Microsoft events/interfaces/Dispose、VisualStudio Go To/Peek及Call Stack、C4 diagrams/dynamic/container/deployment、NASA interface management和Microsoft distributed tracing。各章以相關公開官方文件核對通用機制；Lab角色、參數、模式與完整案例是原創定義。

兩份原創430px直向SVG分別表示控制與媒體獨立驗收、共享owner/借用者及事件反向關係，含title/desc/alt/caption。文字短句並保留實例與身份條件，圖不代表公司部署。

根審首/末篇指出日文混入與C4未定義，已改成中文責任描述並加入C4白話定義。簡繁轉換後人工發現process被錯轉成program，已明確重寫「程式→處理程序（行程）→執行緒」，沒有依批次轉換判語義正確；亦修正實例、支持/通過、局部與單執行緒等用語。同儕審閱後修正control-and-audio goals/recap的處理程序名詞，troubleshooting首次以「必須維持的條件（不變量）」定義invariant；回歸Q3也明示完整key/歷史保留條件，不以request文字存在證明專用guard。evidence Q3改為同一接收窗口，避免與含舊session的資料互斥。

## 已知限制與整合邊界

模型沒有真實畫面、網路、RTP、SDK或裝置，Send固定接受是宣告的Lab契約，Emit由測試者注入。媒體兩short僅代表數值計算，不用它計真實頻寬或聲音品質。單執行緒未驗回呼/關閉並行競爭、廠商釋放順序或舊.NET Framework相容性。

作者只完成內容與模型自審，不代稱網站ZIP/瀏覽/公開部署或同儕審查已完成；主代理整合後再驗收。這份圖文與功能檔案範例可遷移到公司程式閱讀，真實關係仍需其版本、合法文檔和執行證據。
