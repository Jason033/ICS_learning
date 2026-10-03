# Wireshark 第二版重編與審稿紀錄

這次解決的問題是原八篇教材僅有短段落與記憶題，無法讓新手理解抓包怎麼觀測、欄位怎麼形成，以及統計如何支持工程判斷。第二版全部重寫，先教定義、機制和關係，再用可計算的例子檢查理解。工具操作與合成PCAP是概念的驗證材料，不是用資產數量代替教材深度。保留原章節ID，未改舊PCAP或其他主題。

## 基礎觀念與可審查產物

正文數包含導言與段落、表格、程式等可見內容，排除標題、練習、來源與資產說明；非空白字元是防止摘要的警戒值，不是教學品質的結論。

| 章節 | 正文字元 | 核心機制與維護能力 | 完整例題／實作產物 | 故障鑑別 |
|---|---:|---|---|---|
| first-capture | 4,624 | Socket到介面的資料副本、觀測點、snaplen與有限擷取緩衝 | q17從雜訊定位；67→64bytes截斷；十筆正常時間線 | 選錯介面、篩選排除、時間窗或未發送的區分 |
| packet-panes | 5,075 | 多層標頭、offset、各層長度、大端、byte序號、工具產生欄位、重組與offload | 14+20+20+13分層；雙向SYN/data/FIN原始seq計算 | Malformed來自錯解碼、截斷或格式錯誤 |
| filters | 4,968 | Capture/Display的處理階段、集合、方向、多值欄位、存在與Boolean值 | 完整雙向條件；SIP集合與媒體集合分離；五個正反控制查詢 | 零筆、contains跨段與純ACK被len條件隱藏 |
| streams-statistics | 4,648 | Endpoint/Conversation/Stream/Request、唯一bytes、桶寬、統計分母 | q19可見26與唯一13；q20 ACK覆蓋未捕獲15；同數媒體掩蓋缺口 | 最大流量與根因的假關聯、觀測遺漏 |
| tcp-troubleshooting | 4,602 | 有序流、累積ACK、重傳／業務重試、窗口／擁塞、半關閉與RST | q18 ACK4014；q19 seq5001與ACK5014；零窗口300ms | 拒絕、SYN-only、ACK後排隊、擷取缺口與接收壓力 |
| sip-rtp | 4,868 | 信令／媒體分工、訊息、交易／Dialog、CSeq／tags、SDP接收端點／格式／方向 | 5060上的兩通交錯呼叫；offer/answer推雙向目的端點 | 相同無聲症狀區分媒體未發、錯端點、解碼與播放 |
| rtp-analysis | 4,700 | 取樣／編碼／封包化、seq/timestamp/SSRC、亂序／晚到、RFC jitter與緩衝取捨 | 總20與唯一19；307/309/308/310；D和J中間值；兩種播放期限 | 網路遺失、漏捕、解碼供不應求與late drop |
| evidence-and-logs | 5,013 | 事件／探針、關聯鍵、時鐘基準／偏移／誤差、競爭假說、有限修正 | q17三種延遲；q18排隊／逾時／晚完成；跨觀測報告 | UI阻塞、任務生命週期、捕捉遺漏與結果未知 |

八篇共80個section、65題分層練習，每題以三段說明證據、規則、推論與驗證。每章有至少兩個worked-example、一個lab、一個diagnostic-case，至少四題採用正文沒有給過的新端點、序號、時間或負載。最後一章題目使用新的q60/q70等資料，並未直接把本章已示範的q17/q18答案再問一次。

## 合成資料設計與驗證標準

基準是正常q17連線：三次握手、13-byte長度前綴請求、15-byte回覆和正常關閉。變因保持端點模型與格式規則，分別引入RST、SYN-only、ACK後無業務回覆、相同序號重傳、觀測遺漏、零窗口；加入60筆DNS與15筆telemetry干擾，訓練先找集合再判讀。SIP為兩通Call-ID不同而UDP端點相同的呼叫；RTP含兩方向與另一通獨立來源、缺口、重複和亂序。

- `wireshark-maintenance-mixed.pcap`：171筆；q20回覆與入向RTP305故意未入這份合成觀察檔。
- `wireshark-maintenance-receiver.pcap`：172筆；q19第一份未入觀察，q20回覆及RTP305可見。這是預先設定的對照模型，並非實際兩臺主機量測。
- `wireshark-maintenance-truncated.pcap`：同mixed的171筆，長frame只保留64bytes，原始長度仍在record中。
- `wireshark-maintenance-manifest.json`：保留雜湊、每筆原始欄位與合成設計，完成自行分析後才核對。新的練習數字不來自manifest，避免答案直接洩露。
- `wireshark-maintenance-logs.csv`：同PCAP起點的q17/q18程式探針；SEND指呼叫返回後記錄。
- `wireshark-maintenance-generator.py`：純Python標準函式庫、無網路操作，在空資料夾重建上述資料。
- `wireshark-maintenance-verify.py`：獨立解析record、IPv4、TCP、UDP、SIP與RTP；不匯入generator，因此可交叉查出生成資料與教材算式的不一致。
- 三張SVG：觀測管線、標頭byte邊界、媒體到達／播放時鐘關係。均為自製示意，不是工具實測截圖。

已執行獨立驗證，三檔record數、時間排序與manifest、截斷長度、IPv4及完整TCP/UDP校驗和通過。TCP的請求長度前綴、payload長度、seq與ACK中間值通過；SIP Content-Length與本文、Call-ID通過；RTP版本、160-byte負載、SSRC、seq與timestamp通過。RTP入向mixed總20／唯一19，reference總21／唯一20，309先於308亦通過。此驗證不把label或manifest情境名稱當故障真相。

## 來源核對與測試限制

已透過web核對Wireshark官方User Guide具體頁：Capture、Packet Details／Bytes、Display Filter Expressions、Follow、Reassembly、Checksums、TCP Analysis、Conversations、I/O Graphs、VoIP Calls、RTP；TShark/capinfos/editcap的官方man page；SIP/TCP/RTP的dfref。關鍵欄位`sip.CSeq.seq`、`sip.CSeq.method`、`sip.Call-ID`、`tcp.flags.reset`、`tcp.seq_raw`、`rtp.seq`等採官方拼寫。

TCP序號／SYN／FIN依RFC9293，SIP關聯依RFC3261，offer/answer依RFC3264，SDP依RFC8866，RTP時鐘／jitter依RFC3550。例題與數字爲原創最小模型，不逐字摘錄標準。Sources列在各篇末端，GUI選項變動需依實際安裝版本核對。

製作環境沒有可用的Wireshark或TShark，未在GUI驗證選單、實際dissector自動關聯、Expert Info標籤、RTP Analysis數值／播放，未跑本文TShark命令。因此不宣稱完成GUI工具測試或真實音質測試。基礎計算與PCAP結構已獨立驗證；下一步是在已安裝的讀者環境依lab驗收，若GUI結果不同，先核對版本與解碼設定。

所有資料為虛構保留地址與靜音bytes，沒有公司流量、設備、SDK或真人聲音。未驗證公司實際拓撲與協議。閱讀／實作／作答時間分列為未計時估計，主代理將按實際正文統一重算閱讀範圍，不能以舊45／50分鐘當閱讀證據。
