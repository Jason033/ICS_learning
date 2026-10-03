# VoIP 第二版：從通話到聲音的概念閉環

本主題重寫九篇教材。先建立「通話意圖與狀態」和「連續音訊資料」的不同任務，再依角色、交易、協商、媒體、數字音訊、編碼、時序回到維修案例。實作是驗證概念的輔助，不把有聲或無聲直接歸因於 SIP，也不假設公司使用特定 SDK／網路拓撲。

|章節|正文要建立的因果關係|可獨立核對的例題與產物|維修時要分辨的機制|
|---|---|---|---|
|call-vs-audio|通話控制與媒體為何分工；數值到 byte 與封包的關係|PCM→PCMU→IP 帶寬逐層推導；雙向路徑與容量比較|接通、有媒體封包、可聽懂聲音是不同證據|
|sip-roles-registration|AoR、Contact、註冊租期、Proxy／Registrar／UAC／UAS|註冊更新時間線；401 challenge 後的新交易|過期位置、挑戰失敗、路由失敗不能混為密碼錯誤|
|sip-call-flow|交易與 dialog 的不同生命週期，tags／branch／CSeq、ACK、CANCEL／BYE|同 CSeq 不同方法與晚到 200；重送與新 INVITE 比較|取消競態、重送、晚完成與殘留通話|
|sdp-offer-answer|接收地址、動態 PT 對照、方向、逐 m-line 接受與拒絕|兩端接收埠與方向推導；不同通話相同 PT 的 codec 差異|宣告接收位置與封包實際到達不同；不能從 PT 數字猜 codec|
|rtp-rtcp|序號、時間戳、SSRC 及時鐘頻率；RTCP SR／RR 不同報告|wrap、中途遺漏；fraction lost、jitter、LSR/DLSR 算 RTT|漏抓與漏播不同，報告與觀測點不同，RTP 不保證送達|
|audio-path|取樣、量化、聲道與 frame；PCM、WAV、音訊裝置緩衝|8000Hz mono16bit WAV 的長度；削波與左右聲道相消|解碼正常而選錯裝置、增益錯誤、剪波或格式不合|
|codecs-packetization|codec、sample rate、RTP clock、ptime 和網路開銷|PCMU 10/20/40ms；G.729 10-byte/10ms 只作算式介紹|封包率與帶寬、掉一包聲音長度、協商／payload 不一致|
|jitter-and-quality|到達變動、RFC jitter、播放期限、緩衝延遲和時鐘漂移|固定資料的 jitter 中間值與40/60ms期限；丟失／晚到／重排|統計量不等於主觀品質，緩衝無法補回缺失資料|
|voip-troubleshooting|把狀態、payload、時間和裝置證據放到同一故障模型|三條未知原因時間線及有限變因驗收|PT 0／8 解碼錯、晚到、音訊裝置問題分別驗證|

每篇至少兩個完整例題、一個有輸入與驗收的實作、一個候選原因鑑別，以及基礎／應用／診斷三層練習；總計 73 題，解答用多段推理。練習有不同數值與時間線，讀者可先獨立作答，不需看解答猜起始條件。字數僅作警戒，真正審查重點是是否能從定義推導不同機制。

## 已實際驗證

新增的 `docs/assets/labs/voip-maintenance-lab` 是自行建立的 .NET 10 標準函式庫 Console 計算模型，沒有錄音、網路、公司 SDK 或 codec 實作。在 `/tmp` 複本用 SDK 10.0.401 編譯執行，通過內建驗收：PCM frame/blockAlign/byteRate、20ms sample 數、封包化成本、播放期限、削波和 WAV 結構。另用 Python 標準 wave 模組獨立讀三份 WAV，核對 8000Hz／mono／16bit／8000 frames、16044 bytes、正弦峰值±8000、silence 全零、clipped 有2000筆邊界值。RFC jitter 算式最後為15.439453125時鐘單位＝1.929931640625ms。

Wireshark新混合與對照PCAP的 SIP Content-Length、SDP兩通話／雙方向端點、RTP seq／timestamp／SSRC 已由獨立 parser核對；檔案沒有RTCP，課內RTCP數據是明示的獨立例題，不聲稱抓包中有報告。保留所有舊PCAP與Python程式。沒有把bin/obj、WAV結果放入網站原始碼。

## 官方來源與仍需驗證的邊界

已查核 RFC3261交易/dialog/ACK/CANCEL/註冊；RFC3264與RFC8866的offer-answer及SDP欄位；RFC3550的RTP／RTCP／jitter／RTT；RFC3551的PCMU/PCMA/G.722/G.729；RFC6716與RFC7587的Opus及48000Hz RTP時鐘；RFC8760的現代digest；Microsoft WAVEFORMATEX、BinaryWriter、WASAPI capture/render官方文件。每章sources連到相關原始資料，不以第三方教學作規格依據。

未在GUI／TShark播放、未測真實通話／音訊設備／NAT／公司SDK，亦未實作或宣稱可使用G.729編碼器。合成流量與離線數學無法驗證真實產品相容性；學員下一步是用同一證據架構在授權測試環境確認，不能將Lab結果直接套成公司系統事實。
