# Windows／Linux觀測：教材交付與驗證

這八篇先建立作業系統承載程式與資源的機制，再把唯讀工具輸出連到程式維護。正文不是工具清單；每篇從定義、運作、條件與工程後果推進，兩個算例和多段答案讓讀者能檢查陌生數據。所有資料與Lab型別是合成，不宣稱公司架構。

| 章節 | 要建立的維護能力 | 核心推導與故障 | 可重跑驗證 |
| --- | --- | --- | --- |
| find-evidence | 分開存在、可存取、就緒、完成與缺觀察 | 回環綁定、PID重用與CPU分母；錯實例log | evidence按層判讀 |
| processes-threads-handles | 程序地址空間、Thread/Task與資源所有權 | FD斜率與鎖等待環；GC不代替Dispose | ownership正常/例外清理 |
| services-and-startup | 管理就緒契約、身份、路徑與依賴 | Type/After邊界；930ms查詢與期限 | startup與平台原生路徑 |
| network-endpoints | 位址/協定匹配、最長前綴及觀測視圖 | /25優先；namespace、映射、框架不足 | endpoints固定單表模型 |
| devices-and-audio | 驅動/入口/角色、共享獨佔與PCM格式 | 960frames=20ms/3840byte；同名方向與underflow | devices純數據 |
| logs-and-time | 記錄保存管線、UTC/單調/offset範圍 | 校正差22–30ms；校時與保存負證據 | time區間與世代拒絕 |
| resource-pressure | 利用/容量/等待、記憶體層與I/O排隊 | 1核心=整機25%；queue20/120秒滿 | pressure持續速率模型 |
| systems-troubleshooting | 多層故障分支、有限修正與反例驗收 | 端點/資源/音訊/時鐘案例，非恢復即原因 | diagnose中間界線斷言 |

每篇8題、三層，解答至少3段，包含新數據、等待圖、格式、部署或時序。第一篇缺觀測分類已按因果層修正：程序false、後層null仍確定NoProcess；沒有要求取得全部欄位才允許任何結論。

已核對Microsoft程序/執行緒、Handle、Service/帳號、Get-NetTCPConnection/Get-NetUDPEndpoint、PnP及Core Audio、Event Log、Stopwatch、性能與.NET counter；Linux man-pages/iproute2的proc/FD/ss/route/namespace/clock以及systemd官方服務契約、journal和核心PSI。每章sources指具體文件，不以Wiki或模型替代正式規格。Windows命令與輸出明示為唯讀範例和人工合成，未聲稱在Linux實跑Windows工具。

systems-maintenance-lab在/tmp隔離副本使用.NET 10.0.401編譯，0 warning/0 error；all八模式完成，expected-output.txt保存實跑數據與斷言。另獨立副本故意漏例外Dispose、改整數除法、先按metric排序及offset符號錯誤，四項均非零退出FAIL，證據見systems-mutation-verification.json。正式來源未保留缺陷，無bin/obj。

兩張原創SVG分別教程序/資源/業務責任、事件保存與時間尺度；正文帶alt/caption。語文已轉繁體後人工核對單位、並行/平行、模擬/類比及上述因果小修。

限制：未測公司SDK、實體音訊/COM、Windows服務部署、真實ETW/journal权限、VPN/防火牆或硬體效能；模型使用固定邏輯時間、單表選路和緊密打包PCM。交叉審查與全站驗收由主代理接續，這份作者報告不能取代交叉審查。

作者複核補充：FD例題分開完成後淨庫存（674次到1024）與先Open兩次的瞬間峰值（第674次需要1025，可能失敗）；陌生開3關2題同樣補第799次峰值901。模型的operations_to_limit是淨差值，不承諾實際可完成操作數。
