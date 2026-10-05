# 新版教材起點、技術與銜接審查

審查日期：2026-10-04。這是第一階段 28 篇網路、Wireshark、Socket 與 RS-232 新版教材的專項審查快照，不是全站目前版本的狀態報告。後續六個核心系列另有各自的作者檢查和獨立審查，總覽見[目前改版紀錄](README.md)。

## 審查目標

使用者確認一般資工系的常見概念大致熟悉，主要缺口是把概念接成端到端流程，並用它讀程式、判斷故障和修正既有系統。因此審查重點是：課文是否把概念放回同一次操作；讀者能否分辨程式、傳輸及觀測工具各自代表什麼；練習是否能由正文推理，而不是背名詞或重算同一組數字。技術審查另核對協定、API、byte數值、PCAP 和例子的限制。

## 各篇建立的能力

| 篇章 | 審查結論 |
| --- | --- |
| 一筆網路請求如何抵達伺服器 | 用一筆 HTTP 查詢交代端點、TCP、IP 路由、本地鏈路、服務程式及回程的交接；ACK 的結論不延伸成業務完成。 |
| 在封包中讀取應用資料 | 連起本文、UTF-8 bytes、HTTP 長度和 socket 讀取範圍；Wireshark 操作枝節已移到 Wireshark 主題，留下與 bytes 推理直接相關的內容。 |
| 判斷資料停在哪個交接點 | 以程式、兩端封包和紀錄區分送出、讀取、HTTP 解析、handler、呼叫端與 UI 完成；範例的三個故障時間線彼此獨立。 |
| TCP、UDP與應用程式回覆 | 從資料時效和順序需求連到 Socket API；區分呼叫返回、傳輸確認、遠端處理及業務回覆。 |
| IP、Port與端點 | 從名稱解析追到實際目的地、本機來源端點、伺服器 Bind 範圍和 IPv4／IPv6 listener，並將錯誤放回相應階段。 |
| Wireshark第一次抓包 | 有離線正常 PCAP 作練習，並把另一個「client 逾時」明確標成假設故障，避免讀者在正常附件中找不到該事件。 |
| 擷取與顯示篩選器 | 逐步用已知 frame 驗證篩選；說明 `ip.addr` 和 `tcp.port` 各自符合任一端點，不能將它們誤當精確的 server 配對；DNS 定位與後續 HTTP/TCP 篩選分開。 |
| 封包列表、細節與原始位元組 | 區分 record 保存長度、原始 bytes、協定標頭宣告和解析器推算；練習從截短 TCP frame 遷移到完整 UDP frame，檢查方法是否能用於不同長度案例。 |
| TCP重試、ACK與無回應 | 跨 frame 比較建立前 RST、SYN 未見回應、ACK、同序號重送及 server 廣告的接收窗口；將 SYN 無回覆結論限於本檔觀測範圍，並提醒全文使用原始序號欄位。 |
| 封包與程式紀錄整合練習 | 以 request ID 和共用時間起點對照 q17 的 bytes／程式探針，再用 q18 逾時與 q20 擷取缺口練習區分已知和未知；不同 PCAP 是獨立合成案例，不共用現場時間線。 |
| Wireshark統計：方向、時間窗與分母 | 先定義母體，再由端點縮到對話、TCP stream或分方向 I/O Graph；用20 ms和1秒時間桶比較總數相同但分布不同的實例。逐題標清frame數、frame bytes、速率分母和單點證據限制。 |
| Wireshark：SIP／SDP協商與RTP方向 | 依 RFC 3261/3264/8866/3551 追 INVITE、Dialog、ACK、BYE、有效 SDP 媒體接收端點與 PT0/PCMU；兩份PCAP各自驗 frame／方向，不跨檔拼時間線。單向未觀察與播放不能證明均有明確界線。 |
| Wireshark：RTP序號、到達時間與抖動 | 按單一方向／SSRC／時間窗保留到達順序，核對範圍缺口、重複與晚到；以RFC 3550同一時鐘單位重算兩次 jitter 更新。混合／參照檔不是同步雙端，固定FF payload不代表真人音訊。 |
| 先看懂一通電話的物件與事件 | 從程式入口追 listener、accepted Socket、handler 和釋放範圍，判斷連線及錯誤影響到哪一層。 |
| TCP 與 UDP 的 Socket 呼叫 | 從 API 返回數量、EOF、空 datagram 與例外路徑判斷下一步；不把呼叫完成說成遠端功能成功。 |
| 訊息邊界與緩衝 | 用跨讀取 parser 狀態還原完整 frame，並分辨 EOF、取消、截斷和非法長度。 |
| 二進位協定與位元組順序 | 依欄位表逐一解析完整 body，核對 offset、signed 值、端序、flags、長度及嚴格 UTF-8。 |
| 阻塞、逾時與可讀寫通知 | 追查 deadline 到實際 I/O 的取消傳遞，區分單筆 waiter 和共用 reader 的生命週期。 |
| 斷線、重試與重連 | 依命令是否可能已生效判斷能否安全重送，再用 session generation 隔離舊工作的遲到結果。 |
| 用 Socket 紀錄定位問題 | 按 request／connection／operation 關聯階段時間，分清本地 API、封包、parser 和 UI 證據。 |
| Socket 整合除錯練習 | 由重連後狀態倒退追到 finally 的物件所有權，再設計針對性的回歸順序；另用錯位 parser 值驗證資料保留。 |
| RS-232、串列設定與命令是三件事 | 從C#呼叫、編碼bytes、串列資源和設備解析追到回覆；前置使用已知byte概念，不重教二進位定義。 |
| UART參數與字元傳送 | 對照程式生效設定與設備契約，區分傳送時間、flow等待、字元錯誤和接收服務量。 |
| 資料格式、檢查碼與逾時 | 沿實際buffer狀態推算分段與多框架，判斷長度、校驗及回覆是否仍在期限內。 |
| 串列工具與軟體回環 | 區分loopback、設備回覆、通知次數與Read實際資料數；以單一接收者避免資料被多路消費。 |
| 串列通訊整合除錯 | 從設定、raw bytes、訊息、期限和業務狀態提出可驗證假設與有限回歸。 |
| 電氣介面與接線文件（選讀） | 軟體層證據不足時才查轉接器和設備型號、訊號方向與觀測邊界；不要求硬體設計。 |
| RS-485、SPI、I²C與通訊橋（選讀） | 依現有程式理解方向控制、交易完成和橋接後的狀態；不計算終端或電流。 |

## 重要審查發現與處理
RS-232系列另由一位技術審查者核對UART與I²C機制、時間例題、.NET API語意和來源；一位獨立讀者審查核心路徑、選讀標記及C#範例用語。Device-Lab-A的各篇算例統一為20ms設備處理和39.7917ms理想下界。電氣設計公式、反射、終端和拉昇電流計算已移出必要閱讀；使用者只在軟體證據指向介面問題時需要讀選讀篇。完整範圍及模型測試見[RS-232驗收紀錄](../serial-review.md)。


第一輪及後續審查移除了會讓讀者誤判責任的說法：HTTP 用戶端函式庫與服務端處理程式分開描述；async C# 範例的回傳型別與 JSON null 契約修正；TCP／UDP 的 EOF、空 datagram、部分讀取和來源過濾分別說明；Wireshark 的重組資料不再與單一 frame 混為一談。Addresses、Bind 與 DualMode 部分也更正了本機 IP 只匹配目的地址、`::1` 與 `::` 不相同，以及取消和網路逾時的差異。

Wireshark 作者檢查和獨立審查另外修正以下問題：

- **假設與附件分開。** 13-frame HTTP 練習檔包含正常 HTTP 200；抓包章後段的 client timeout 是另一個假設故障，不是該檔案內的事件。
- **跨案例的 ACK 方向寫清楚。** q17 的 server→client ACK 確認 client→server request 序號；q20 的 client→server ACK 確認反方向的 server response 序號。題目直接標出 frame、發送端和被確認的序號，降低讀反風險。
- **使用練習確認能否遷移。** 封包長度章不再重複三題 67／64-byte 的同一算式；新增 49-byte UDP frame，讓讀者分清「這筆 record 完整保存」與「整份擷取沒有遺漏」是兩個不同主張。
- **TCP 序號基準明示。** Wireshark 可用相對序號顯示 SYN 的 0；TCP 診斷篇先說本文數值是 `tcp.seq_raw`／`tcp.ack_raw` 原始欄位，不能把兩種數字直接混比。
- **自訂請求長度完整交代。** q17、q19 的 13-byte TCP 資料包含 2-byte `00 0b` 長度前綴及 11-byte `REQ q17 GET`／`REQ q19 GET` 本文，不把本文誤稱為整個 payload。

網路資料表示篇的 `Content-Length: 17` 是十進位文字，線上對應 bytes `31 37`；整數 17 的單 byte `HEX 11` 是另一種欄位表示。這項區分已放在 bytes 篇正文的 HEX 例子中，並將同一點原本造成的章節旁支刪除。

新增三篇Wireshark教材的獨立審查確認：SIP 2xx INVITE 與 BYE 的相同 `200 OK` 按 CSeq 方法區分；SDP 的接收端點方向反轉為對端目的；RTP jitter 按封包到達順序及8000 Hz時間單位計算，兩次手算結果與素材吻合。維護混合PCAP中未觀察到某方向媒體，不推成發送端、路徑或播放根因。統計教材明確記錄各工具母體、協定層、方向與時間分母，圖表用來定位後續frame而非直接診斷根因；審查也核對20 ms空桶要以Line／Bar呈現及維持擷取相對時間。已補上截短frame時原始frame長度可能高於檔案保存bytes的邊界。審查後已把SIP教材重複的導言／結尾精簡，並為RTP分析補上工具loss summary與唯一序號缺口不一致時應先檢查重複封包與統計定義的提醒。



Socket 系列另經逐篇技術／銜接覆核：框架 reader 與 ZIP 中 UTF-8 `FrameParser` 的回傳型別／EOF 介面不同，正文已明示；整合案例以程式碼呈現舊工作的 `socket` 參數與可變 `currentSocket`，使物件錯關原因可由讀碼驗證。覆核也促成 .NET／C# 版本提示、listener 上未處理 handler 例外會使整個服務迴圈退出的說明，以及 UDP Receive 取消例外的明確處理。更多章節責任、數值、來源及當前執行限制見 [Socket 覆核紀錄](../sockets-review.md)。

## 素材與驗收範圍

13-frame HTTP `/status` 檔供抓包與篩選練習；三份 171／172-frame 維護、截短與 reference PCAP 和 `logs.csv` 供後續不同案例使用。這四份新教材檔案的 frame number、端點、request ID 和時間基準不可拼成一條真實交易。練習數值已依 PCAP record、manifest 和 CSV 核對；`tests/verify_labs.py` 驗證三份既有教學 PCAP，`wireshark-maintenance-verify.py` 另驗證三份維護 PCAP，格式、時間、checksum 和對應內容均通過。

在該審查快照中，網站結構測試涵蓋 16 個主題、126 篇目錄項目、28 篇新版和 98 篇舊版；結構警戒為 0。q17 的 C# 片段在 .NET SDK 10.0.401 編譯成功；Socket 教材專案也在 .NET 10 建置成功，parser 測試通過，但當次 loopback Socket 建立受執行環境權限拒絕。RS-232 模型程式在 .NET 10 建置成功，10 組確定性案例與預期輸出相符。當時工作區沒有 Playwright／Chromium或 Wireshark／TShark，因此未做畫面回歸或原生 Wireshark dissector／GUI 驗證。

## 驗收狀態和限制

這 28 篇教材已完成作者檢查與該階段安排的技術／銜接審查，上述發現均已修正。這證明課文資料、內部推論和列明的素材彼此相符，不能證明使用者已學會，也不能代表任何公司程式已被除錯。使用者本人試讀尚未進行；公司程式、部署、Ozeki 版本及硬體資料也未提供，所以教材沒有推定公司架構或根因。

這份紀錄寫成時，本機工作樹和公開網站狀態仍有差異。當前主分支與網站驗證請以[目前驗收資料](verification.json)及[發布核對](publication-verification.json)為準；[browser-verification.json](browser-verification.json)是先前執行紀錄，不代表本輪瀏覽器畫面已通過。
