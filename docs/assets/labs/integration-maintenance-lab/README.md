# C# 整合層維護練習模型

本專案用原創 LabController、LabAdapterPort、LabScriptedAdapter 建立操作→方法返回→非同步概念通知→狀態的模型。沒有真實SDK或網路，也沒有設備或音訊操作；Emit由測試同步注入，不能當作真實回呼執行緒保證。

使用 .NET10，執行 `dotnet run --project integration-maintenance-lab.csproj -- all`。可把all換成trace、layers、icd、commands、sessions、boundaries、evidence、recovery、troubleshooting。Program.cs包含完整模型與回歸斷言，csproj零外部套件。

基準：Connected、Authenticated、DeviceReady均成立；一個root擁有adapter，controller借用；請求以session+request關聯。Accepted只表示模型接收，Completed要等注入通知。變因涵蓋越界欄位、大小寫錯誤、亂序／重複／舊session、逾時、例外、控制成功但音訊條件不足及controller關閉。比較事件應用次數、結果、發送數和訂閱，不測實際延遲或音質。

在Submit、Send、OnComplete設中斷點畫呼叫堆疊與物件圖。修改一個條件，先寫預測失敗，再執行；復原後all退出0並印PASS。例：移除session檢查應使舊事件保護失效，刪訂閱解除應使關閉檢查失敗。每章提供獨立追碼任務；結果需解釋機制，不只有控制台輸出。

模型假設：一般回歸單一串行處理；結果Completed重複忽略；同session晚到完成可把Unknown解決；重連重置驗證與Ready。這些是Lab契約，不對應任何RCSCall或iCallAPI。真實整合需用目前介面文件、程式和觀測核对并行、去重、取消與owner行為。
