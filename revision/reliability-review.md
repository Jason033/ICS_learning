# 可靠性與並行處理：新版教材檢查紀錄

這九篇是給已接觸一般資工概念、但需要把程式路徑串起來的新進軟體工程師。每篇都從一次操作會經過的本地佇列、adapter／傳輸、遠端執行、回報和狀態更新切入；讀者可按當下問題選讀，不必先修完整系列。所用的服務流程與識別值是教學模型，不代表公司的實際系統。

## 九篇各自建立的維護能力

| 章節 | 追蹤的工程問題 | 讀完應能判斷 |
| --- | --- | --- |
| 逾時與期限 | 操作從本地排隊到回報的等待邊界 | timeout 限制了哪一段等待；遠端結果仍有哪些可能 |
| 重試與冪等 | 同一意圖的多次嘗試與外部副作用 | 重送會否建立重複結果；何時查證比再次執行安全 |
| 活性與恢復 | 連線、應用會話和業務能力的恢復鏈 | 哪個探測只證明局部仍在回應；何時才可報告 Ready |
| 佇列與背壓 | 生產、排隊、處理與滿載政策 | 積壓是在等待、處理還是丟棄；改容量會否只延後失敗 |
| 事件順序 | 事件、快照、來源、會話和版本 | 哪些資料可比較；晚到值能否覆蓋目前狀態 |
| 共享狀態與並行處理 | 多條執行路徑修改同一物件 | 真正共享的是哪個物件；同步範圍要保護哪項不變條件 |
| 非同步事件與派送流程 | callback、Task、await、執行緒和 UI 派送 | 工作卡在等待、派送還是遠端；哪個續體仍未執行 |
| 觀測與負載 | 單筆操作、跨階段追蹤和整體統計 | 延遲發生在哪個區段；修正前後比較是否用同一口徑 |
| 可靠性故障推理 | 上述機制在同一組故障資料中的交互 | 哪項觀察能區分假說；如何驗收修正與恢復責任 |

## 教材編排與審查結論

每篇開頭都先交代它觀察流程中的哪一段，並補上讀懂後文所需的背景。相連的短段落已合成較完整的教學單元；完整算例、程式片段、表格和故障資料仍保留。練習不再用「新案例」等編輯標籤，並保留基礎理解、應用推導和故障判斷層次。

重點放在操作狀態如何改變，而不是單獨背名詞。例如 timeout 只代表本地等待條件未成立；相同時間點，遠端可能未收到、仍在處理，或已完成但回覆尚未到達。重試章接續判斷 OperationId、attempt 和副作用；恢復與佇列章則分別處理新連線的 readiness，以及工作如何累積或被拒收。每篇有可選延伸閱讀，但自身的前置段先重建必要流程。

## 驗證方式和結果

- 九篇均符合 `contentVersion: 3` 結構檢查，含有效段落、來源及具答案的分層練習；教材不含舊版 `studyTime`。
- catalog 同步後，全站 `python3 tools/audit_content.py` 通過：126 篇、0 項結構警戒；`node tests/smoke.cjs` 通過首頁、主題、完整教材、進度與規劃頁互動檢查。
- `python3 tests/verify_labs.py` 通過三份教學 PCAP 的格式、時間、checksum 及協定案例檢查。`git diff --check` 對本次檔案通過。
- `python3 tests/verify_csharp_labs.py reliability` 未能啟動：執行環境沒有可執行的 `dotnet`（回報 `PermissionError`），所以此次 C# 程式與更新後的預期輸出未能實際編譯執行。Lab 模型不連接設備或網路，也不是效能測試；在可用的 .NET 10 SDK 環境仍應執行 `dotnet run -- all`。

本次核對的 .NET 行為以 Microsoft 文件為依據：[`Task.WaitAsync`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync)、[有界 Channels 的背壓與滿載政策](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)、[async/await 常見陷阱](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/common-async-bugs)、[C# `lock` 語句](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)。本文明示 `Task.WaitAsync` 的目標框架相容性；Channels 和其他 .NET 範例仍須按實際專案的 `.csproj`、套件與語言版本確認。

## 獨立精確性覆核後的修正

- 在事件順序章補清楚：同一個 async consumer 若逐筆 `await ProcessAsync(item)`，仍會等前一筆完成才進下一筆；`await` 本身不會創造重疊。未等待的 Task、多個 consumer 或重入入口才可能讓處理重疊。
- 逾時章依 [.NET 10 `Task.WaitAsync` API 文件](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync?view=net-10.0)修正措辭：回傳的是代表該等待的 Task，可能與原 Task 是同一實例。等待逾期或取消也不代表底層操作已停止。
- 負載例題把 20 次／秒明確定義為首輪意圖；每個意圖執行三次總嘗試時，總量為 60 attempts／秒。單 consumer 每次服務 30 ms 的理論上限約 33 attempts／秒，已標出單位與簡化假設。
- 將環形緩衝段的「行動資料」修為「搬移資料」，並更正「可關聯的系統紀錄」用語。
- Lab 的 async 預設 handler 改為 50 ms；依序推演後，狀態 callback 等待 30 ms、心跳不需排隊，最後一項於 101 ms 完成。`expected-output.txt` 同步更新。deadline 輸出改用實際變數，將樣本期限改成 280 ms 時訊息也會一致。
- 並行題改成 `volatile int` 與 `volatile string` 這類欄位個別存取可原子的情境，避免用 C# 不允許宣告為 volatile 的 `long`，並保留題目核心：兩欄各自原子仍不保證複合 `Apply` 更新一致。

## 適用範圍與限制

這些內容能幫助讀者提出可驗證的故障假說，不能代替公司程式碼、SDK 契約、部署設定或設備文件。練習資料皆為合成案例；共享記憶體的去重示例不提供跨程序、崩潰或持久化保證。真正修改服務前，仍需確認相關版本、操作所有權、回報來源和安全恢復條件。
