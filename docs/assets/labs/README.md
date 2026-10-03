# 教學資料與C#追讀專案

這些資料用來檢查教材中的推導：先手算或預測，再執行與比較，修改一個條件，最後復原並驗證。基礎觀念在章節正文解釋，不需先操作設備才能閱讀。

所有PCAP、log、ICD及設備情境都是自行建立的合成資料；沒有公司封包、帳號或原始碼。C#專案是Lab模型，不是廠商SDK替代品。完整專案ZIP包含原始碼、csproj與README，沒有編譯後執行檔。

## 第二版資料

| 資源 | 解決的學習問題 | 不能用來證明 |
| --- | --- | --- |
| networking-maintenance-lab | 逐步計算端點、位址、路由、TCP、MTU、群播及效能 | 真實網路已連通或公司路由配置正確 |
| wireshark-maintenance-mixed／receiver／truncated.pcap | 比較原始bytes、觀測位置、TCP重組、SIP與RTP；三份檔案有不同留存條件 | 實際線路一定丟包，或收到RTP就一定聽得到 |
| wireshark-maintenance-logs.csv、manifest.json | 把請求ID與時間對齊；分析後再核對原始欄位 | 真實公司log的探針位置與時鐘語義 |
| ozeki-maintenance-lab | 理解class／interface、事件、通話歸屬、媒體方向及清理 | 已在Ozeki DLL編譯或真實電話接通 |
| serial-maintenance-lab | 由UART時序、HEX與框架讀到解析、校驗、期限及關聯 | 接線電平、線材、實際COM驅動正常 |
| radio-maintenance-lab | 讀設備參數與功率、品質、鏈路的教學演算 | 天線／RF量測或真實發射效能 |
| ptt-maintenance-lab | 追按鍵、狀態輸入、忙碌、音訊時序與釋放責任 | 廠商實體PTT訊號、燈號或發射狀態 |
| sockets-maintenance-lab | 追Socket生命週期、收送、框架與取消；以各模式README所述為準 | 教學端點等於公司協定或部署拓撲 |
| voip-maintenance-lab | 取樣、PCM資料量、封包化、播放期限與音訊追讀 | 實測聲卡、麥克風、SDK或真人語音品質 |
| integration-maintenance-lab、integration-demo-icd.md | 比對ICD、呼叫、命令、會話、錯誤與事件關聯 | 已確認RCSCall／iCallAPI的真實依賴 |
| reliability-maintenance-lab | 算整體期限、重試、佇列、事件順序與共享狀態 | 生產級去重、備援或WPF實際反應時間 |

## 使用方式與驗收

C#需.NET 10 SDK。下載對應ZIP到自己的練習資料夾，解壓後讀README，再執行本章指定的`dotnet run -- <模式>`。音訊專案的參數是輸出資料夾，其他專案的模式也不同；不要只跑第一個模式就當作學完所有章節。程式的檢查通過，代表這個教學模型的不變條件成立，不代表真實設備已通過。

基準、要改的變因、預期結果與修正後的重測都寫在各章。第一次裝工具另計時間。公司舊.NET Framework與SDK的版本、執行緒和資源規則需另核對；模型原始碼與公司程式分開。

Wireshark使用PCAP開檔，不需連公司網路。維護版內容包含多連線與干擾流量；先定位自己的請求，再用manifest核對，不先從答案反推篩選器。截短檔可讀到標頭，並不表示應用內容也保存完整。

## 資料一致性檢查

在儲存庫根目錄執行：

```bash
python3 tests/verify_labs.py
python3 docs/assets/labs/wireshark-maintenance-verify.py
```

第一個驗證舊三份PCAP，第二個驗證第二版三份PCAP的留存長度、時間、可用checksum、TCP序號與框架、SIP Content-Length、RTP逐來源序號及雙觀測點差異。驗證不等同於Wireshark GUI實際操作或語音播放。

`wireshark-maintenance-generator.py`是第二版產生器；`tools/generate_wireshark_labs.py`產生舊三份檔案。重產前先核對教材依賴的frame、端點與情境，改資料後需一併審查題目和解答。

## 第一版保留的輔助資料

舊三份PCAP及Python腳本保留原網址：wireshark-first-capture、wireshark-tcp-cases、wireshark-sip-rtp；serial-loopback、socket-tcp-lab、socket-udp-lab、voip-tone-lab、integration-trace-lab、reliability-timeline-lab、reliability-race-lab。它們可用於局部觀察，已不代表整個主題的完整教材。

輸出產物應放自己的練習或暫存資料夾，避免混進網站assets。C#的bin／obj是編譯中間檔；公開包由`tools/package_labs.py`以原始碼重新產生。
