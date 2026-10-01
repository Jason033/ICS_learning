# Wireshark 教學封包

這三份 PCAP 是為個人學習網站**合成**的練習資料，不是從公司、設備或任何人的網路擷取。位址使用文件保留範圍，SIP 名稱使用保留網域；固定時間從 2026-01-01 00:00:00 UTC 起算。它們適合練工具操作與判讀證據，不能當成真實網路效能或語音品質的量測。

- `wireshark-first-capture.pcap`：13 筆。DNS 查詢／回覆 2 筆；TCP 8080 握手、GET `/status`、HTTP 200 回覆與關閉共 11 筆。
- `wireshark-tcp-cases.pcap`：8 筆。TCP 7000 只有三次 SYN 嘗試；TCP 7001 有握手、`STATUS?` 與 TCP ACK，沒有應用回覆。檔案內沒有程式逾時紀錄。
- `wireshark-sip-rtp.pcap`：16 筆。SIP 控制訊號 7 筆、RTP 9 筆；B→A 方向刻意沒有 RTP 序號 302。這只能表示**此檔沒有收錄**該序號，不能直接證明網路在哪裡遺失。RTP 負載是固定靜音教學位元組，沒有真實人聲。

來源程式是專案根目錄的 `tools/generate_wireshark_labs.py`。在專案根目錄執行 `python3 tools/generate_wireshark_labs.py` 可重建全部三份檔案；再執行 `python3 tests/verify_labs.py` 檢查 PCAP 格式、時間順序、IP／TCP／UDP checksum 與課程預期的封包數、Port、SIP／RTP 欄位。教材 JSON 的 `resources` 欄位連到這裡的檔案。若要更改案例，請同步修正產生器、驗證程式與相關教材的預期答案。

練習檔可在 Wireshark 用「檔案 → 開啟」直接讀取，不需要即時抓包權限。若 Wireshark 的解析設定沒有自動辨認 SIP 後的 RTP，先核對 SDP 與 UDP Port；只對已知的教學流使用「Decode As」。
