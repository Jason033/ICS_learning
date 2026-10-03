# 網路維護讀碼實驗（原創 .NET 10 模型）

此專案讓讀者追蹤位址、路由、序號、NAT 與觀測數值。它使用固定資料；不傳網路封包、不改網卡、路由或防火牆，也不模擬完整 TCP/IP stack。計算結果不能代表公司環境。

安裝獨立 .NET 10 SDK，在本資料夾執行 `dotnet run -- routes`。沒有 SDK 時，逐行讀 Program.cs、手算後對照章節即可。不要把專案加進公司程式參照。

| 參數 | 要比較的內容 |
|---|---|
| `flow` / `flow --fault` | LF 終止符與 parser 等待 |
| `endpoints` | 監聽、兩條連線與 IPv6 格式 |
| `ethernet` / `ethernet --fault` | access VLAN、ARP 與逐跳 MAC |
| `routes 10.24.6.77` / `routes 10.24.6.130` | 最長前綴與 /26 邊界 |
| `tcp` | SYN、ACK、窗口、分段讀取 |
| `services` | TTL 到期及來源 NAT 五元組 |
| `multicast` / `multicast --fault` | 群播 MAC 映射及加入介面 |
| `mtu 1500` / `mtu 1400` | payload 上限及 IPv4 分片 |
| `metrics` | 平均、中位數、goodput、BDP |
| `diagnose` / `diagnose --fault` | 有 ACK 卻無應用回覆 |

每個參數先預測輸出再執行；改回原值後重跑確認復原。遇到 PASS 只是本模型的指定不變條件通過，不能解讀成實際網路已驗證。進階任務在各章教材：提出替代原因、補資料、寫有限結論。
