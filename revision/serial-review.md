# RS-232與設備介面第二版

原版偏向名詞分類與短提醒；本版先補最基础的bit/byte、表示、時間、電氣與訊息機制，才談工具/除錯。七篇全部重寫，ID保留，原有圖及Python回環檔未改。C#只是幫助閱讀現有程式，正文不以實作步驟替代原理。

| 章節 | 核心概念與能力 | 完整推導/產物 | 故障機制與恢復 | 官方來源核對 |
|---|---|---|---|---|
| 分層 | bit→byte→encoding→UART→收發器→設備消息；COM與交易身份不同 | 9/10byte8N1時間；Text10 vs HEX10；契約卡 | 缺CRLF等消息→只補終點→分段/合併回歸 | ADI UART/RS232、pySerial |
| 電氣 | 單端參考/閾值/噪聲裕度、logic與线路極性、差分/common-mode、方向與角色、供電不等線電平 | 三電壓判斷、參考偏移；差分抵消/方向矩陣；RC邊緣例 | TTL外側不符RS232→正式相容介面→字元/回覆驗證 | ADI RS232/UART/AN960 |
| UART | start與中心採樣、LSB、8N1/7E1、parity/framing/overrun、相對時鐘差、流控反馈與在途量 | 9600/19200時間；誤差/積壓；7/8data效率 | service不足→有界讀/處理分界→負載及flow恢復 | ADI UART、pySerial、Microsoft Handshake/DataReceived |
| frame/期限 | 邊界設計、LEN總長、checksum/CRC責任、重同步、byte order、交易關聯、三種等待 | request/reply XOR；噪聲/壞LEN/CHK逐byte；deadline表 | parser半筆校驗→累積消費→任意分段/壞輸入回歸 | Microsoft Read、pySerial、ADI UART |
| 工具/回環 | 測試範圍、echo不是result、多層buffer、n與count、event/owner/instance/UI、採樣時間 | 舊loop4bytes；3+0+2event；覆蓋圖 | 雙讀者分走→單owner→關閉/重開/晚事件回歸 | pySerial、Microsoft Read/DataReceived |
| 其他介面 | 差分與尋址不同、driver三態/DE、半雙工完成時刻、反射/終端/偏置、SPI/I²Cclock/電氣、橋邊界 | 19200交易23.667ms；addr03重疊；接口矩陣 | DE截尾/地址争用→相應完成/轮询→長短/速率/node回歸 | ADI AN960/UART/SPI、NXP UM10204 |
| 整合 | 各層正常預測、候選的不同觀測、有限修正與回歸範圍 | 正常查詢時間全鏈；新binary校驗；三案例第二輪報告 | COM身份、缺終點/flow、遲到關聯由陌生題分開 | 上述官方來源 |

前六篇各8題，整合10題，共58題；每題至少三段推理，包含新數據算式、診斷與驗證。每章兩個worked-example、一個lab、diagnostic-case，3+具體官方來源。語言已轉繁體並修正機械詞彙；仍需跨作者自然度與技術互審。

## 實際驗證

新`docs/assets/labs/serial-maintenance-lab`只使用.NET10標準函式庫、不引用SerialPort套件、不開COM。於`/tmp/ics-serial-verify`以SDK10.0.401編譯，0 warning/0 error；11組模式實跑，覆蓋layers正常/fault、電氣靜態計算、UART、parser正常/fault、期限、event、半雙工時間、整合正常/fault。輸出保存`expected-output.txt`。

舊`serial-loopback.py`已實跑且未改：4-byte TX，兩次read2，timeout空，內容斷言通過。這是pySerial軟體回環，不是硬體測試。C#實際SerialPort片段是已有引用專案的讀碼示意，未在新獨立模型編譯；新.NET需package的部署差異已說明。Microsoft常規net10頁工具無法開，核對官方Framework Read及net10-pp/plat-ext DataReceived/Handshake頁，未將未知版本當公司版本。

未測真實接線、pinout、電壓、UART硬體允差、USBdriver延遲、bus波形/終端/DE或設備；理想RC/采樣/差分例只解釋因果，不作規格認證。閱讀/實作/作答時間分開估計，尚未使用者計時；主代理統一重算，不能沿用舊45–55分鐘閱讀宣稱。

重要檔案：lesson JSON是教材；Program.cs提供可追的原創模型；csproj指定NET10；README說明模式/覆蓋；expected-output保留執行結果；三張原有SVG作概念圖，不含可施工pinout。ZIP/全站目錄/測試/發布由主代理統一處理。未變更其他主題或全站程式。
