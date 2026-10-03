# Socket 第二版：基礎正文與維護讀碼覆核

舊版把關鍵概念壓成少量句子，讀者難以從端點一路推到程式、框架和業務結果。此次八篇整篇重寫：先教定義、需要它的原因與逐步機制，再用已給足規則的資料追算。C# 模型是驗證輔助，不能取代正文，也不是公司 API。

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

八篇各至少兩完整例題、實作與同症狀分叉診斷。練習共65題，基礎、應用、診斷三層；回答以多段證據、規則、推理和驗證組成，不以一個術語結束。閱讀時間由主代理按最終正文統一估計；操作及作答估值對應本章任務，尚未計時校準。

## 來源核對

透過網頁讀取核對 Microsoft .NET 10 Socket/AcceptAsync/SendAsync/ReceiveAsync、Shutdown/Connected/Poll/ReceiveTimeout、UdpClient Connect/ReceiveAsync、DualMode、BinaryPrimitives、UTF8Encoding、Encoding.GetByteCount、整數型別、CancellationTokenSource與TaskCompletionSource；並讀RFC9293/RFC768、Pipelines、Stopwatch、netstat、dotnet-stack、Activity遙測及Microsoft Azure重試模式。例題、協定及因果追算是原創教學資料。Windows/Framework版本差異與平臺截斷行為沒有寫成單一 通用承諾。

## 恢復後實際驗證

.NET 10.0.401 在 `/tmp/ics-sockets-verify` 副本編譯，0警告0錯誤。完整確定性parser與127.0.0.1 TCP/UDP基準exit0，原始輸出儲存為 `docs/assets/labs/sockets-maintenance-lab/expected-output.txt`。初次受限制執行被本機Socket許可權阻擋，之後經執行環境授權改在允許回環的工具模式實跑；這是環境限制，未改教材程式去繞過或連公司網路。

獨立變因在副本進行：`[11]`、11個1兩種合法切片均exit0；清空第一框後所有pending、用字元數宣稱byte長兩種故障均exit134且有相應斷言/解析失敗。復原後完整基準再次exit0。暫時Port與READ分段不固定，因此不把整份stdout逐字相等作測試。

跨審作者 radio_finish 已全文讀首章、框架核心、末章，獨立算五道陌生題。已修兩個缺口並由其複核：非空接收buffer返回0的EOF前提；Socket/世代必須同一受保護快照，不能把示意片段當並行完整方案。另校訂環形緩衝與每流程資源等白話用語。

## 界線

未驗證外部丟包/MTU、Windows特定異常順序、真實公司的重連狀態機、Ozeki DLL、操作去重、UI框架、裝置或負載效能。這些用規則與時間線讀碼追算，明示為教材情境。沒有根據公司RCSCall/iCallAPI名字杜撰依賴；教材成功不能代表公司系統成功。
