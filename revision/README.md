# 教材重編與擴充紀錄

> **最新工作已改為重新設計學習路線。** 下文是兩輪舊版製作與驗證紀錄；發布及程式測試成功不表示教材已能帶新手循序學習。新版目標、課程設計與實際進度見[新版重編紀錄](learning-redesign/README.md)。

這裡保留兩輪工作的證據。第一輪是原有十主題83篇全文重編，以下歷史數據與報告保持原樣；第二輪是剩餘六主題的40篇完整系列，範圍、內容審查和驗收另存[剩餘主題紀錄](remaining-topics/README.md)與[本輪驗收說明](remaining-topics/validation.md)。不要把第一輪83篇的量測檔當成現在全站總數。

## 第一輪：十主題83篇重編

使用者指出第一版像科普摘要，內容和標示時間不相稱，無法建立讀公司程式及維護系統需要的完整觀念。本次針對十個已發布主題、83 篇全文重寫，不擴充尚未製作的主題。

使用者已確認：C#／.NET 為主，Python 為輔助；以程式閱讀、維護和除錯為學習重點。目標是從基礎走到能理解機制、追出呼叫／事件與狀態、比較候選故障原因、驗證有限修正的能力。真實公司系統仍需授權原始碼、版本與設備證據才能映射。

基準審查（以 audit_content.py 統一口徑重算）：83 篇正文共 107,581 非空白字元，中位數 1,145；舊目錄卻總列 4,220 分鐘。數據檔保留舊狀態，可供日後比較。正文指 intro、section 文字、表格及程式，排除標題、練習、來源。科普摘要和單句答案不能支持原先「完整」的標示。

使用者再次明確指出：核心缺口是基礎正文不足，不能把問題縮成實作缺少。重編以完整概念講解和機制推導為優先；程式與練習只是檢驗理解。

重編流程：先明確能力與前置概念；按主題分派作者；同一章包含完整推導、C#／資料追讀、可重現操作及未見過的故障練習；由另一位代理審查並依反饋修正；最後驗證網站和實作產物。閱讀與實作、作答時間分開並標示預估，實際學習時間待使用者回饋。

驗收包含兩種不同證據：程式和網站測試驗證可執行／可存取；人工審稿驗證教材能否靠自身資料完成推理。字數、段數、題數只作缺漏警戒，不能證明中級能力。

- [authoring-standard.md](authoring-standard.md)：共同撰寫規格及資料欄位。
- [baseline-content-metrics.json](baseline-content-metrics.json)：改寫前的完整量測證據。
- 各主題的 `*-review.md`：能力、實作產物、故障和來源核對紀錄。
- [validation.md](validation.md)：本輪結果、審查修正與驗證範圍。
- [final-content-metrics.json](final-content-metrics.json)：第二版全篇量測，沿用基準口徑。
- [csharp-verification.json](csharp-verification.json)：九個專案的編譯、情境輸出與原始碼雜湊。
- [browser-verification.json](browser-verification.json)：真實瀏覽器檢查。
- [download-verification.json](download-verification.json)：九份ZIP與原始碼一致性。

## 完成結果

十個主題83篇全部重寫，正文合計402,456非空白字元，中位數4,707；練習675題。與第一版使用同一量測口徑，顯示原本摘要已改成可逐步推導的正文；數量本身不證明讀者已具備中級能力。閱讀、操作、作答改成分項預估，讀者時間尚待校準。

三組交叉審查巡查全83篇的目標、段落與題目，全文複審30篇代表章，另補讀特定段落。審查不是全83篇每個段落逐句二次複審；已發現的算式、單位、前提與詞形問題均由另一作者讀回確認修正。

審稿紀錄：[基礎／串列／無線電／可靠性](peer-review-foundations.md)、[封包／VoIP／Socket](peer-review-network-group.md)、[Ozeki／整合／PTT](peer-review-api-group.md)。

公開站188檔逐檔比對一致，83篇公開瀏覽檢查通過，見[發布驗證](publication-verification.json)與[公開瀏覽結果](public-browser-verification.json)。
