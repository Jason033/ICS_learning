# 教學練習檔

本資料夾放三種用途不同的檔案：Wireshark 合成封包、RS-232 軟體回環，以及 Socket 本機 TCP／UDP 練習。全部不含公司資料，也沒有真實設備的設定。

## Wireshark 合成封包

這三份 PCAP 是為個人學習網站**合成**的練習資料，不是從公司、設備或任何人的網路擷取。位址使用文件保留範圍，SIP 名稱使用保留網域；固定時間從 2026-01-01 00:00:00 UTC 起算。它們適合練工具操作與判讀證據，不能當成真實網路效能或語音品質的量測。

- `wireshark-first-capture.pcap`：13 筆。DNS 查詢／回覆 2 筆；TCP 8080 握手、GET `/status`、HTTP 200 回覆與關閉共 11 筆。
- `wireshark-tcp-cases.pcap`：8 筆。TCP 7000 只有三次 SYN 嘗試；TCP 7001 有握手、`STATUS?` 與 TCP ACK，沒有應用回覆。檔案內沒有程式逾時紀錄。
- `wireshark-sip-rtp.pcap`：16 筆。SIP 控制訊號 7 筆、RTP 9 筆；B→A 方向刻意沒有 RTP 序號 302。這只能表示**此檔沒有收錄**該序號，不能直接證明網路在哪裡遺失。RTP 負載是固定靜音教學位元組，沒有真實人聲。

來源程式是專案根目錄的 `tools/generate_wireshark_labs.py`。在專案根目錄執行 `python3 tools/generate_wireshark_labs.py` 可重建全部三份檔案；再執行 `python3 tests/verify_labs.py` 檢查 PCAP 格式、時間順序、IP／TCP／UDP checksum 與課程預期的封包數、Port、SIP／RTP 欄位。教材 JSON 的 `resources` 欄位連到這裡的檔案。若要更改案例，請同步修正產生器、驗證程式與相關教材的預期答案。

練習檔可在 Wireshark 用「檔案 → 開啟」直接讀取，不需要即時抓包權限。若 Wireshark 的解析設定沒有自動辨認 SIP 後的 RTP，先核對 SDP 與 UDP Port；只對已知的教學流使用「Decode As」。

## RS-232 主題的軟體回環

`serial-loopback.py` 使用 pySerial 的 `loop://`，把虛構的四個位元組 `AA 01 10 11` 回送給同一個程式，分兩次讀回，最後示範逾時回空。它不會開啟 COM Port，也不連任何實體設備；成功只表示這個本機程式的寫入、讀取與逾時運作，不能驗證轉接器、RS-232 電平、接線或設備回覆。

先安裝 Python 和 pySerial，再在下載檔所在資料夾執行 `python3 serial-loopback.py`；Windows 可使用 `py serial-loopback.py`。預期前兩次讀取合併為原本的四個位元組，第三次讀取顯示 `(empty)`。若環境不允許安裝套件，直接閱讀[串列工具與軟體回環章節](../../content/lessons/serial--tools-and-loopback.json)的輸出解說即可。

## Socket 主題的本機 TCP／UDP 練習

`socket-tcp-lab.py` 與 `socket-udp-lab.py` 只使用 Python 標準函式庫，伺服器綁在 `127.0.0.1`，Port 由作業系統自動分配，不連外部網路。兩個腳本都交換虛構的 `STATUS?` 與 `OK,READY`；前者在兩端使用 2-byte 大端序長度前綴、限制內容最多 1024 bytes，並示範分兩次呼叫 `sendall` 後仍應依長度讀取；後者示範 `sendto`／`recvfrom` 和回覆來源檢查。它們不模擬封包遺失或真實設備狀態。

在下載檔所在資料夾執行 `python3 socket-tcp-lab.py` 和 `python3 socket-udp-lab.py`；Windows 可用 `py socket-tcp-lab.py` 和 `py socket-udp-lab.py`。預期都印出請求、回覆和「本機範例通過」。若本機政策禁止啟動回環服務，可直接閱讀對應教材及練習；腳本成功不代表公司系統的 Socket、SDK 或設備也相同。
