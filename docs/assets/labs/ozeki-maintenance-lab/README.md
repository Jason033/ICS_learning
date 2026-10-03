# Ozeki 程式閱讀與生命週期練習模型

這個 .NET 10 Console 專案用自製 Lab 型別，練習 SDK 包裝層常見的物件、事件、媒體方向與清理問題。它沒有 Ozeki DLL、SIP、RTP、麥克風、喇叭或公司系統參照，因此不會撥號，也不能證明真實 SDK 相容性。

執行 `dotnet run --project ozeki-maintenance-lab.csproj -- all`。可把 all 換為 graph、registration、calls、media、lifecycle、version、evidence。測試會刻意重現錯誤，再驗證修正；最後退出碼 0 並印出 PASS all assertions。任何判定失敗會退出 1。

基準是單條 line、單通 call、兩條方向正確且附著同一通的媒體路徑。變因包含先操作後訂閱、舊事件清掉新通話、方向接反、未附著接收端、重複訂閱及重複結束。比較的是狀態、訂閱數、路徑可用性、清理次數，不是聲音品質或網路速度。

在 Program.cs 的 LabScenarios 各方法設中斷點，逐步畫出輸入→物件→事件→狀態→斷言。修改一個守衛或一條連線，記下預期失敗的檢查，再執行證明。復原後重跑 all，確認第二、第三通與遲到結束事件不會污染新通。

重要檔案：Program.cs 是全部原創模型與回歸檢查；csproj 僅設定 .NET 10 與零外部套件。網站七篇 Ozeki 教材各指定一個 scenario 並提供追碼問題。官方 SDK 用法在教材中另外列出來源，未在此專案編譯。
