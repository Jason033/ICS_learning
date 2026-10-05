# Wireshark 系列課程盤點

盤點日期：2026-10-04。這份文件依本機課程內容和附帶教材檔檢查 Wireshark 系列的技能重疊與必要銜接。讀者已修過一般資工課程，主要需要把網路概念接回既有程式、封包和除錯證據；章數不是配額。

## 盤點結論

新版〈第一次抓包〉和〈篩選器〉可以作為入口。其餘舊版內容各有工程用途，但不能原樣逐篇重寫：封包欄位、TCP序號、重組、重傳和故障推理在數篇中重疊。較精簡的必讀路徑是：抓到已知操作 → 用篩選器縮小證據 → 確認封包實際保存的資料 → 判讀跨封包的TCP行為 → 對照程式紀錄完成有限結論。多流量統計和語音封包分析則按工作需要選讀。

| 章節 | 建議 | 保留的獨有能力與邊界 |
| --- | --- | --- |
| 第一次抓包 v3 | 保留，去掉重複前置條件 | 選擇合理觀測介面、保存短擷取、定位一筆請求和回覆，並說明抓包看不到哪些程式事件。 |
| 篩選器 v3 | 保留為獨立技能 | 把問題改成可回退的display filter；懂方向錯誤、解析欄位不存在及「篩選為零筆」不等於「沒有流量」。Capture filter與display filter的用途和語法不同，必須分清。 |
| 封包列表、細節與原始位元組 v3 | 已完成獨立審查 | 核對實際frame長度、擷取長度、各層欄位與解析器推算值；比較完整檔與逐筆截短的合成檔，辨認資料不足的邊界。TCP序號和跨封包時序移到後續篇章；另以完整UDP frame檢查判讀方法是否能遷移。 |
| TCP重試、ACK與無回應 v3 | 已完成獨立審查 | 沿SYN/RST、ACK、相同序號範圍重送、server廣告零窗口和應用紀錄提出有限結論；序號明示使用raw欄位，不從Wireshark提示直接判根因。 |
| 統計視窗 v3 | 完成，進階選讀 | 依問題選Endpoint、Conversation、TCP Stream或I/O Graph；定義母體、方向、filter、時間桶、bytes和分母。用實際合成資料示範兩方向總數相同仍可能有時間缺口；GUI桶值尚未於原生Wireshark重跑。 |
| SIP／SDP與RTP呼叫流程 v3 | 完成，語音支線第一篇 | 從交易與Dialog識別呼叫、按offer／answer推導接收端點和payload，再以不同但各自完整標示的PCAP核對實際RTP方向。 |
| RTP序列與時間分析 v3 | 完成，語音支線第二篇 | 固定方向、SSRC和擷取窗，區分序號缺口、重複、亂序、RTP timestamp與到達間隔；逐步算RFC jitter，並說明PCAP不能直接證明解碼或播放。原生GUI統計未實測。 |
| 封包與程式紀錄整合練習 v3 | 已完成獨立審查 | 將C#示範、日誌探針、封包與回覆狀態依request ID和共同時間基準配對；說明每個觀測點能支持的有限結論、未知項目及下一步。合成案例分開標示，不共用一條假造時間線。 |

## 教材檔案與推理限制

〈第一次抓包〉和〈篩選器〉使用13個frame的合成HTTP／DNS檔，內容是一次 `GET /status` 與HTTP 200。封包欄位篇使用另一組171-frame合成混合流量及其截斷版本，供檢查擷取長度與資料是否留全。這是不同的教學案例，不能把frame編號、地址、事件或時間拼成同一個真實操作。

觀察不到一個欄位，不代表線上沒有該資訊：capture filter、snap length、選錯介面、擷取時間窗、解析器辨識和加密都會改變工具可見範圍。Sequence/ACK與checksum提示也要回到原始frame和擷取位置查證；Expert Info、重組視圖或紅色標記本身不是根因。解密、offload及抓包完整性的說法只能依本次量測條件成立，不能從教材示範延伸到未知公司部署。

盤點依據包含[Wireshark顯示篩選文件](https://www.wireshark.org/docs/wsug_html_chunked/ChWorkDisplayFilterSection.html)、[封包重組文件](https://www.wireshark.org/docs/wsug_html_chunked/ChAdvReassemblySection.html)、[SIP Flows視圖](https://www.wireshark.org/docs/wsug_html_chunked/ChTelSIPFlows.html)與[RTP分析文件](https://www.wireshark.org/docs/wsug_html_chunked/ChTelRTP.html)。文章本身仍須在重寫時依實際使用欄位補上直接來源和資料核對。

截至本次整合，Wireshark共有八篇v3教材，均通過作者檢查及獨立技術／銜接審查。五篇主線依「擷取 → 篩選 → frame／bytes → TCP跨frame診斷 → 程式紀錄關聯」前進；統計視窗和SIP／RTP語音支線則按工作需要選讀。新稿核對附帶PCAP、manifest與RFC／Wireshark官方文件，但本環境沒有Wireshark或TShark，因此沒有宣稱已實測原生GUI及dissector。這是教材結構與資料驗收，不代表讀者已學會；使用者理解尚未做試讀測量。每篇狀態以網站目錄和[驗收紀錄](reader-review.md)為準。
