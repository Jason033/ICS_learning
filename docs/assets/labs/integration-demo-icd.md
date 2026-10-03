# Lab Integrator v2.0 介面閱讀卡（原創虛構）

此卡只定義 `integration-maintenance-lab` 的程式內介面，沒有網路格式、實際設備或廠商命令。

- 輸入：LabCommand(RequestId, Session, Action, Target, Value)。controller分配包含自身範圍的RequestId，Session表示控制世代。Target供關聯，本模型不檢查真實設備存在。
- Action大小寫敏感：SetLevel、QueryState。SetLevel.Value為整數百分比，0至100含端點；QueryState不使用Value。輸入型別和語義皆需檢查；模型adapter實際驗證Action與SetLevel範圍，未模擬JSON或封包。
- Send同步返回LabReceipt。Accepted只表示模型已接收並記入Sent，不代表Completed。UnsupportedAction/OutOfRange表示未接受，不加入Sent。
- Completed事件攜LabCompletion(RequestId, Session, Result)。回歸使用Completed；控制層亦將Failed視終態，其他文字不構成真實廠商狀態。測試呼叫Emit同步注入，沒有異步延遲與執行緒保證。
- 結果按request+session配對；目前session的Pending/Unknown可以被有效結果更新；Completed/Failed後重複終态忽略；舊session、未知request、Rejected、本地例外不可被完成通知覆寫。
- Timeout只將Pending改Unknown，不重送；Reconnect提升Session、清pending並撤銷Authenticated與DeviceReady；Connected不表示驗證及Ready已恢復。
- root擁有adapter。controller借用，Dispose只关闭自身與解除自身事件，不釋放共用adapter。

驗證：正常接受/完成、-1/0/100/101、錯Action大小寫、亂序/重複/舊session、逾時晚到、重連未Ready及借用者關閉。缺少的實際ICD項目包括transport、encoding、framing、錯誤碼完整集合、認證、時間界限與並發政策，故不可把此卡作公司ICD。
