# 概念地圖：C# 功能追讀模型

這個專案以一項「給目標設定 level」操作，練習控制結果、媒體數值、事件時序與資源責任。所有 `Lab` 型別都是教材自建，不是公司 RCSCall、iCallAPI 或 Ozeki API。它不連網、不接設備、不呼叫音訊裝置；Console取代畫面。

使用自己的 .NET 10 獨立環境，解壓ZIP後執行：

```text
dotnet run -- all
dotnet run -- paths
dotnet run -- contracts
dotnet run -- trace
dotnet run -- ownership
```

未知模式拋例外。`expected-output.txt` 是實際執行結果，包含各欄位和順序。

| 模式 | 完整輸入/條件 | 要解釋的結果 |
|---|---|---|
| paths | controller session1、B、level40；媒體先附著0再1，Ready與樣本各自變動 | 控制Completed不保證媒體可用；[600,-600] RMS600，全零RMS0；guard拒絕不更新LastRms |
| contracts | UI75%，合法整數0–100；非法150、錯target、未知Result、重連後舊session及仍存在的歷史完整Pending key | Send true只本地接受；所有錯誤條件都不應套用當前，合法完成才更新 |
| trace | CompleteDuringSend=true、level30；同身份重複Emit | Callback/Apply在Send返回前，Pending先建立；回呼多次不等本地應用多次 |
| ownership | B/C共用同一adapter，各有target而可同名R1；wrong另一實例；B關閉兩次 | B只退訂、adapter仍活、C繼續；全部借用者結束後owner釋放 |

## 關鍵契約

`LabKey(Request,Session,Target)` 完整比較。每個controller從Session1開始，Reconnect只增加本地世代，沒有網路動作。Reconnect保留歷史States供查閱，但舊世代即使仍Pending也不得再套用。Pending在Send前建立，事件允許同步發出；結果集合只允許Completed/Rejected，只有同身份Pending能套用一次。

`LabAdapter.Requests` 是本地API嘗試，不證明任何封包外送。Send固定接受是本文刻意限定的契約，不聲稱測過廠商的接受/拒絕機制。測試者 `Emit` 注入結果，不是外部收包。

controller借用adapter，Dispose只設closed並退訂自己的handler，重複Dispose無額外動作；最外層Console擁有adapter。SubscriberCount由實際委派清單計算，讓「設closed但未退訂」仍能被測試抓到。

LabMedia只驗證附著session/Ready/樣本非空；兩個short代表數值計算，沒有真實採樣率、codec、RTP或20ms幀。RMS為平方平均開根號，在接受本次資料後更新；guard拒絕會保留舊值，不能把它當本次輸出證據。Output true不保證作業系統播放、喇叭發聲或真人聽見。

## 有目的的故障注入

先預測、保存原始source與預期，再只在暫存副本一次改一項：

- 移除媒體AttachedSession或Ready：各自的拒絕保護斷言應失敗。
- 把百分比轉成0–1比例：UI75應不再對應level75，單位斷言應失敗。
- 移除controller目前session檢查：舊R2/session1的完整key不存在，可能仍被lookup擋下；歷史R1/session1/B的完整key仍Pending，則必須由session guard拒絕。`historical_pending_session_guard`應失敗，並見Applied從0變1。
- 移除未知Result拒絕：`InventedState`不應進入合法終態，回報語義斷言應失敗。
- 把Pending建立移到Send後：同步Callback會找不到工作，時序斷言應失敗。
- 移除Pending終態guard：第二份合法Completed會再應用，重複斷言應失敗。
- 移除退訂：即使closed阻止Apply，Subscribers仍不符，生命週期斷言應失敗。
- 借用者直接Dispose共享adapter：另一使用者將失效，owner斷言應失敗。

每次復原後跑all並比較實際數字與事件順序，不只看總結。兩個身份guard與字典完整key可能互相補充，不能因刪一guard仍被另一路擋住，就宣稱測試已單獨驗過該guard；測試要說明自己檢查的是哪項契約。

## 實際驗證與限制

作者以 .NET SDK 10.0.401 在暫存副本實際build/run，專案原始碼資料夾無bin/obj；四模式、19項斷言與最後總結通過。故障注入結果保存在 `revision/big-picture-author-verification.json`，與章節作者審查一起解釋。

未驗證實體設備、廠商SDK、Windows畫面派送或多執行緒回呼/關閉競爭。舊公司.NET Framework可能沒有同樣API；可遷移的是責任、契約、身份、時序與追碼方法，不是把Lab直接部署到公司。
