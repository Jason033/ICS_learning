# Ozeki 與 VoIP 系列交叉覆核

覆核日期：2026-10-05
範圍：Ozeki 本機音訊控制 7 篇、VoIP 與聲音 9 篇。逐篇檢查正常流程、段落順序、必要前置、程式示例和練習解答，並以 Ozeki 官方 API／版本紀錄及 IETF RFC 原文抽查技術主張。案例地址、通話識別值和量測都是教材合成資料；這份覆核不推測公司部署，也沒有修改教材。

## 審查結論

兩個系列的主線大致連貫：Ozeki 從辨認版本、追本機媒體圖、管理資源，再接到通話媒體邊界及診斷；VoIP 從一次呼叫總覽，逐步進入註冊、SIP 狀態、SDP、RTP、音訊交接、封包化、播放時序和整合除錯。大多數練習的計算與證據推論正確，並清楚區分「命令已送出」「封包已到」和「使用者實際聽見」。

前次覆核列出的三項問題已重新對照目前教材，均已在正文加以修正或明確限縮：SDP 現按有效方向解讀 RTP 埠並補上 sendonly 的 RTCP 例外及 PT 映射；SIP 示意模型已宣告必要欄位並說明 response method 來自 CSeq；Ozeki 篇已明列 1.8.12 與 1.8.14 修正紀錄的差異。這三項不再列為待修。

仍有一項需保留為教材限制：SIP 範例刻意不是完整的 transaction/dialog matcher。sendonly 練習已補齊 answer 的動態 PT `rtpmap`。公司實際 SDK、wrapper 和執行版本仍未知，公開文件不能替代現場契約。

## 前次發現問題的複核狀態

### 1. SDP 方向、RTCP 例外與 payload type

位置：[SDP 與媒體協商](../docs/content/lessons/voip--sdp-offer-answer.json)，第 3、4 節。現在明確限定雙向位址例子是 `sendrecv`，並說明要先依有效方向確認 RTP 是否允許；sendonly RTP offer 的位址／埠間接表示 offerer 接收 RTCP 報告的位置，不能當反向 RTP 接收埠。正文亦補上 answer 可對同一 codec 使用不同 dynamic PT，送出端需依 answer 映射發送。

第二次讀回另補足 sendonly 練習的 answer：目前 offer 和 answer 都明列 `a=rtpmap:101 opus/48000/2`，不再將有動態 PT 101 的片段留作看似完整但缺 mapping 的 SDP。這符合 RFC 3264 §6.1 對 answer 動態 PT mapping 的要求。

### 2. SIP 呼叫流程的 C# 示意模型

位置：[一通 SIP 電話的流程](../docs/content/lessons/voip--sip-call-flow.json)，第 6 節。前次指出的 `PendingInvite.CallId` 未宣告問題已修正：模型現在保存 `CallId`、`FromTag` 與 INVITE 的 CSeq；回應模型明列 `CSeqMethod`，旁文指出該值取自 response 的 CSeq 標頭，而 response 起始列沒有 method。

剩餘限制：該函式只比對 CSeq method／數字、Call-ID、From-tag，未完成 Via branch、方向與完整 dialog/transaction 規則；教材已明確標為簡化模型，不能用來處理封包或取代 SDK。此限制與編譯層的欄位錯誤不同，適合作為閱讀示意保留。

### 3. Ozeki Receiver 串接版本風險

位置：[電話中的音訊邊界](../docs/content/lessons/ozeki--calls-and-events.json)，第 2 節、示意程式與練習。教材現在將 1.8.12 的型別繼承關係限定為資料方向教學，並明說這不是執行保證；引用官方 1.8.14 修正特定 Receiver→MediaHandler 例外的紀錄，提醒重現前先核對實際 DLL／套件版本。練習答案也要求核對版本。

此項已修正，不再要求改教材。剩餘限制是公司實際 Ozeki 版本與 wrapper 仍未知，故本章只對照公開版本資訊，不能據此判定公司部署有無該例外，也未宣稱已在任何公司設備重現。

## 逐篇覆核紀錄

| 系列／篇章 | 覆核結果 |
| --- | --- |
| Ozeki：先核對版本與契約（`documentation-and-version`） | 版本、套件、執行組件與公司 wrapper 的證據層分得清楚。公開下載頁目前列 10.5.1，歷史版本區同時保留 11.x／12.x 條目，且本系列所用多個 API 頁標示 1.8.12；要求讀者回到實際 DLL／部署證據是必要的。 |
| Ozeki：追本機音訊資料流（`read-a-call`） | `Connect(sender, receiver)` 的方向、初始化／啟動證據和實際可聽驗收分得清楚。1.8.12 `Microphone`、`Speaker` API 對型別、Start/Stop、格式與裝置狀態的描述相符。 |
| Ozeki：MediaConnector 拓樸（`media`） | 分流、合流和錄音點位置沒有混為一談。Ozeki 發行紀錄提及 10.4.58 多接收端轉送修正及 11.0.0 多來源不必經 mixer；教材有正確保留版本條件，沒有宣稱所有 `Connect` 都自動混音。 |
| Ozeki：生命週期與事件（`lifecycle-and-events`） | Stop、Disconnect、Dispose 各自範圍、owner 和回呼競態說明正確；共享實體裝置的版本特例有明確標示。官方 10.3.193 發行紀錄支持 Start/Stop 會因共享裝置互相影響的案例。清理順序也明說須依實際 API 同步契約確認。 |
| Ozeki：音訊格式與觀測（舊 ID `registration`） | 章節正文現為格式／計量，不再教 SIP 註冊；PCM bytes、取樣數與區塊數的例題計算正確，也沒有把 Level 當 dB。舊 ID 為保留網址的技術債，入口顯示名稱應維持「音訊格式」以免誤導。 |
| Ozeki：電話中的音訊邊界（`calls-and-events`） | 本機 graph、通話 sender/receiver 兩種方向和 SIP/RTP 責任界線成立；`PhoneCallAudioReceiver` 的型別方向正確。1.8.12 API 型別說明已與 1.8.14 官方例外修正紀錄並列，並清楚限制為示意；剩餘限制是未知公司版本與未做實機相容測試。 |
| Ozeki：本機音訊整合診斷（`evidence-troubleshooting`） | 各案例固定操作與方向，先找第一個缺證交接；錄音支路、播放支路和通話方向的題解均合理。所有指標都標為合成，沒有將 SDK 不一定提供的計數器宣稱為真實 API。 |
| VoIP：通話建立與聲音傳送（`call-vs-audio`） | 呼叫控制、協商、RTP、解碼與本機播放分層正確；例題清楚限制證據能證明到哪裡。Call-ID、tag、CSeq、五元組在總覽中出現後，下一步有章節接續說明；可在總覽首次使用時加短標籤或跳轉，讓單篇選讀者更容易跟上。 |
| VoIP：SIP 角色與註冊（`sip-roles-registration`） | AoR／Contact、Registrar／Location Service、註冊有效期限和呼叫路徑分離清楚。401 challenge 再授權的流程與題解正確，沒有將第一個 401 誤當成錯誤密碼；呼叫時應查路由的建議適當。 |
| VoIP：一通 SIP 電話的流程（`sip-call-flow`） | transaction、dialog、2xx ACK、CANCEL/BYE、分叉和遲到回呼的流程大致符合 RFC 3261／6026；前次欄位與 response method 問題已修正。第 6 節仍是明確標示不完整的教學 matcher，不能拿來解析完整 SIP 封包。 |
| VoIP：SDP 與媒體協商（`sdp-offer-answer`） | session/media 層、方向屬性、port 0、靜態／動態 PT 與有效 SDP 版本的主體說明正確；sendonly 埠語義、answer PT remapping 和練習 answer 的 dynamic PT mapping 已補上。 |
| VoIP：RTP／RTCP 與封包判讀（`rtp-rtcp`） | sequence 繞回、timestamp 與到達時間區分、RTCP fraction-lost、jitter 與 RTT 證據邊界符合 RFC 3550。抽算 3%→欄位值 7、16/1024→欄位值 4，以及各練習答案的單位均正確。 |
| VoIP：從麥克風到喇叭（`audio-path`） | 本機媒體和網路媒體的責任界線不重教 Ozeki API；8 kHz mono 16-bit PCM 20 ms=320 bytes，PCMU payload=160 bytes 的推導正確，且有交代 callback buffer 不一定等於 RTP packet。 |
| VoIP：Codec 與封包化（`codecs-packetization`） | PCMU 的 10／20／40 ms payload、pps 和 IPv4/UDP/RTP 層速率計算正確；未把 payload bitrate 當 wire rate，也有解釋 ptime、codec frame 與 PT 的不同。 |
| VoIP：抖動緩衝與音質（`jitter-and-quality`） | RFC 3550 estimator 的四包示例和習題更新值已重算，結果正確；late、loss、buffer delay 與 clock drift 的界線也保留。時間線的 50 ppm × 600 秒 = 30 ms 正確。 |
| VoIP：VoIP 整合除錯練習（`voip-troubleshooting`） | 按同一呼叫、媒體方向和 SDP 代次追 sender→接收→decoder→本機交接，案例分支與題解合理。指出缺少資料不等於該段故障，並限制未知 SDK 與網路拓撲的推論。 |

## 篇章銜接與是否合併

目前兩個系列各篇的學習任務仍有區別，建議保留 7 篇 Ozeki、9 篇 VoIP 的入口與順序，不為減少篇數而合併：

- `ozeki--read-a-call` 建立本機 handler graph；`ozeki--media` 進一步判斷分流、混音和錄音接點；`ozeki--lifecycle-and-events` 處理 owner 和資源結束；`ozeki--registration` 專注本機格式與資料量。它們都使用同一條媒體圖，但每篇要完成的維護判斷不同。
- Ozeki 的 `calls-and-events` 與 VoIP 的 `audio-path` 都跨到通話媒體，卻在不同位置停下：前者讀特定 SDK 的本機 handler/call 附著，後者讀通用 encoder、RTP payload、decoder 和本機提交邊界。保留兩篇，互相連結即可；不要再於兩邊重複 SIP/RTP 全流程或 Ozeki 裝置操作。
- Ozeki 的格式篇、VoIP `audio-path` 和 `codecs-packetization` 都會算 bytes，但問題各不相同：本機 PCM handler 格式、PCM 到 codec payload 的交接、以及 RTP/IP 固定封裝成本。公式可以引用共同短參照，計算仍應留在其資料層範例內。
- 整合系列的 `read-an-icd`／`documentation-and-version` 與 Ozeki 版本盤點都談文件與版本；前者教跨廠商如何比較 ICD、API、程式和運行證據，後者提供 Ozeki 的具體版本盤點任務，適合互相連結，不宜合併。`commands-and-events`、`sessions-and-state`、`errors-and-recovery` 與 Ozeki lifecycle／VoIP SIP callback 也有共通的非同步和清理原則；整合篇是可重用通用方法，領域篇則承擔 SDK／SIP 專屬機制，保留兩層並避免複製相同長篇定義。

本次未逐篇覆核 PTT 或無線電系列，故不對這兩系列彼此是否合併作結論；其責任邊界應由相應主題的獨立覆核確認。

## 已核對的一手來源

以下來源直接支持本次核對的協定和公開 SDK 主張；它們只說明標準或 Ozeki 公開版本，不能證明公司目前部署：

- [Ozeki MediaHandlers 教學](https://www.voip-sip-sdk.com/p_7536-how-to-use-media-handlers.html)：source／destination、MediaConnector、媒體物件啟停與 call sender 邊界。
- [Ozeki MediaConnector API（頁面標示 1.8.12）](https://voip-sip-sdk.com/doc/html/fd3ef57a-c7f3-df17-509b-9d11890938e5.htm)：單向 sender→receiver、Disconnect／Dispose、格式轉換說明。
- [Ozeki PhoneCallAudioSender.AttachToCall（1.8.12）](https://voip-sip-sdk.com/doc/html/5743b1d9-48ea-6352-4019-09e205aa6191.htm)、[PhoneCallAudioReceiver（1.8.12）](https://voip-sip-sdk.com/doc/html/f5a4c883-82b4-f01a-1ed8-f319cb8d8a81.htm)：附著通話的責任與 Receiver 繼承 `AudioSender`。
- [Ozeki Microphone API（1.8.12）](https://voip-sip-sdk.com/doc/html/1acde4be-300f-ff36-9df0-54b1d2a849b8.htm)、[Speaker API（1.8.12）](https://voip-sip-sdk.com/doc/html/13fd25fe-5822-b770-de51-afd58fd7c14f.htm)：裝置格式、狀態、Level、Start／Stop 與事件。
- [Ozeki 下載與發行紀錄](https://www.voip-sip-sdk.com/p_7021-download-ozeki-voip-sip-sdk.html)：10.3.193 共享裝置行為、10.4.58 多接收端轉送修正、11.0.0 mixer 版本變化，以及 1.8.14 的 `PhoneCallAudioReceiver` 修正。
- [RFC 3261](https://www.rfc-editor.org/rfc/rfc3261.html) 與 [RFC 6026](https://www.rfc-editor.org/rfc/rfc6026.html)：SIP REGISTER／呼叫訊息、transaction/dialog、ACK／CANCEL／BYE 與 INVITE 狀態機。
- [RFC 3264](https://www.rfc-editor.org/rfc/rfc3264.html)：offer/answer、媒體方向、接收地址與 payload type 規則；特別是 §5.1 sendonly RTP 的位址／埠用途。
- [RFC 8866](https://www.rfc-editor.org/rfc/rfc8866.html)：SDP 語法及 session/media 層級。
- [RFC 3550](https://www.rfc-editor.org/rfc/rfc3550.html)、[RFC 3551](https://www.rfc-editor.org/rfc/rfc3551.html)、[RFC 5761](https://www.rfc-editor.org/rfc/rfc5761.html)、[RFC 7587](https://www.rfc-editor.org/rfc/rfc7587.html)：RTP/RTCP 欄位與計算、RTP/AVP 靜態對照、mux 條件及 Opus RTP clock/payload。
- [RFC 8760](https://www.rfc-editor.org/rfc/rfc8760.html)：SIP Digest 認證更新，與註冊章的版本說明相符。

自查：16 個 JSON 均在逐篇讀取時成功解析；核對了所有練習答案中可計算的 PCM、封包率、封裝速率、RTCP loss、RTP jitter 和時鐘漂移數值。教材未作任何修改；公司 DLL、wrapper、目標框架、真實設備及部署拓撲仍未知，因此本報告只對照公開契約和合成案例。
