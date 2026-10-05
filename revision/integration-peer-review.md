# 廠商 API 整合系列獨立審查

審查日期：2026-10-05。對象是目前工作區的 `integration--*.json` 九篇 v3 教材、七十題解答、目錄順序與四個下載資源。這是內容與資料審查，不代表公司 SDK、設備或部署環境已驗收。

**獨立初審未見 P1 阻斷問題。** 九篇能把一次操作從入口、包裝層、外部契約接到回覆、事件、狀態與維護證據；教材沒有把方法返回、Accepted、Completed 和設備效果混成同一件事。初審提出三類 P2 和幾處文字錯誤，均已由主協調者作最小修正並記錄在文末；目前沒有未處理的 P2。公司實際介面仍需依版本來源確認。

## 審查方式與範圍

全文閱讀九篇的 intro、前置條件、目標、九十個段落區塊（sections）、所有程式／流程／表格、七十題與答案、recap、來源和資源。逐題檢查答案需要的條件是否已在正文或虛構契約說明；另外追讀 C# `Program.cs`、專案 README、虛構 ICD 卡與 Python 模型。不是只以篇幅、題數或 schema 通過判斷品質。

讀者起點沿用已確認需求：一般資工概念大致熟悉，主要需要把端到端流程接起來，能閱讀、除錯及修改既有程式；C# 語法遇到時補。本文不要求把所有未知公司介面補成一套假想架構。

本次實際執行 Python 五個案例，退出碼 0；C# 做完整靜態追讀及輸出推導。當前 `/tmp/ics-dotnet/dotnet` 不存在，沒有宣稱本次編譯或執行 C#，也沒有自行安裝 SDK。瀏覽核對下列核心官方來源；其餘來源做 URL／來源類型核對，沒有把「網址存在」說成所有主張均已逐條驗證。

## 逐篇判斷與目錄順序

目錄順序已符合概念依賴：先沿操作找到邊界，再查實例與所有權、讀契約，才深入通知、狀態、外部效果、恢復、證據及綜合除錯。可以維持現有順序；無須新增通訊或 CS 基礎必修。

| 順序／篇章檔名 | 本篇回答的問題 | 審查判斷 |
|---|---|---|
| 1 `integration--trace-an-action.json` | 一次操作實際經過誰，結果從哪裡回來？ | 正反向流程、物件／堆疊／時間線、雙實例故障均教清；API／SDK 首用角色已補。 |
| 2 `integration--layers-and-ownership.json` | 實際用的是哪個實例，誰可以釋放？ | 對參照、借用、擁有、DI／factory、舊參照與解除訂閱交代充分。雙控制器與替換實例的答案可由例題推導。 |
| 3 `integration--read-an-icd.json` | 哪份文件能證明什麼，欄位怎樣變成規則？ | ICD 在首節明確定義；API reference、版本說明、實作和執行證據責任分明。型別、單位、範圍、Accepted 語義和格式邊界均有可追算例子。 |
| 4 `integration--commands-and-events.json` | 同步回傳、事件與畫面套用為何分開？ | 委託／訂閱、同步重入可能、pending 建立順序、亂序／重複與 UI 派送形成完整機制，沒有把事件語法當作背景執行緒。 |
| 5 `integration--sessions-and-state.json` | 連上與 Ready 有何不同，舊結果如何隔離？ | 連線、驗證、設備條件及操作結果分開；當前 ID 配舊 session 的例子能單獨驗證世代檢查。重連、快照新鮮度與 Unknown 已交代。 |
| 6 `integration--audio-and-device-boundaries.json` | 控制回覆到哪裡為止，哪些效果仍未證明？ | 範圍已收斂到外部交接和效果證據；不重教 Ozeki、PTT、VoIP 或 RF。四節雖較短，限定目標均有正文支持，不必為篇幅展開相鄰主題。 |
| 7 `integration--errors-and-recovery.json` | 拒絕、本地錯誤與未知結果應如何恢復？ | 例外傳播、逾時不等於未執行、冪等、部分成功、預算及所有權完整；總嘗試的計數口徑已補明。 |
| 8 `integration--correlate-evidence.json` | 多層紀錄如何連成同一操作的證據？ | ID 範圍、來源、時間基準、結構化紀錄和因果對照分工明確。數字先算分段，再限制結論，未把整段等待直接叫網路時間。 |
| 9 `integration--integration-troubleshooting.json` | 如何用前八篇找第一個不一致交接並有限修正？ | 雙實例與當前 ID／舊 session 兩例能區分候選機制；再處理外部讀回、UI 等待及所有權，構成維護閉環。 |

## P1：未發現

沒有發現錯誤基本機制、題目在既定假設下無法回答、主路徑缺失，或把模型當公司 API 的重大問題。「未發現」限於本次版本與上述範圍，不代表真實產品保證。

## P2：初審提出並已處理的最小修正

### P2-1：API、SDK 首次出現時缺少兩者關係（已處理）

位置：`integration--trace-an-action.json` intro（行 3）、第 1 節首段（行 19）；ICD 先在第 5 節使用，正式定義在 `integration--read-an-icd.json` 第 1 節（行 18）。

首篇會說 adapter 映射 API 參數、SDK 執行契約，但尚未直接回答「API 是可使用的介面約定，SDK 是廠商提供的實作／開發工具包，SDK 可以提供多個 API」這個關係。對新進工程師，容易把 DLL、SDK、API、ICD 都當成不同黑箱的名稱。

最小修正：在首篇流程旁用兩三句說明角色，例如 API 是程式可呼叫的入口與約定；SDK 是廠商提供的函式庫及開發支援，可含 DLL 與多個 API；ICD 是交換邊界的契約文件，第三篇才細讀。這不是要求增加獨立名詞百科，也不應宣稱所有廠商 SDK 必有相同包裝方式。

### P2-2：正文「重試三次」與總嘗試次數口徑不一致（已處理）

位置：`integration--errors-and-recovery.json` 第 5 節第 3 段（行 61）；同篇第 5 題已明示「次數均總嘗試」。

正文寫「用戶、controller、adapter 各重試三次，最壞 27 次外送」。27 對應各層**總嘗試 3 次**；若「重試」指初次之外再做 3 次，會是 4×4×4=64。題解並未算錯，但正文詞義會讓讀者得到兩套口徑。

最小修正：改為「每層最多總嘗試三次（含第一次），最壞 3×3×3=27 次外送」，保留其他重試語義與預算機制。這是概念精確性修正，不是改策略參數。

### P2-3：Python 與 C# 的晚到結果政策不同，下載說明未提示（已處理）

位置：提供 `integration-trace-lab.py` 的五篇 resources：首篇、commands、sessions、evidence、最後篇；比較 Python `on_device_event` 與 C# `LabController.OnComplete`／`LabScenarios.Recovery`，以及 C# 虛構 ICD 卡。

Python `late` 在 50 ms 逾時，80 ms 收到同 session 事件後仍維持 unknown，要求再次驗證當前狀態。C# 本系列契約則允許當前 request／session 的有效晚到完成將 Unknown 解析成 Completed。兩者可各自成立，正文也提醒真實政策依契約；問題是讀者下載到兩份同主題素材時，沒有明確知道它們採不同政策。

最小修正：把 Python 標成獨立的保守觀測示例，資源 description 或 README 直接寫出其晚到仍待查證；本系列題解以 C# ICD 的政策為準。也可由主協調者選擇統一模型政策，但這不是本審查已授權修改的內容。

## 純措辭／typo（不構成原理阻斷）

| 位置 | 現有文字 | 最小修正 |
|---|---|---|
| `integration--trace-an-action.json` 第 8 節，行 130 | 操作條件條件 | 操作條件 |
| `integration--sessions-and-state.json` 第 2 節行 28、第 1 題答案行 161 | 操作條件條件 | 操作條件 |
| `integration--audio-and-device-boundaries.json` 第 3 節第 2 段，行 75 | 若 callback 根本沒程式式 | 若 callback 根本沒進入可觀測的 handler；保留「無紀錄不等於沒發生」的限制 |

這幾項與 P2-1 的必要概念、P2-2 的次數口徑、P2-3 的政策差異分開處理，避免一個錯字被解讀為整篇機制錯誤。

## 題解獨立推導與模型對照

除全文逐題比對，另先依正文推理再比較答案：

- Polling 第 4 篇：50 設備×10 次／秒=500 次／秒；每設備每次 0.2 秒，穩定發起且可重疊時平均約 2 筆在途。答案有速率和條件，沒有把 500 直接叫執行緒數。
- 亂序 b、未知 x、a、重複 b：a／b 各套用一次，Applied=2；x 與已終態的 b 忽略，Ignored=2。與 C# 有效 ID／終態規則一致。
- 同時 Connected／Authenticated／Ready：八個布林組合中只允許 111；OR 會多放行七種不完整條件。正文已教這三個不同事實。
- 重試預算第 7 篇第 4 題：400×3+100+200=1500 ms，三次完整嘗試；第四次還需 400 等待+400 嘗試，超預算。等待只算在嘗試之間，與例題一致。
- 第 7 篇第 5 題明示總嘗試：2×3×4=24。答案正確，需修的是上述正文的「重試三次」用語。
- 第 8 篇第 5 題：15−10=5、90−15=75、94−90=4、110−94=16，總 100 ms；75 ms 是結果觀察等待，不能由此推出單向網路或設備處理時間。
- 最後篇 received=100、apply=102、UI=900：主要已界定的等待在套用至顯示的 798 ms；先看 UI 派送／阻塞。不能拿增加網路 timeout 作為此證據支持的修正。
- 最後篇世代例題：建立當前合法 ID，再給舊 session，單獨檢验 session 規則；僅給不存在的舊 ID 會被另一條過濾擋下，不能驗證該規則。題解與正文因果一致。

下載 ICD 明說 Target 只供關聯、模型不驗證設備存在；`Emit` 是同步注入、沒有真實 I/O／回呼執行緒保證。C# 確實先建立 Pending 再 Send；例外記 LocalException 並重新拋；借用 controller 只解除自身訂閱，不 Dispose 共享 adapter。這些與九篇題解一致。模型未實作實際廠商 ID 轉換、認證、服務端冪等或多執行緒同步，正文沒有宣稱已覆蓋。

## 技術來源核對與適用界線

- [NASA 6.3 Interface Management](https://www.nasa.gov/reference/6-3-interface-management/) 與 [Appendix L](https://www.nasa.gov/reference/appendix-l-interface-requirements-document-outline/) 支持介面角色、交換格式／時序／初始化／狀態與變更管理；不證明公司有 NASA 式流程，更不定義公司 Accepted／Completed。
- [Microsoft 事件訂閱與解除](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/events/how-to-subscribe-to-and-unsubscribe-from-events) 支持 handler 的保存、解除與發布者保留參照；[委託](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/delegates/) 支持可呼叫簽章與方法目標關係。這些不是廠商回呼執行緒或送達保證。
- [Microsoft DI guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines) 支持容器建立資源的清理責任；正文將手動 root 模型與實際 container／factory 分開，未要求公司一定採 DI 框架。
- [Microsoft 例外最佳實踐](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions) 支持 `throw;` 保留原堆疊、`throw ex;` 重設拋出點，以及避免 finally 產生新例外；[合作取消](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) 支持取消由接收者配合，不證明遠端設備撤回。
- [Microsoft await](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/await) 核對非同步等待語義；[Stopwatch／.NET 10](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-10.0) 核對經過時間、計數與 Frequency。本文不把不同起點的 elapsed ticks 或未對齊牆鐘直接當跨機延遲。
- [RFC 8259](https://www.rfc-editor.org/rfc/rfc8259.html) 支持 JSON 數字與字串不同；[RFC 2119](https://www.rfc-editor.org/rfc/rfc2119.html) 是明示採用時的要求層級詞彙。正文沒有假定所有廠商文件中的 must 都自動具有同樣規範地位。[Microsoft JSON 反序列化](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization) 用來核對解析與型別處理的範圍，不把解析成功當業務合法。
- [AWS 冪等 API 與重試](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/) 支持 caller/request ID 與同意圖去重的契約思路；[Google SRE 級聯失敗](https://sre.google/sre-book/addressing-cascading-failures/) 支持重試負荷和預算問題。兩者不給教學模型或公司 SDK ExactlyOnce 保證。
- [Microsoft distributed tracing](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-concepts) 與 [logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview) 支持 Activity／TraceId／SpanId、父子上下文與結構化欄位；不會自動替私有設備協定傳遞業務 ID。[Visual Studio call stack](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-use-the-call-stack-window?view=visualstudio) 支持同步堆疊觀測，不取代跨事件歷史。

## 結構、連結與交付檢查

- 九份 JSON 與 catalog 可解析；另用保留鍵順序的 parser 檢查重複 JSON key，未見重複。九個章節 ID 唯一，檔名／目錄一對一，全部 contentVersion=3，沒有 studyTime。
- 所有必需資料均非空；九十個 sections 有 heading／paragraphs；七十題均有非空多段答案；所有表格欄數一致。
- 來源有二十二個不同 HTTPS URL；四個不同本地下載路徑全存在，且符合 app 的 safeAsset 規則。教材沒有引用圖片，不存在缺圖項。
- C# ZIP 的 Program.cs、README.md、csproj 與目前獨立下載／資料夾檔案逐 byte 相同。初審 Python 五案例退出 0；晚到政策當時列為 P2-3。
- `docs/app.js` 用 topic／chapter ID 產生 `#/lesson/integration/<chapter-id>`，九條 route 均能映射現有檔案；前／後篇依 catalog 的 ready 順序生成。這是資料與路由程式核對，沒有宣稱本次做瀏覽器點擊驗收。
- 正文中的〈篇名〉及 Ozeki／PTT／VoIP／無線電目前是文字交接，`formatText` 只處理 inline code，不將它們渲染為超連結。未見錯誤篇名或斷裂的實際 href；若未來需一鍵跨主題，屬網站導覽增強，不是本次教材內容阻斷。
- 對九篇及 catalog 執行範圍化 `git diff --check`，通過。此報告新增後亦做 whitespace 檢查；未修改 lesson、catalog、app、lab 或其他作者的檔案。

## 不需要改與驗收界線

保留目前同一 SetLevel 主線與幾次重新對照是合理的：首篇畫路徑，第二篇教實例責任，第三篇教契約，第六篇限制效果證據，最後篇綜合診斷；各自回答不同問題。不要為刪重複而把必要的機制只留下名詞。

也不必在通用整合教材展開 PCM、Ozeki handler API、SIP／RTP 封包、PTT 准入或 RF 內部機制。SDK 外部未知邊界應維持標示；實際傳輸、回呼時序、ID 範圍、Ready 定義、重連、取消與設備效果，仍須公司部署版本的文件／實作／受控觀測。這些未知不是教材應編造的缺章。

正式發布前的 C# 編譯／執行和瀏覽器驗收由整合流程承接，本審查不以靜態閱讀替代。

## 主協調者修正回讀（2026-10-05）

- P2-1：在第一篇正常操作流程首次遇到 API／SDK 時補上兩者角色關係，讓讀者先定位名詞，再進入後續契約細節。
- P2-2：改為「每層最多三次總嘗試，包含第一次」，並明算 3×3×3，避免把三次嘗試誤讀成初次之外再重試三次。
- P2-3：Python 追蹤模型事件現在攜帶 request ID 與 session；匹配的同 session 晚到結果依本例政策將 Unknown 更新為已確認，舊 session 結果仍不套用。下載說明明列此政策，與 C# 教學模型對齊。
- 修正 trace、sessions 與 audio 篇中的重複字和 callback 說明錯字。

以上內容已回讀。Python 五個情境重跑退出碼 0；九篇教材 JSON、目錄連結和 `git diff --check` 需在整合最終版再次跑。C# 專案本輪仍未能建置：目前環境沒有 .NET SDK。
