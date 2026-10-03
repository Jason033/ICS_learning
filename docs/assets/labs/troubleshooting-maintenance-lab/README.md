# 故障分析 C# 讀碼模型

這份專案讓你練習「同樣無聲，如何用不同觀測區分原因」，並理解觀測器、負載、身份和時鐘為何會改變除錯結論。它是自行定義的 Lab 模型，沒有公司程式、廠商 SDK、SIP/RTP 通訊、音訊裝置或實體發射。

使用自己的 .NET 10 Console 環境，將 ZIP 解壓到獨立資料夾：

```text
dotnet run -- all
dotnet run -- audio
dotnet run -- observer
dotnet run -- experiment
dotnet run -- timeline
dotnet run -- cross-layer
dotnet run -- regression
```

`all` 依序執行六個模式；未知模式會拋例外。`expected-output.txt` 是實際執行輸出，可逐欄比較，不只看 PASS。

| 模式 | 模型要回答的問題 | 真正的輸入與規則 |
|---|---|---|
| audio | 沒有非零輸出是哪個邊界失敗？ | 三幀160樣本PCM，小端序16-bit；B、session8、40ms播放buffer；錯路由、舊session、零樣本、晚到、靜音各自造成不同計數 |
| observer | 沒有接收紀錄能否證明沒有接收？ | `observe=false` 只略過記錄計數，接收處理照常 |
| experiment | 負載與同步log是否互相放大？ | 宣告成本20+15×(L−1)+(V?10×L:0)ms，L=1/2；50ms期限 |
| timeline | 負延遲是否來自錯誤相減？ | B−A偏移−50±5ms，原始Send100/Receive80；完整request/session/target身份 |
| cross-layer | 請求後立刻送音訊與等待Ready有何差異？ | 20ms幀時刻、Ready80、Release110；鏈路世代避免舊結果升級當前 |
| regression | 修好正常情境會不會放進不合法資料？ | 正常三幀非零，舊session/靜音/超期仍不得進入輸出，目標身份分隔 |

## 怎麼解讀輸出

Produced、Sent、Received、Accepted 是累積階段，不能全部加起來當幀總數。Late 是 Accepted 中晚到的子集合；Played 是模型進入輸出分支的次數。Energetic 表示這些輸出幀的數值 RMS 大於零，不表示真人聽見。

LastRms 是最後一幀「通過 session 且未超過期限」的接收解碼樣本 RMS，在靜音 guard 前計算。不是從喇叭輸出量到的值。RMS=平方平均再開根號；+1000/−1000 等幅值的 RMS=1000。沒有符合該探針條件的幀時保留初始0，不能用此0宣稱已解碼全零。

所有毫秒是共享的教學邏輯軸。音訊 deadline 的 T0=S0=0、clock8000；到達20/40/60與期限40/60/80比较。late 模式加30ms得到50/70/90，三幀均晚10ms。clock不代表這個模型已做實際校時。

## 預測、故障注入與復原

先依章節預測輸出、保存 source 和基準，再在暫存副本一次修改一項：

1. 移除 `frame.Session != currentSession` 的 guard：舊session應被錯誤接受，`audio_old-session` 或 `receive_is_not_accept` 應失敗。
2. 將 `10 * load` 改為 `10`：高負載詳細log由55變45，`factorial_2_True` 應失敗。
3. 將時鐘校正 `remoteReceive - offset` 改為 `remoteReceive + offset`：`offset_not_negative_transport` 應失敗。
4. 將完整身份比較只改成 request 比較：`join_full_identity` 應失敗。

修改後的失敗是測試能辨認錯誤的證據。每次復原後 `all` 應通過，並重新比較具體數值，不把中間修改混到正式專案。

## 已驗證與限制

作者將原始碼複製到暫存目錄，以 .NET SDK 10.0.401 實際 build/run；原始碼資料夾不包含 bin/obj。六種模式的正常與故障資料已實際執行，另有刻意破壞 guard、成本、時鐘及身份的測試。

模型以單執行緒執行，不測真實排程、網路、NTP、裝置、真人語音、並行計數或 SDK 相容性。真實維護仍要取得該版本的契約、觀測設定和設備證據。舊公司 .NET Framework 不一定提供同樣 API，這個專案不是正式系統修改方案。
