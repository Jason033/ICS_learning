# 衛星與多鏈路：作者審查

這七篇從路徑、時間、功率及容量建立觀念，再教健康選擇與切換。目的在於讀公司維護碼時知道數值與成功事件代表什麼，不把未知廠商拓撲、API或衛星方案當已知。保留原 `latency`、`failover` URL，新增五篇。

| 章節 | 正文建立的機制與維護能力 | 完整算例／追碼與驗證 |
| --- | --- | --- |
| latency | 傳播、序列化、排隊、處理；GEO兩段單程與RTT；LEO只說幾何類別；期限與副作用未知。 | 每向273ms、業務回覆576ms；短幾何仍可因容量／排隊變816ms。追 `Delay`、`latency`。時間圖依算式比例繪製。 |
| satellite-path-and-budget | terminal/gateway上下行；dB/dBm/dBi；同參考功率、門檻、雨衰與兩段噪聲。 | FSPL206.074825dB、餘量6.925175dB；新增8dB負餘量。兩段線性C/N10合成5。追 `budget`。 |
| link-quality-and-capacity | Hz/baud/bit/s、FEC比例、MODCOD、ACM、封裝、份額、配額；品質與容量分離。 | 240k→160kpayload bit/s、180k供給過剩；4M物理與配額下1M實際上限。追 `capacity`。 |
| transport-and-queues | rwnd/cwnd/BDP、長反饋、ACK返程、服務差、期限、PEP確認邊界與MTU。 | BDP150000byte與視窗426666.667bit/s；12500byte積量→625ms，再6.25秒排空。追 `queues`。 |
| health-and-selection | 分層合格、unknown與down、來源路由、證據過期、連續miss重置、共同故障及排序。 | 1100/1700/1750ms選A/空/B；F/S/F/F/F第五份達3。明示80ms只是短時間線例，不適用高RTT。追 `health`。 |
| failover | candidate/active、路由與端點、NAT/會話、準備提交、epoch、回切hold及副作用分類。 | 序列恢復1280ms；舊epoch與僅路由就緒不能提交。追 `failover`。 |
| satellite-troubleshooting | 以工作、方向、時窗、身份與故障域串證據，逐候選判斷。 | 同幾何RTT42→1242由queue解釋；1220+80=1300超MTU1280，最大內IP包1200。追 `diagnose`。 |

每篇有兩個完整輸入與推導例題、追碼、變因／預期失敗／復原實作、至少一個多候選診斷，以及8題基礎／應用／診斷、多段答案。共56題，陌生資料含時間線、單位、容量、候選、世代與錯誤邊界。篇幅只是警戒，表格說明實際機制，並不以篇幅保證職稱或學習成效。

官方來源已經browse核對：RFC2488只取衛星物理與長反饋的歷史架構，不將1999服務數字或視窗當現況；TCP以RFC9293、視窗RFC7323、壅塞RFC5681、RTO RFC6298；UDP RFC8085、佇列RFC7567、PEP RFC3135；ITU P.525及P.618界定自由空間與實際傳播方法；BFD RFC5880只說其範圍，自製計數不是協定實作；SIP RFC3261、NAT RFC4787、Stopwatch官方檔案對齊相應邊界。各篇 sources 指向具體原頁。

已實際在 `/tmp` 隔離副本用 .NET 10.0.401 編譯、執行七模式 `all`，25項機制檢查與總完成透過；來源目錄沒有bin/obj。預期輸出來自實跑。另在隔離副本拿掉 `Commit` 世代守衛，`failover`應退出1並出現 `FAIL old epoch cannot commit new switch`，證據見 [satellite-model-verification.json](satellite-model-verification.json)。原來源守衛維持正確。

未驗證：真實衛星終端、RF、BFD、OS路由、NAT/SIP遷移、供應商計數與公司SDK；真雨場和全年可用率不由此模型預測。模型採固定單執行緒邏輯、連續平均佇列；流程圖是功能示意。跨作者審查與網站整體驗證由主代理另記，不將作者自查當獨立審稿。
