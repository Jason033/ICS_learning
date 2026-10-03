# Ozeki API 第二版：概念重寫與驗證紀錄

這次七篇全部重寫。主要補足的是讀者必須先懂的機制：API／SDK／包裝、型別與實例、參考與所有權、委託與事件、註冊綁定與認證、單通狀態、兩條音訊路徑、終止與晚到工作。程式和實作用於逐步驗證這些機制，沒有把延長閱讀時間當作深度。

正文只計 sections.paragraphs 字元，七篇分別約 4,540／4,686／4,805／4,601／4,689／4,634／4,779；每篇至少兩個完整例題、一個追碼實作與同症狀多原因鑑別。共56題，基礎21、應用21、診斷14；每題多段解答，每章至少五題給新資料、程式條件或時間線，題幹足以在展開答案前推理。時間已改閱讀／實作／練習範圍，待主代理按全站實際文字統一校準，沒有沿用固定50分鐘。

| 章節 | 建立的維護能力與概念 | 例題與追碼產物 | 故障機制與驗收 |
| --- | --- | --- | --- |
| read-a-call | 區分SDK與包裝；物件、參考、三層責任和媒體圖 | C1/C2/alias身分；先操作後訂閱；graph物件圖 | 舊參考擋入口／handler訂錯物件；第二通及晚到事件 |
| documentation-and-version | 組件與執行環境；完整簽章、事件委託和部署身分 | 相同版本不同路徑；Action簽章配對；version閱讀卡 | 編譯失敗仍跑舊EXE／包裝漏狀態；真實啟動路徑驗收 |
| registration | 帳號與Contact的有期限綁定；欄位角色、挑戰、刷新 | REGISTER/401/認證/200延遲；A/B線路隔離；registration | 太晚訂閱／Error未轉發；快速成功、拒絕、舊配置回覆 |
| calls-and-events | 單通歷史、命令前提、來電物件、終態與再進入 | 正常LabSession轉移；C1晚到清C2；calls時間線 | Answer錯物件／已取消仍顯示；拒絕、取消、舊事件回歸 |
| media | PCM樣本與區塊；來源→目標、附著、啟用、SDP/RTP | 16k/16bit/20ms算例；雙向邊與目標C1；media資料圖 | 採集零／sender附舊通／網路／裝置；逐方向與下一通 |
| lifecycle-and-events | owner與借用、GC引用、委託集合、UI派送與終止 | 重複訂閱數量；連續三通清理；lifecycle引用圖 | 已釋放共享媒體／舊清理越界／UI等待；重復End與關閉 |
| evidence-troubleshooting | 症狀→機制→可推翻假說→有限修正→回歸 | 兩組單向資料分叉；兩通清理隔離；all故障樹與報告 | 註冊、撥號、單向、第二通多原因；同條件與相關路徑回歸 |

## 來源核對與API邊界

已用web核對 Ozeki p_7535 註冊、p_7537 通話、p_7536 媒體、p_7542 多通、p_7539 控制、p_7272 舊桌面、p_7092 接聽頁、p_7383 TLS、p_7307 裝置，以及IncomingCall參考頁。核對項目包括 CreateSoftPhone 的媒體埠範圍、CreatePhoneLine、RegistrationStateChanged、RegisterPhoneLine、CreateCallObject、Start、Answer、HangUp、CallStateChanged、MediaConnector 的來源/目標、AttachToCall/Detach、終態清理和每通CallHandler。

文件確有年代與層次差異：較新教學的e.State和舊示範的e.Item、範例自寫Softphone.PhoneLineStateChanged、ICall/IPhoneCall、接聽頁文字Accept與通話程式Answer。教材以具體頁與目前宣告配對，沒有宣稱存在一種跨所有版本通用簽章。IncomingCall參考頁明列VoIPSDK.dll 11.2.4.290；不是公司版本。

SIP註冊／認證／建立／取消／終止以RFC3261核對；RTP序號、時間戳與交付邊界用RFC3550；SDP用RFC8866；事件、委託、Dispose、Assembly.Location、WinForms線程與Invoke/InvokeAsync用Microsoft官方文件。各篇4至5項具體來源。

## 實際已驗證

- 七篇JSON能解析，正文≥4,500；每篇2例題、lab、診斷、8題三層多段解答、≥3來源，資源存在、表格欄數一致、兩張原創SVG能解析。
- `ozeki-maintenance-lab`是獨立.NET10 Console專案、零外部套件，所有模型型別為Lab前綴，無Ozeki參照、無SIP/RTP、無裝置使用。
- 複製至`/tmp/ics-ozeki-verify`，以.NET SDK10.0.401編譯及執行all，退出0，全部情境斷言通過。
- 在臨時副本故意移除通話事件解除，lifecycle於`C1_cleanup_once`失敗、退出1；恢復後all再通過。這驗證測試確實能發現訂閱殘留，而不是只照印PASS。
- 公開docs內未產生bin/obj。原URL章節ID保留，舊資源不刪除。新增兩張概念圖、Program.cs下載；主代理會加入同名完整專案zip下載。

## 尚未驗證及限制

官方SDK片段是已核對成員與順序的閱讀錨點，未在實際Ozeki DLL編譯；沒有偽裝為Lab專案已驗證。真實裝置、PBX、授權/試用、網路、回呼線程、併發清理與SDK資源釋放契約，須在取得公司實際版本和授權測試環境後驗證。Lab同步事件不能證明SDK回呼保證；其媒體bool只檢查模型關係，不是音訊質量或真實SDK遙測。

下一步先由另一位代理互審代表章與陌生題，主代理整合render、時間與下載；再以公司唯讀參照和實際組件建立版本/調用圖，逐步映射Lab機制。
