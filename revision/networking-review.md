# 電腦網路第二版：改了什麼，以及驗證到哪裡

原版大多是約千字的名詞與證據提醒，不能支撐完整工程概念。本版重写10篇，保留ID，正文先建立問題、定義、機制、概念關係及工程後果，實作只是補助。主語言為C#/.NET，從維護既有程式的端點、資料、狀態與紀錄切入；所有案例為原創假資料。

## 能力與驗收對照

| 章節 | 概念與維護能力 | 兩個例題／主要產物 | 故障機制與驗證 | 核對來源 |
|---|---|---|---|---|
| 一次通訊 | 封裝、表示、傳輸與業務契約；追connect/write/read/parse | 分階段時間；LF及3+5讀取；責任鏈圖 | 缺LF→ACK但parser等；补終止符及分段回歸 | RFC9293、Microsoft TcpClient/ReadAsync |
| IP/Port | 位址、介面、五元組、PID、監聽/連線、家族與生命週期 | 兩條同目的連線；三種binding；端點關係圖 | 回環與外部地址不匹配→限定binding驗證 | Microsoft TcpListener/Get-NetTCPConnection、RFC9293 |
| Ethernet/VLAN | 下一站MAC、ARP、來源學習、broadcast domain、逐跳封裝、二層迴圈 | 跨路由MAC/IP/TTL；VLAN錯配；長度與地址表 | ARP缺reply→VLAN候選與port證據→ARP/TCP/結果復原 | RFC826/1812/4541、Microsoft arp |
| 路由 | 位元AND、前綴集合、LPM、metric、on-link與回程 | /26全部邊界；四條重疊路由；/23延伸 | 特定過期路由勝預設→下一站不可達→限定修正 | RFC1812/8200、Microsoft route |
| TCP/UDP | bytes→SEQ→累積ACK→缺口→窗口→狀態；資料報交付契約 | SYN/DATA/FIN編號；flight/rwnd/cwnd；流重組 | zero-window/read停→與路徑重傳、業務慢分開 | RFC9293/5681/6298/768 |
| DNS/DHCP/NAT | 解析/配置/轉換擁有不同狀態；快取與更新時機 | TTL與長連線；lease門檻和NAT雙向五元組 | 新DNS但舊Socket/cache→受控更新與新端點驗證 | RFC1035/2308/2131/3022、Microsoft Resolve-DnsName |
| 群播 | 分發與可靠性不同；join/IGMP/snooping/querier/路由；來源與介面 | 低23bit映射；50/48/0分段計數 | 成員老化→query/report及跨原故障時間驗證 | RFC1112/3376/4541、Microsoft JoinMulticastGroup |
| MTU/QoS | 每層長度、MSS、分片、DF/反馈；競爭服務與容量 | 1500/1400payload；offset/MF；串行與隊列時間 | PMTU黑洞→途經too-big/反馈→大小與雙向回歸 | RFC791/1191/8200/2474/3246、Microsoft ping |
| 效能 | 容量/throughput/goodput；延遲成分/分布；損失/jitter/BDP/採樣 | 五RTT分布；20M/50ms窗口上限；指標表 | 逐筆等業務ACK→window外的應用限制 | RFC6349/3393/7679/7680/9293 |
| 整合 | 建正常預測、反證候選、最小變因、分階段修正與回歸 | 正常全路徑；跨機校時；兩輪故障報告 | 讀邊界與PMTU案例留給陌生題作答；含UI後段案例 | RFC1812/9293/1191、Microsoft Test-NetConnection |

每章至少兩個完整例題、可操作的讀碼任務與第二輪診斷；前九章各8題，末章10題，共82題。解答有三段以上推理，包含未在正文原數據中出現的新前綴、序號、窗口、長度與時序。來源於本次重編經web開啟核對；RFC使用具體章節標籤，未整段照搬。概念與算例為原創講解。

## 實際驗證與限制

- `networking-maintenance-lab`在獨立`/tmp/ics-networking-verify`以.NET SDK10.0.401編譯：0 error、0 warning；未在公開docs產生bin/obj。
- 17組模式已實跑並比對關鍵結果：成功/缺LF、端點、兩種VLAN、三個路由目的、TCP、TTL/NAT、兩種群播介面、兩種MTU、統計、兩種整合狀態。預期輸出存在`docs/assets/labs/networking-maintenance-lab/expected-output.txt`。
- C#程式只算固定資料與印出模型狀態，不建立真實Socket；文章部分Socket片段是閱讀示意，不是單獨可編譯的完整產品。未聲稱實測真實DNS、DHCP、NAT、VLAN、QoS或群播。
- 10篇JSON均解析通過；例題/實作/診斷種類、練習分層、三段答案與3+來源已檢查。圖片`networking-layer-boundaries.svg`為原創概念圖。繁體用`OpenCC s2twp`統一，正式審稿仍需看詞句自然度。
- 正文時間是閱讀、實作、作答分開的未校準範圍，不能稱閱讀需要50分鐘。主代理應依最終篇幅與使用者回饋重算。
- 尚需主代理及另一位作者人工互審：前綴/窗口邊界、群播API家族、統計口徑、陌生題是否足夠遷移；技術格式檢查不能代替學習效果。未實際測試Windows命令輸出或公司環境，發布與ZIP由主代理統一處理。

重要檔案：lesson JSON供網站顯示；Program.cs是可逐行讀的原創模型；csproj指定獨立.NET10；README說明模式與不涉及的環境；expected-output保留實跑結果。未修改舊lab、catalog、全站README、app或測試。
