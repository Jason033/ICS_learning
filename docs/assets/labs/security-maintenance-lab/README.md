# 資安責任模型：讀程式時辨認哪一層拒絕

本專案幫助理解教材的安全邊界：確認身分、允許動作、憑證名稱與用途、保護區段、金鑰版本、訊息完整性、防重放與審計。它是獨立的 .NET 10 Console 教學程式，使用自行定義的 Lab 型別，不是 Ozeki SDK 或公司系統的替代實作。所有輸入為合成資料；沒有 Socket、公司設備、OS 帳號/ACL/信任庫變更或韌體刷寫。

## 執行與比較方式

需要 .NET 10 SDK，在此資料夾執行 `dotnet run -- all`。每行 DATA 顯示實際值與期望，PASS 表示具體斷言成立；任一失敗輸出 FAIL 並以非零代碼結束。`expected-output.txt` 保存 SDK 10.0.401 的實際執行輸出，同一最終來源已跑兩次確認輸出逐行一致。隨機金鑰只在記憶體使用，輸出不列金鑰、序號或憑證指紋。

| 模式 | 基準與變因 | 判讀重點 |
| --- | --- | --- |
| `auth` | Reader 可讀 channel-A；未認證、寫入及 channel-B 分別拒絕 | 可信認證結果、動作與資源須同時成立，模型 bool 不是 Token 驗證器 |
| `tls` | 合成可信根、SAN、serverAuth；錯名、clientAuth、過期、不可信根拒絕 | X509Chain、名稱、用途是不同責任，固定 2030-06-01 驗證日 |
| `boundaries` | 三段部分保護、全保護、空路徑；MTU 1500/封裝80/內頭40 | All 不能改 Any；1380 是此輸入的 payload 預算，不是通用 VPN 常數 |
| `rotation` | K0=[0,100)、K1=[80,200)；可信字典查 K1、未知 ID、時間邊界 | 公開 ID 只選本地受管理金鑰；真 HMAC/SHA256 及 RSA-PSS/SHA256 驗簽，舊合法版本仍拒絕 |
| `replay` | 長度頭、嚴格UTF-8、100/102/101/重複/70；壞MAC的10000之後合法103 | 驗 MAC 前不更新窗口；序列號、會話、時限與完整性分別檢查 |
| `audit` | 合成讀權限、缺父路徑search；帶假Password/Token請求 | 請求最小權限；只序列化白名單審計資料，ID換行拒絕 |
| `diagnose` | 名稱/授權/缺觀察、方向版本、K5/K6截止、相同operation不同seq | 安全新訊息不代表新業務意圖，記憶體去重不跨重啟 |

可依各章指定方式只改一個條件再跑該模式，預期負例 FAIL；還原後跑 all。作者另外在 `/tmp` 複製來源，實際編譯七種錯誤：放行錯名稱、跳過EKU、Any替All、截止含等號、刪除防降版、壞MAC推進窗口、整個請求寫審計，七者皆被斷言攔下。實際證據保存在網站專案的 `revision/security-mutation-verification.json`，不是以硬編碼 FAIL 表示測試。

## 已驗證結果與範圍

最終來源在獨立暫存副本編譯：0 warning、0 error，七模式 all 全部通過。模型確實呼叫標準庫 HMAC、固定時間比較、RSA 簽章、CertificateRequest、X509Chain 及 MatchesHostname；其餘政策與權限卡是明確合成規格，不冒充 OS 或協定引擎。

憑證模型使用每個 X509Chain 的 CustomRootTrust，`DisableCertificateDownloads=true`，固定驗證日且沒有 AIA/CRL 網路地址；`RevocationMode.NoCheck` 只為離線合成實驗，**未驗撤銷，不是正式 TLS 策略**。本模型不做真正 TLS 握手、不確認公司私鑰可讀性；名稱匹配與明確 serverAuth 檢查也不是完整的平臺 TLS 回呼。

LabEnvelope 用兩端同一程式的固定 JSON 序列化規則得到 MAC 輸入；這不是任意 JSON 的跨平臺標準化方案。它沒有生產 JWT/密碼資料庫、SRTP、VPN、OS ACL、正式祕密庫、多節點防重放、持久去重或完整更新流程。RSA 簽章範例證明內容與版本綁定，不代表 Secure Boot/設備更新已執行或安全。

下一步是把每個模型責任對照實際程式的類別、驗證設定、事件與輸出來源，再依公司授權環境補真正的 SDK、OS 與設備驗證。不能把此專案的 Authenticated bool 或 HashSet 直接當正式認證與一次執行保證。

## 檔案用途

- `Program.cs`：七模式、Lab 型別及成功/拒絕斷言，閱讀時由 modes 查到對應函式。
- `SecurityMaintenanceLab.csproj`：.NET 10 專案，啟用 nullable 與 warnings-as-errors。
- `NuGet.Config`：清空外部套件來源；此專案僅使用 SDK 標準庫。
- `expected-output.txt`：最終 all 的實際、可重現輸出。
- `README.md`：實驗原因、變因、結論與限制。
