# Socket 維護教學模型

這是搭配 Socket 八章的獨立 .NET 10 Console 專案，幫助讀者把 bytes、框架、端點、收送返回值及生命週期連起來。它不是公司程式或 Ozeki API，只使用 .NET 標準函式庫。所有 TCP/UDP 僅綁 `127.0.0.1` 和系統分配的暫時 Port，不呼叫裝置或外部服務。

安裝 .NET 10 SDK，解壓下載包，在專案目錄執行：

```text
dotnet run --project sockets-maintenance-lab.csproj
```

舊公司的 .NET Framework 不一定支援本專案的 Memory／CancellationToken 多載。這裡用獨立新環境驗證機制，閱讀舊碼仍需核對其目標版本與簽章。`NuGet.Config` 清空外部套件來源；專案無套件相依。

## 協定與驗證範圍

自行設計的框架：兩 bytes 大端序無號本文長度，合法範圍 1–1024 bytes，本文為嚴格 UTF-8。先檢查長度，再配置或取出本文；它與公司的線上協定無關。

執行依序檢查：

- 確定性切片 `[1,2,3,5]`，將 A 和溫度兩框共 11 bytes 交給 `FrameParser`；預期輸出 A、溫度，最後保留 0 bytes。
- 中文兩字是六個 UTF-8 bytes，加標頭共八 bytes；長度 1025、標頭中 EOF、本文中 EOF、無效 UTF-8 必須分別拒絕。
- TCP listener 與 accepted socket 是不同物件。client 送請求後只關送出方向，server 讀完請求與 EOF 仍可回覆；client 讀到回覆再讀到框架間 EOF。
- UDP 空資料報仍有來源端點，下一則三 bytes 資料報可獨立接收；已取消 token 的等待產生取消例外。

`expected-output.txt` 是恢復工作後實跑的輸出。暫時 Port、READ 分段及並行輸出次序可以不同，不能整份逐字作合格判斷；對照 PASS、資料內容、累計數和端點關係。

## 用變因找出錯誤

先保留基準，修改自己的副本，每次只改一件事，最後復原。把切片換 `[11]` 或 11 個 1，輸出列表應一致；在完成第一框後用 `pending.Clear()` 取代 `RemoveRange`，單片 11 會丟第二框；把標頭長度改用 `text.Length`，中文會失去正確邊界，ASCII可能掩蓋錯誤。這些是教材中推導過的機制，不靠網路排程偶發才重現。

實際恢復驗證已在 `/tmp` 副本編譯與執行，未在來源目錄留下 bin/obj。單片與逐 byte 的合法變因均透過；錯誤 Clear 與字元長度變因均按預期失敗。完整版基準最後重新透過。

## 限制與維護

`List<byte>` 與複製本文便於教學，未做高吞吐最佳化。沒有模擬外部丟包、MTU、完整重連狀態機、公司命令去重、UI框架或裝置；全 PASS 不能寫成公司系統驗證。十秒 token 是避免練習永久等待的保護，並非公司的服務期限。未在真實公司環境測過。

`Program.cs` 提供框架編碼、確定性解析、ReadExact、SendAll、TCP 半關閉與 UDP 結果範例；`sockets-maintenance-lab.csproj` 指定 net10.0；`NuGet.Config` 控制來源；`expected-output.txt` 儲存完整本機證據。擴充範例時維持單一讀取所有者、框架完整性與清楚錯誤分類，並同步更新對應章節的規則和預期。
