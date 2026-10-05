# 六個核心系列的品質複核與修正

更新日期：2026-10-05

## 這次複核做了什麼

複核範圍依已確認的課程目標，限於 Ozeki／ICS、PTT、VoIP、無線電、廠商 API、可靠性六個核心系列，共 50 篇。兩位獨立審查者全文檢視了不同的篇章：前 24 篇涵蓋 Ozeki、PTT、VoIP；另外 26 篇涵蓋無線電、廠商 API、可靠性，並逐題核對 183 道練習的解答。另一位審查者檢查系列之間的分工與閱讀路徑，沒有發現應只為了達到「約 40 篇」而合併的章節。修正後，指定問題再由審查者窄範圍讀回；沒有新的阻斷項目。

## 發現與修正

- **SIP 呼叫分支：**原文把一般 transaction 的回應方式說得過於絕對，和後面提到的 INVITE 分叉及多個成功結果相衝。現已說明 INVITE 分叉可能使呼叫端收到不同分支的多個 2xx，並分別依 `Call-ID`、`From-tag`、`To-tag` 追蹤 dialog；同時區分遠端 SIP 408 與本地 SDK／應用程式 timeout，也補上帶 `To-tag` 的 180 Ringing 可建立 early dialog。依據為 [RFC 3261 §13.2.2.4](https://www.rfc-editor.org/rfc/rfc3261.html#section-13.2.2.4) 與 [RFC 6026](https://www.rfc-editor.org/rfc/rfc6026.html)。
- **無線電讀值與程式：**修正 C# 範例把保留字 `event` 當變數名的錯誤；補上 RSSI 的正確全名、dBm 負值的意義、BER／PER 的分母與觀測位置、DUT（受測設備）以及頻率、頻寬和通道間隔的區別。`.NET` 範例中的 record 型別也移到頂層程式敘述之後，避免讀者照著程式碼排列時遇到 C# 語法錯誤。來源包含 [ITU-R V.574-5](https://www.itu.int/dms_pubrec/itu-r/rec/v/R-REC-V.574-5-201508-I%21%21PDF-E.pdf) 與 [ITU-R M.2015-2](https://www.itu.int/dms_pubrec/itu-r/rec/m/R-REC-M.2015-2-201801-I%21%21PDF-E.pdf)。
- **停止與清理：**補充兩個無線電範例的限制：`ReleaseAsync(CancellationToken.None)` 不代表有界等待，也不保證設備已停止發射；若釋放一直未完成，後續的佇列清理亦不會執行。讀者需以實際介面契約確認停止條件，超過等待上限時保留「設備狀態未知」。
- **可靠性 Lab：**統一文章、C# 原始碼與下載 ZIP 的內容。現在 `dotnet run -- async` 使用 50 個邏輯毫秒作基準，完成點為 101；`dotnet run -- async 500` 可直接比較長工作情境，完成點為 503，程式會依輸入值驗證時間線。ZIP 的五個檔案已逐一與來源資料夾做位元組比對。並行章節目標也已改成與正文實際教學相符的「識別重疊路徑及共享狀態」。
- **閱讀負擔：**50 篇是完整教材庫，不是必須線性讀完的課程。三篇目錄已標明按需選讀：本機音訊接入電話媒體、設備使用介面盒、系統使用 SIP REGISTER；無線電 RF 鏈與天線篇原本即為選讀。依這些條件，常用主線約 46 篇，各主題仍能獨立跳讀。
- **導覽：**教材中的 `#/lesson/...` 參照現在會變成站內連結；程式碼區塊中的同樣字串維持原樣，不會誤轉成連結。Smoke test 會檢查所有站內參照是否指向已開放的教材。

## 驗證結果與限制

本次重跑 `node --check docs/app.js`、`node tests/smoke.cjs`、`python3 tools/audit_content.py`、`python3 tests/verify_labs.py`、`python3 docs/assets/labs/wireshark-maintenance-verify.py`、`git diff --check`，均通過。結構稽核仍是 126 篇教材、78 篇新版、48 篇舊版參考，沒有結構警戒；這只驗證資料形狀，不是教學效果分數。

目前環境沒有 .NET SDK 或 C# 編譯器，因此本次沒有宣稱 C# Lab 已重新編譯執行。50／500 情境的數值由程式公式獨立推算，且 ZIP 已逐檔與原始碼比對；仍需要在有 .NET 10 SDK 的環境實際建置。Playwright／Chromium、Wireshark／TShark 也不可用，未做真實瀏覽器排版或原生 dissector 畫面檢查。公司原始碼、Ozeki 版本及設備文件尚未提供，教材沒有代填那些未知條件；讀者尚未完成連續試讀與知識檢核，因此不能從技術審查推論其已達成中級工程能力。
