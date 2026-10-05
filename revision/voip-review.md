# VoIP 系列 v3：從 SIP 控制到媒體故障定位

本輪重寫保留九個 lesson ID，因此原章節網址與已讀紀錄不變。課程以一次呼叫的資料與狀態如何交接為主線，讓讀者能區分「邀請被接受」「媒體協商成立」「封包到達」「資料解碼」「交給本機媒體層」各自需要的證據。案例中的帳號、位址、時間、SSRC 和封包都是合成資料；沒有假定公司使用特定 SDK、拓撲或媒體設備。

## 學習路徑

| 順序 | Lesson ID | 本篇接手的問題 | 交給後續的問題 |
| --- | --- | --- | --- |
| 1 | `call-vs-audio` | 將使用者撥號、SIP 控制、SDP 協商、RTP 媒體和本機媒體交接放進一條責任鏈。 | 用相同呼叫觀念分別追註冊、SIP 狀態和媒體細節。 |
| 2 | `sip-roles-registration` | 分開邏輯身分、Contact、Registrar 綁定、Proxy 路由與註冊期限。 | 呼叫時要再確認 INVITE transaction 與 dialog 如何變化。 |
| 3 | `sip-call-flow` | 追 transaction、dialog、INVITE/ACK、CANCEL、BYE、重傳、分叉與 callback 世代。 | 成功建立控制後，還需知道媒體雙方實際協商什麼。 |
| 4 | `sdp-offer-answer` | 由有效 offer/answer 推導每個媒體方向的接收端點、格式與方向。 | 協商預期要和實際 RTP/RTCP 封包核對。 |
| 5 | `rtp-rtcp` | 讀 sequence、timestamp、PT、SSRC 和接收報告，並界定報告能力。 | RTP payload 尚需依 codec 格式和 packetization 解讀。 |
| 6 | `audio-path` | 定位本機媒體處理層與通話網路媒體的軟體交接，算最少必要 PCM 數量。 | 對 payload 表示、封包率、頻寬與等待時間做具體計算。 |
| 7 | `codecs-packetization` | 把 codec bitrate、ptime、每包 payload、pps 與 IP 封裝成本相連。 | 接收端仍需在時間期限內整理和解碼封包。 |
| 8 | `jitter-and-quality` | 推導 RTP interarrival jitter，區分遺失、重排、晚到、buffer 延遲和時鐘漂移。 | 使用上述分層證據處理端到端案例。 |
| 9 | `voip-troubleshooting` | 將呼叫、協商、雙向媒體、解碼、播放期限與本機媒體邊界連成診斷路徑。 | 將流程映射到公司版本文件、程式碼及授權觀察；教材不宣稱已替公司系統定案。 |

章節可單篇跳讀。每篇前置說明指出最少需補的上一段，案例也重新提供足夠的合成輸入；導航順序則支持由總覽一路走到整合診斷。

## 範圍與責任邊界

本系列聚焦一般 SIP/SDP/RTP 音訊通話的協定與軟體資料路徑，包含一條完整成功流程、註冊和信令狀態、媒體協商、封包欄位、codec/封包化、播放期限與故障證據。所有計算都標出假設和單位，主要目的是使讀者能重新推導，不把教材數字當成公司 QoS 門檻。

- Wireshark 的安裝、擷取、filter、GUI 與 dissector 欄位操作留在 Wireshark 系列；這裡只說明需要哪些欄位以及它們能支持什麼判斷。
- 本機麥克風/喇叭選擇、Ozeki MediaHandler/MediaConnector 與 SDK 物件生命週期留在 Ozeki 系列。VoIP 的 `audio-path` 只把本機媒體子系統當作交接邊界，不假裝教了實體設備操作。
- PTT 的授權、發話權與控制狀態留在 PTT 系列，不混入一般 SIP 呼叫接受。
- 不推測公司所用 SDK、SIP server、NAT/relay/SBC/B2BUA、codec 支援、加密方式或實際部署。例子採簡化單播 RTP；需要公司結論時仍須取得版本匹配文件、程式碼與授權測試證據。

## 來源依據與適用範圍

教材的規範性依據直接連到 IETF RFC Editor 的標準文件：RFC 3261 用於 SIP 註冊、消息、交易、dialog 與呼叫控制；RFC 6026 補充 INVITE transaction 狀態機；RFC 3264 用於 offer/answer；SDP 語法以現行 RFC 8866 為主，不沿用已被取代的 RFC 4566 作現行格式依據；RTP/RTCP 欄位與統計以 RFC 3550 為主，RTP/AVP 靜態媒體映射使用 RFC 3551，Opus over RTP 的 PT/clock/frame 說明使用 RFC 7587，RTP/RTCP mux 邊界連到 RFC 5761。Digest challenge 的延伸只在註冊篇點到 RFC 8760。

這些 RFC 支持協定欄位和一般行為，不代表每家 SDK 都將工作交給相同 API，也不保證特定產品啟用某項擴充。內容採標準中與本文範圍相關的部分；ICE、SRTP、telephone-event、視訊、多播、完整 SIP extension 互通、廠商特殊參數和實際網路設備配置沒有在此系列完整教授。若例子簡化了標頭或省略其他媒體流，正文均應以「合成」「簡化」提示，而不是當完整生產封包。

## 自查與限制

九篇均已改成 `contentVersion: 3`，刪除舊 `studyTime`；lesson ID、JSON 檔名及 catalog URL 沒有更動。每篇都保留既有章節內容結構，使用網站支援的段落、流程、表格與程式片段；練習採基礎、應用或診斷情境，解答提供可從正文核對的依據。本文撰寫時檢查了 RFC Editor 對 RFC 3261、3264、3550、3551、4566/8866、5761、7587 的頁面，並依新版 authoring standard 和 curriculum plan 劃定主題界線。

自查重點是同一流程能由 SIP 對話接到有效 SDP，再分方向核對 RTP/RTCP，最後停在本機媒體交接；數字例題標示編碼率、clock rate、窗口、單位與未計入的網路開銷。這是作者核對，不等於獨立技術審稿、真實通話互通測試或學習成效驗收。

仍未驗證的部分：未在 Wireshark/TShark 重放這輪教學案例、未使用公司 SDK 或設備實際建呼叫、未驗證 NAT/SBC/relay 路徑、未做真實語音品質量測，也未對未知部署提出根因結論。本系列沒有新增 Lab/PCAP 資源。標準版本或廠商行為若會影響具體維修決策，仍需讀者以適用產品版本重新查證；後續由獨立審稿和網站 smoke/結構驗證確認格式及讀者銜接。
