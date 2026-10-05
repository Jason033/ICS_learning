# Socket v3：基礎觀念、程式維護與驗收覆核

更新日期：2026-10-04

舊版把關鍵概念壓成少量句子，讀者難以從端點一路推到程式、框架和業務結果。此次八篇整篇重寫：以一筆資料流程交代各段責任，再依章節需要深入 .NET API、狀態機和故障證據。已知的資工概念不重教完整課本定義；缺口放在讀碼與實際資料判讀中補足。C# 模型用於可重跑驗證，不能取代正文，也不是公司 API。

| 章節 | 建立的觀念與維護能力 | 例題／驗證與故障邊界 |
|---|---|---|
| socket-flow | IP/Port/物件/連線身分、位址家族、兩方向所有權、listener/accepted、緩衝與完成階段 | 兩客戶同Port仍獨立；部分Send offset；背景使用與Dispose；完整框傳送不可交錯 |
| tcp-udp-apis | 串流/資料報、UDP Connect、空資料報/EOF、截斷、MTU、流量控制/壅塞 | 八bytes兩模型；TCP半關閉 vs UDP零長與取消；來源與request關聯 |
| message-boundaries | 為何邊界無法由Send/封包/Read猜出；三格式、狀態、不變條件、背壓、記憶體壽命 | 11bytes四切片逐步輸出；框架中/框架間EOF；清字尾、複用buffer與跨世代parser |
| binary-protocols | bit/byte/hex、偏移、有號/無號、端序、縮放、旗標、UTF-8與可變欄位 | 六byte狀態各中間值；框架/編碼錯誤分門檻；往返不能取代獨立已知樣本 |
| timeouts-and-readiness | 阻塞/非阻塞/非同步、就緒的多種條件、總期限/子期限、取消、單接收與一次完成 | 600ms剩餘預算；可讀後EOF/錯誤/半標頭；外層逾時留下讀取競爭 |
| disconnect-and-reconnect | 兩方向終止、Connected限度、未知結果、冪等、世代/所有權、同步快照與增量 | 三條命令結果時間線；舊回呼汙染；退避/總預算；TIME_WAIT不是物件洩漏證明 |
| logs-and-debugging | 正常路徑、ID階層、觀測工具範圍、本地/跨端時鐘、錯誤上下文、可反駁假說 | 405ms拆段與340msUI等待；總bytes正確仍覆寫；取樣缺行不能推未執行 |
| socket-troubleshooting | 首先讀物件/資料/退出責任；第一破壞點、反事實、有限修正、多維驗收 | 完整兩示例；新A/B/C三組期限/端序/特殊值/重複/關聯資料，共9題 |

八篇各有一個完整主例，並依章節加入程式讀碼、反例、測試或整合故障推演；目前共有33道練習。題目要求使用正文規則、資料和證據推理，不以背名詞作答。時間尚未經使用者實際計時校準，故不以估計分鐘數代表已驗證教學成效。

## 來源核對

透過網頁讀取核對 Microsoft .NET 10 Socket/AcceptAsync/SendAsync/ReceiveAsync、Shutdown/Connected/Poll/ReceiveTimeout、UdpClient Connect/ReceiveAsync、DualMode、BinaryPrimitives、UTF8Encoding、Encoding.GetByteCount、整數型別、CancellationTokenSource與TaskCompletionSource；並讀RFC9293/RFC768、Pipelines、Stopwatch、netstat、dotnet-stack、Activity遙測及Microsoft Azure重試模式。例題、協定及因果追算是原創教學資料。Windows/Framework版本差異與平臺截斷行為沒有寫成單一 通用承諾。

## 先前獲准執行的實際驗證紀錄

.NET 10.0.401 在 `/tmp/ics-sockets-verify` 副本編譯，0警告0錯誤。完整確定性parser與127.0.0.1 TCP/UDP基準exit0，原始輸出儲存為 `docs/assets/labs/sockets-maintenance-lab/expected-output.txt`。初次受限制執行被本機Socket許可權阻擋，之後經執行環境授權改在允許回環的工具模式實跑；這是環境限制，未改教材程式去繞過或連公司網路。

獨立變因在副本進行：`[11]`、11個1兩種合法切片均exit0；清空第一框後所有pending、用字元數宣稱byte長兩種故障均exit134且有相應斷言/解析失敗。復原後完整基準再次exit0。暫時Port與READ分段不固定，因此不把整份stdout逐字相等作測試。

跨審作者 radio_finish 已全文讀首章、框架核心、末章，獨立算五道陌生題。已修兩個缺口並由其複核：非空接收buffer返回0的EOF前提；Socket/世代必須同一受保護快照，不能把示意片段當並行完整方案。另校訂環形緩衝與每流程資源等白話用語。

## 界線

未驗證外部丟包/MTU、Windows特定異常順序、真實公司的重連狀態機、Ozeki DLL、操作去重、UI框架、裝置或負載效能。這些用規則與時間線讀碼追算，明示為教材情境。沒有根據公司RCSCall/iCallAPI名字杜撰依賴；教材成功不能代表公司系統成功。


## 2026-10-04 最終覆核

六篇（`message-boundaries`、`binary-protocols`、`timeouts-and-readiness`、`disconnect-and-reconnect`、`logs-and-debugging`、`socket-troubleshooting`）完成獨立技術／銜接覆核，沒有重大正確性問題；另兩篇由第二位覆核者逐段核對，完成修正後讀回確認，八篇均無未解決的阻擋問題。審查指出的 .NET 目標版本差異已在對應範例旁補註；整合除錯篇另明示 Receive 工作保存的 `socket` 參數，與 `finally` 重新讀取的可變 `currentSocket` 可能是不同物件。覆核者確認這段錯誤程式碼足以讓讀者從物件來源推出誤關新連線的原因。前兩篇經另一位覆核者核對官方 .NET 文件與 RFC，修正了 handler 未處理例外會終止 listener、UDP 取消例外的上拋路徑，以及補課章名不一致；讀者位置已對齊目錄正式標題，服務 token 取消也標為整體服務的正常停止。覆核者回讀兩項控制流程修正後，確認 API 語意與說明一致。

本次把教學專案複製到 `/tmp/sockets-maintenance-lab-final`，以 .NET SDK 10.0.401 建置成功（0 警告、0 錯誤）。另抽出 `socket-flow` 的 `ServeAsync`／handler 和 TCP／UDP API 範例，置入含必要應用 stub 的 .NET 10 harness 編譯；0 警告、0 錯誤。執行時，確定性 parser 測試全部通過：分段／合併 frame、UTF-8 byte 長度、超限長度拒絕、中途 EOF、非法 UTF-8；隨後在建立 `Socket` 時收到 `SocketException (13): Permission denied`。因此本次確認了建置、摘錄程式編譯與 parser 行為，但沒有在此受限環境重驗 loopback TCP／UDP。前節的授權執行紀錄保留為先前環境的測試，不等同本次回歸。
