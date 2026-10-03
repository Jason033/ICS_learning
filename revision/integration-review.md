# 整合程式閱讀主題：重寫與驗證

此主題的問題是：陌生 C# 程式有多層封裝與回呼，讀者若只會找方法名，無法判斷執行的是哪個物件、回傳代表什麼、後續結果應套到哪次操作。重寫先建立語言與執行模型，再連到介面契約、生命週期、狀態與證據。公司名詞僅保留為查碼入口，沒有宣稱公司拓撲。

| 章節 | 正文建立的能力 | 例題與模型驗證 | 兩種以上故障機制 |
| --- | --- | --- | --- |
| trace-an-action | 呼叫堆疊、參數、參照、同步回傳與事件返回 | SetLevel 正反路徑；A/B 實例錯配 | 未走入口、錯實例、通知未匹配 |
| layers-and-ownership | 型別與實例、介面與實作、構造依賴、owner 與借用 | 同介面參照；替換全域參照不搬移內部依賴 | 越界 Dispose、殘留訂閱、替換錯範圍 |
| read-an-icd | 語法、型別、單位、缺欄與零、接受與完成 | 0/100 邊界；JSON 缺欄與值零 | 單位轉換、大小寫、版本契約 |
| commands-and-events | 委託、事件、同步與非同步、建立觀察順序 | 亂序兩請求；重複終態 | pending 太晚、重複訂閱、錯 ID |
| sessions-and-state | 狀態維度、guard、世代、快照與終態 | 重連准入；當前 ID 配舊 session | 舊回呼套新狀態、旗標未重新確認 |
| audio-and-device-boundaries | 控制與資料路徑、PCM 單位、緩衝所有權、雙向連接 | Tx/Rx 交集；樣本與位元組推導 | 缺 Capture、附著錯、格式/佇列 |
| correlate-evidence | 身分階層、紀錄位置、時間基準、因果與對照 | 分段時間；錯 session 與時鐘 | 派送延遲、接收時間誤標、舊清理 |
| errors-and-recovery | 例外控制流、拒絕/未知、部分成功、重試語意與预算 | Unknown 晚到；IOException 保留 | 重試放大、重複訂閱、越界清理 |
| integration-troubleshooting | 以意圖與契約組合三種圖和有限修正 | 正確 ID 錯實例；當前 ID 舊世代 | 單位、世代、UI 等待、媒體缺口 |

九篇正文段落各約 4,550–4,725 字元，不含程式、表格、題解；每篇兩個完整 C# 例題、追碼實作、八道分層题與多段答案，題目第 4–8 題提供不同於例題的資料。章節可獨立讀懂，再用相同模型把觀念串起來。舊 Python 資源與網址保留；新增資源是 `integration-maintenance-lab` 和 `integration-demo-icd.md`。

主要依據是 Microsoft 的 objects/interfaces/delegates/events、DI 生命週期、Dispose、async、合作取消、JSON、例外、Stopwatch、Logging/Activity 和 Call Stack 文件；NASA 介面管理支持契約分工；AWS 幂等重試與 Google SRE 支持重試風險。每篇 4–5 個一手來源，已讀原文件；模型政策及數值是原創明示假設，不是標準對所有系統的要求。

驗證於 .NET 10.0.401 臨時副本進行，公開資產沒有 bin/obj。九情境 `all` 全部通過，另將正文 18 個 C# 程式逐一合併 Lab 型別編譯執行：初次查到 JSON 字串引號錯誤，修正為 C# raw string 後輸出 `MissingValue`／`Valid:0`，其餘通過。補了共享控制器 request 隔離驗證。

對 session guard 的故障注入先發現原測試只驗總數，舊結果誤套後合法結果被忽略也會相同計數；因此新增中間狀態斷言 `current_id_old_session_is_rejected`。刪除 session 判斷後確實 FAIL、exit 1，復原後 all PASS、exit 0。這讓回歸能真正區分機制，非只重述程式。

限制：模型沒有公司 DLL、网络、序列埠、音訊或硬體，驗證的是明示 Lab 契約的控制流；回呼在測試呼叫執行緒同步觸發，不能據此推斷公司 SDK 執行緒約定。真實結果語意、去重、取消、重連與實體效果，仍須實際版本文件和授權測試。後續將由另一作者審正文前置與陌生題獨立可答性，再由主代理處理全站導覽、時間估計與打包。
