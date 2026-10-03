# 系統維護與通訊安全：本次剩餘系列交叉審閱

這份紀錄評估本次新增的「系統維護」8章與「通訊安全」7章，並非先前83篇的歷史審查。目的不是以字數或PASS數判定教材深度，而是檢查外行讀者能否從正文建立判斷條件、讀到相符的C#機制，再用給定資料解釋陌生故障。

目前結論：六篇指定章已全文審閱，15篇全部目標、段落架構、120題與所有解答已檢視；13個陌生輸入先自行推算再比對，均一致。發現的程式名稱、模式責任與文字問題已交作者修正並讀回。兩個原始碼專案在隔離副本實際執行`dotnet run -- all`，均退出0，與正式expected-output完全相同。此結論是教材與有限模型的驗證，不代表未知公司系統、Windows服務、裝置或正式安全通訊已驗收。

## 審閱方法與範圍

先看每章想建立的觀念、前置條件、段落與練習，再選主題的入口、核心與綜合故障章逐段全文閱讀，包括兩個例題、C#片段、故障分支、實作限制及所有解答。

|主題|全文讀稿的章節|全系列額外巡查|
|---|---|---|
|系統維護|find-evidence、processes-threads-handles、systems-troubleshooting|全部8章目標/段落架構、64題及所有解答|
|通訊安全|auth、tls-and-certificates、security-troubleshooting|全部7章目標/段落架構、56題及所有解答|

題目檢查重點是資料能否支持答案，而不是只檢查有沒有答案欄位。例如CPU題須給取樣期間與核心數，校時題須給偏移方向/範圍，FD題須區分操作結束的淨量與操作途中峰值，輪替題須給含不含截止點的規則。每章皆有8題、三層難度及多段解答；每章4至6個來源。沒有把非抽讀章宣稱為全文逐段審閱。

閱讀內容同時核對專案真實符號、資料流與guard順序。security的Program.cs已全文檢查，包括離線憑證建立、名稱/EKU、可信KeyId字典、HMAC與固定時間比較、重放窗口、審計欄位及單程序業務去重。systems檢查FirstMismatch、資源清理、查詢時間與守恆/單位計算；這些是Lab模型，不把合成布林值當作實際OS探針。

## 為什麼目前內容可以支持維護學習

系統維護入口先分程式檔案、正在執行的處理程序、OS/驅動與業務狀態，並教PID重用、未知觀察不等於false、Running不等於Ready。核心章從位址空間、執行緒等待、資源句柄及所有權解釋GC/Dispose，沒有把Task等同一條執行緒，也沒有把RSS或CPU一個數字當作故障本身。終章逐步區分LAN連不上迴環監聽、本地資源拒絕、音訊供應不足及日誌佇列壓力，讓修復後的新錯誤可被定位，而不是全套重試。

安全入口分開傳輸、安全通道、可信主體、資源/動作授權與業務完成。TLS章由保密/完整性/來源與公私鑰開始，才解釋信任鏈、SAN、用途、期限與私鑰。其限制清楚：鏈Build成功不足以證明名字正確；HasPrivateKey關聯不足以保證服務帳號可用；離線NoCheck不證明未撤銷。末章把名字、權限、媒體上下文及輪替分開，並教同訊息防重放與同業務意圖冪等的不同範圍。

所有練習的解答都檢查了能否由已給條件推出；讀者不需猜公司的SDK、私有拓撲或真正金鑰。答案也保留有限修正和反例，不把「取消驗證」或「全改管理員」當作修復。

## 陌生輸入的獨立推算

下表記錄先計算後讀答案的結果。它不是抄expected-output；題目的數字與固定模型不同，用來檢查觀念是否能遷移。

|系統維護題目|給定條件與推導|比對結果|
|---|---|---|
|devices-and-audio Q3|32000Hz、24bit緊密排列、雙聲道、640frames：每frame=3×2=6bytes；共3840bytes；640/32000秒=20ms|一致|
|resource-pressure Q3|CPU累計12→21秒、經過6秒、3核心：(21−12)/6=1.5核心；單核心基準150%，全機50%|一致|
|resource-pressure Q4|佇列120、容量720、產95/s消80/s：淨增15/s，(720−120)/15=40秒；容量1320則80秒，仍持續超載|一致|
|logs-and-time Q3|A記錄500、offset[2,6]→真時[494,498]；B記錄520、offset[−3,1]→[519,523]；B−A=[21,29]|一致|
|processes-threads-handles Q3|初值100，每操作先開3再關2，40次/分，上限900：淨增1，800次約20分；但第798次峰值900且結束898，第799次第三個open需901，會提前失敗|一致；解答區分淨量與峰值|
|services-and-startup Q3|查詢時刻50+80n，Ready410：第一個可看見的是450；絕對deadline650前成立，不能說410就是觀察時刻|一致|
|systems-troubleshooting Q7|遠端720，offset[35,45]→本地[675,685]；本地接收700，完成到接收=[15,25]ms|一致|

|通訊安全題目|給定條件與推導|比對結果|
|---|---|---|
|input-and-replay Q4|寬8、最高50、已見50/48：下界43；49接受、48重複拒絕、42過舊拒絕、51接受並改下界44|一致；MAC/會話先成立且不迴繞|
|secure-media-and-tunnels Q4|MTU1400、外96、內44、payload1300：外長1440、超40；最大payload=1400−96−44=1260|一致；只屬題目固定開銷|
|keys-and-secrets Q3|K2[100,150)、K3[140,210)：重疊[140,150)長10；150時K2拒絕、K3仍適用|一致|
|security-troubleshooting Q3|K7[0,200)、K8[180,280)、180切K8：重疊20；t190版本適用但接收節點缺K8，檔案存在不能替代實例載入|一致|
|auth Q3|已驗證token aud=A、expires900、now800、服務B：時間成立、audience不符，仍拒絕|一致|
|tls-and-certificates Q3|根/日期/serverAuth成立、SAN ctrl、參考voice：鏈可true但name=false，不能接受預期身份|一致|

## 發現、修正與讀回

|發現|對閱讀/推理的影響|修正及狀態|
|---|---|---|
|systems終章正文稱FindFirstMismatch，snippet與真實來源為FirstMismatch|讀者依教材查符號會找不到|作者統一FirstMismatch；已讀回JSON與Program.cs，結案|
|security終章稱LabDecision/FirstFailure，而實際只有SecurityDecision三個nullable參數|會把不存在的完整TLS資料結構誤認為真實模型|改為實際三個觀察，明示鏈/日期/用途由tls模式獨立驗證；已讀回，結案|
|security終章lab要求在diagnose預測UnknownKey，但當時diagnose未查可信字典|練習指示與可執行資料不相符|作者拆清責任：diagnose列自己的分支，可信KeyId查找在rotation；來源TryGetValue→時間→MAC及四個相應斷言已讀回；all實跑包含UnknownKey，結案|
|systems的「噹噹前」「這隻在」，security的「會社系統」「RTP聲聲音」|語句打斷基礎閱讀|改為目前主要原因、這只在、公司系統、RTP聲音；已讀回，結案|

## 可重建模型的實際驗證

原始碼由`docs/assets/labs/{systems,security}-maintenance-lab`複製至隔離`/tmp`目錄，排除bin/obj。使用既有.NET SDK 10.0.401執行all；沒有在原始碼目錄留下編譯產物，沒有變更OS信任存放區、連線公司端點或操作設備。

|專案|實際結果|能支持的結論|
|---|---|---|
|systems-maintenance-lab|exit0、空stderr、60個斷言及PASS end；stdout與expected-output逐字相同|固定模型的證據、所有權、啟動、端點、音訊、校時、壓力和綜合案例一致|
|security-maintenance-lab|exit0、空stderr、75個斷言及PASS end；stdout與expected-output逐字相同|實際記憶體憑證鏈、名稱/EKU、HMAC、可信KeyId與時限、重放、審計及固定政策成立|

完整stdout、原始碼/專案/README/expected-output雜湊保存在[peer-systems-security-execution.json](peer-systems-security-execution.json)。本次交叉審閱沒有重跑作者全部破壞測試；作者紀錄與整站驗收由主代理另行檢查，不把未重跑項列作本次實證。

## 官方資料交叉核對

本次另開官方來源檢查容易混淆的責任：Microsoft文件明示MatchesHostname不建立信任或檢查serverAuth；RFC9525把參考身份與憑證名稱驗證和路徑驗證分開；RFC3711的重放清單更新在認證之後；RFC9110的403可能與憑證無關，不能僅憑狀態碼宣稱認證完成。教材保留這些分界。

- [Microsoft MatchesHostname](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.matcheshostname?view=net-10.0)
- [RFC9525服務身份](https://www.rfc-editor.org/rfc/rfc9525.html)
- [RFC3711 §3.3.2 重放清單](https://www.rfc-editor.org/rfc/rfc3711.html#section-3.3.2)
- [RFC9110 §15.5.4 403](https://www.rfc-editor.org/rfc/rfc9110.html#section-15.5.4)

## 限制與下一步

本次未在使用者的Windows實測服務、私鑰ACL、音訊驅動或Linux檔案權限；模型沒有正式TLS握手/撤銷、SRTP密碼棧、VPN封裝、公司SDK或實體發話。HashSet業務去重只限本程序，不能外推跨節點持久一次。其餘非抽讀章已巡查題目與所有解答，但不宣稱每段正文全面人工驗收。

交叉審閱發現已修正後，主代理仍須做來源ZIP打包、整站教材連結/圖文呈現與最終目錄整合。教材目前能給讀者完整條件、機制與追讀方法；公司實際流程仍需在有來源與版本證據時套用，不用虛構Lab推定真實架構。
