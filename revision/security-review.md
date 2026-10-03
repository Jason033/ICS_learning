# 資安完整系列：作者驗證與維護範圍

本輪完成七篇，把資安當成讀取和維護程式的責任鏈：先解釋身分、授權、通道、金鑰、訊息、OS 權限與審計，再用輸入推到實際拒絕。不是堆疊實作取代基礎，也不把憑證存在或VPNConnected當作全部安全。每章有必要前置概念、兩個完整推導例、C#判讀、離線模型、同症狀分支故障和八題三層解答；數學題列中間值，修正題保留合法/非法回歸。

| 章節 | 學會讀取的責任 | 完整例題與故障分支 | 模型與核對範圍 |
| --- | --- | --- | --- |
| 驗證、授權與執行身分 | principal/resource/action；OS與API身分；角色與可信上下文 | Reader不能寫；本地私鑰讀取、HTTP401/403不同入口 | auth實算集合與audience/time，不實現JWT；RFC6750、9110及ASP.NET Core授權 |
| TLS與憑證 | TLS1.3握手、信任根/鏈、SAN、EKU、日期、私鑰及撤銷各有責任 | 可信鏈的錯名稱仍拒絕；錯用途/日期/私鑰需獨立證據 | tls真建X509鏈及SAN，固定2030日、離線NoCheck；RFC8446/5280/9525及.NET10政策API |
| 安全媒體與隧道 | SIP TLS、SRTP/SRTCP、DTLS-SRTP/SDES、VPN的起終與方向 | 中間隧道不是完整路徑；MTU1500給定封裝算1380，280byte小語音不能只怪MTU | boundaries實算All/空路徑/context及長度；RFC3711/5764/4301/4303/4568 |
| 金鑰、密碼與祕密生命週期 | 密碼成本化儲存、公開salt/私密pepper、祕密格式與取用、輪替/撤銷 | K0/K1半開窗口及缺新鑰節點；合法舊簽章仍違反最低版本 | rotation可信字典查ID/時間/真HMAC、RSA-PSS綁定版本；NIST63B、UserSecrets、SecureBoot、.NET10 |
| 輸入驗證與防重放 | byte長度/UTF-8/JSON規格、MAC綁定、防重放窗口與業務去重差異 | 4GB宣告先拒絕不分配；亂序可用、重複/70拒絕，壞10000不能毒化窗口 | replay真MAC/窗口/嚴格解碼；RFC8259及BinaryPrimitives/UTF8Encoding/FixedTimeEquals |
| 審計、權限與最小暴露 | Windows Token/DACL、Linux credential/父路徑search、TOCTOU、最小rights | Read成功而ReadWrite拒絕；父路徑拒絕，完整請求含祕密不可寫審計 | audit合成rights與真正白名單JSON/ID限制；Windows access-control、Linux credentials/path_resolution |
| 資安維護綜合除錯 | 威脅邊界、第一拒絕、證據與有限修正，合法/拒絕回歸 | 名稱修正後仍缺動作；方向context；輪替；不同seq同operation | diagnose真HMAC、記憶體窗口/去重與三層nullable觀察；tls與rotation獨立覆蓋其他層 |

## 真實執行與變因結果

`docs/assets/labs/security-maintenance-lab/` 有給人看的 README、完整.NET10專案、標準庫限定設定及最終實際輸出。複製到 `/tmp/ics-security-build` 用SDK10.0.401編譯，0 warning/0 error，`dotnet run --no-build -- all`兩次皆成功，stdout相同。`expected-output.txt`保存這次最終來源實際輸出；不含隨機私鑰、序號或憑證指紋。

作者另外將來源複製到獨立暫存資料夾，實際注入七種錯誤並重新編譯。每份皆編譯成功，但所選模式退出1並給出對應FAIL：名稱繞過→wrong_host_rejected；EKU繞過→client_only_usage_rejected；Any替All→partial_tunnel_not_entire_path；截止含等號→old_at_cutoff_rejected；刪最低版本→signed_old_version_rejected；壞MAC仍更新狀態→highest_not_poisoned；完整請求審計→audit_no_password。完整stdout/stderr在 `security-mutation-verification.json`，官方來源未套用缺陷。

兩張原創SVG分別掛在TLS及安全媒體章，標示前置責任、拒絕邊界與觀察位置，附title/desc/中文alt/caption，不借圖杜撰公司架構。正文函式與最終source逐一核對，Mac/VerifyMac/VerifyVersionedMac、AllRequiredSegmentsProtected、SecurityDecision等名字可在程式找到；其他片段有參數前提，不承諾單獨貼上可編譯。

## 結論與尚未驗證的部分

已驗證教材合成輸入下的機制和正/負例。自建憑證限制在單一X509Chain的CustomRootTrust，DisableCertificateDownloads=true且無網路資源；NoCheck只為離線實驗，沒有驗撤銷，不能成為正式TLS策略。MatchesHostname也不替代完整TLS握手、用途與信任政策。

未驗公司SDK、真實TLS/SRTP/VPN、OS ACL引擎、設備簽署更新、正式密碼庫或祕密管理、多節點窗口/持久去重。LabPrincipal.Authenticated是給定可信上游結果；LabEnvelope固定JSON只是同程式教學編碼，非跨平臺任意JSON標準化；記憶體HashSet不保證跨重啟或全域一次。下一步需把責任對照實際授權環境中的設定與呼叫鏈。本紀錄是作者自審，跨代理審稿由獨立報告保存。
