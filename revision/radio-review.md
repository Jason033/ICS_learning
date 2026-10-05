# 無線電通訊主題重寫審查

## 本次重寫要解決什麼

舊稿先展開射頻公式、儀器參數與硬體細節，讀者還沒建立「應用程式請求如何經設備成為遠端可驗證內容」的流程，就要同時消化 dB、噪聲、調變和天線。新稿把主線改成軟體維護可用的端到端路徑：辨認角色和方向、追設定的來源與回讀、確認模式相容、讀接收端品質證據、比較量測條件，再用最後正常邊界定位故障。

教材的音訊範圍停在既有內容交到無線設備介面、以及遠端恢復後交到受測上層介面；不重教麥克風擷取、本機媒體處理或喇叭播放。硬體章只提供軟體與設備之間的責任邊界，不要求設計天線、匹配電路或測試治具。

## 建議學習路徑

| 次序 | lesson ID | 建議章名 | 定位 |
|---|---|---|---|
| 1 | `tx-rx-path` | 發射與接收：從應用控制到空中鏈路 | 核心起點；標出設定、控制、內容、RF 與遠端結果各自能證明什麼。 |
| 2 | `parameters` | 頻率與通道：讀懂設備設定資料 | 核心；追設定來源、角色方向、模式映射、設備回覆及回讀。 |
| 3 | `modulation-and-coding` | 調變與編碼：通訊模式在流程中的位置 | 核心；只教必要的端到端職責和相容性，不做數學推導。 |
| 4 | `duplex-and-repeater` | 收發方向與中繼：分段驗證路徑 | 核心；分開兩端角色、方向、門控、PTT 和中繼狀態。 |
| 5 | `rssi-snr` | RSSI、SNR 與接收品質：讀值不等於內容成功 | 核心；讀強度、雜訊／干擾和解碼結果的證據邊界。 |
| 6 | `measurements-and-logs` | 射頻量測與日誌：建立可比證據 | 核心；固定測點、儀器設定、時間來源與工作識別。 |
| 7 | `rf-chain-and-antenna` | RF 硬體鏈與維護邊界（選讀） | 有設備告警或 RF 交接需求時再讀；不屬於軟體工程師必修硬體設計。 |
| 8 | `radio-troubleshooting` | 無線電通訊整合除錯 | 核心收束；將前述內容用於重現、假說、單變因檢查和回歸。 |

catalog 的順序、摘要和「選讀」標籤由整合者統一處理；本次未修改 `catalog.json`。

## 內容設計和重要限制

- 每篇先交代正常流程與角色責任，再用一筆設定、狀態、日誌或合成事件走到可檢查結果；練習要求讀者提出下一個有區辨力的觀測，而非背常見問答。
- 所有 `U/D`、`CH-A`、模式名稱、工作 ID、毫秒及品質數字均為 **Synthetic 教學資料**，不代表任何頻點、合法配置、實機門檻或品牌 API。
- `RSSI`、`SNR`、`SINR`、`BER/PER` 的意義依標準、產品和量測定義而定。課程要求保存接收端、參考點、頻寬／模式、時間窗、來源和分母；不把一個廠商或行動通訊標準的定義當成所有無線電的通則。
- 通道是否可用取決於適用地區、服務、授權與受控配置。本教材不列可直接發射的頻率組合，也不從國際規則推導特定部署的許可。
- 調變、編碼與 MCS 只用來分清資料路徑和故障邊界。標準中的調適行為只在該標準與設備文件確認支援時適用，不能外推到其他專用無線電。
- RF 儀器範例用來解釋量測設定會影響觀測結果，不代表軟體工程師應自行接線或執行發射測試。實機連接、門檻和維護動作依設備／儀器手冊與組織程序。
- 《RF 硬體鏈》保留反射／負載告警等軟體交接概念；未保留天線設計、自由空間路徑損失計算、dB 公式推導、VSWR 換算或電路設計練習。

## 技術依據及其適用範圍

- [ITU Radio Regulations 2024](https://www.itu.int/hub/publication/r-reg-rr-2024/) 說明無線電規則與頻率配置背景。課程用它支持「配置需依地區與適用規則核對」，不據此列出可使用頻率。
- [ITU-R SM.328](https://www.itu.int/dms_pubrec/itu-r/rec/sm/R-REC-SM.328-12-202509-I!!TOC-HTM-E.htm) 涵蓋發射頻譜及多種調變範例；正文只以它支持模式屬於有規則、有頻譜後果的介面，不複製技術公式。
- [ITU-R M.2012-7](https://www.itu.int/epublications/publication/recommendation-itu-r-m-2012-7-02-2026-detailed-specifications-of-the-terrestrial-radio-interfaces-of-international-mobile-telecommunications-advanced-imt-advanced) 描述 IMT-Advanced 的特定無線介面與鏈路調適。教材明確限制其適用範圍，不把 MCS 動態調整寫成所有設備行為。
- [ETSI TS 136 214／3GPP TS 36.214](https://www.etsi.org/deliver/etsi_TS/136200_136299/136214/15.02.00_60/ts_136214v150200p.pdf) 的 LTE RSSI 定義包含指定量測頻寬中的總接收功率及受控量測條件。用來說明 RSSI 定義要看標準，不宣稱專用無線電也一定如此。
- [ETSI TS 102 177](https://www.etsi.org/deliver/etsi_TS/102100_102199/102177/01.01.01_60/ts_102177v010101p.pdf) 是特定 HIPERMAN PHY 規格；其中 RSSI 與 CINR 的量測／統計區別只用於展示「強度量和接收運作狀況可能是不同觀測」的標準範例。
- [Rohde & Schwarz 頻譜分析基礎](https://www.rohde-schwarz.com/us/products/test-and-measurement/essentials-test-equipment/spectrum-analyzers/understanding-basic-spectrum-analyzer-operation_256005.html) 說明中心／span、reference level、RBW、VBW 對頻譜儀畫面、解析能力、噪底與掃描時間的影響。用於建立量測紀錄要保留 setup 的理由。
- [ARRL Radio Lab 中繼教材](https://www.arrl.org/files/file/Radio%20Lab%20Handbook/lesson%206/RLH%20Unit%206.pdf) 和 [Icom IC-2820H 使用手冊](https://www.paloalto.gov/files/assets/public/v/1/oes/ares-races/ic-2820h-2.pdf) 支持中繼接收／發射方向及特定設備的控制操作例。它們是業餘無線電／特定型號語境，沒有被當成所有公司系統或其他地區的通用規則。
- [Rohde & Schwarz VSWR 與 return loss 說明](https://www.rohde-schwarz.com/us/products/test-and-measurement/essentials-test-equipment/spectrum-analyzers/voltage-standing-wave-ratio-vswr-and-return-loss_258140.html) 支持匹配／反射術語用途；教材不提供通用硬體門檻或調校指示。

## 本次檔案與自查

- 重寫 8 個原有 lesson JSON；檔名與 lesson ID 不變，故既有課程網址可沿用。
- 8 篇均改為 `contentVersion: 3`，移除 `studyTime`；每篇有章節、程式／紀錄練習及解答。
- 已用 Python `json` parser 逐篇讀取：8 個檔案均可解析；內容均為 v3、沒有 `studyTime`，每篇 5 題練習。章節數為 6 篇各 6 節、整合除錯 7 節。
- 整合者已將八篇新版版本標記、順序、摘要與選讀標籤整合至 catalog；整體 smoke 仍須待其他同步重寫中的主題版本一致後再跑。
- 本篇是作者自查，不等於獨立領域審查或硬體驗收。實際系統特有的欄位語意、設備條件與適用法規仍須以其正式文件核實。

## 獨立覆核與修正

另一位審查者核對八篇的流程、推論、技術範圍與練習，沒有發現需阻擋的領域錯誤，並確認本主題將本機媒體處理留在 Ozeki 系列。審查指出兩個程式範例問題，整合時已修正：

- 儀器關聯紀錄的 C# `Event` 字串多出跳脫反斜線，範例原本無法編譯；已改為一般字串常值。
- 收發會話的 `ReleaseAsync` 若失敗，佇列清理會被略過；已以巢狀 `try/finally` 保證佇列清理仍執行，並補充記錄各項清理失敗的注意事項。

獨立覆核另核對 RSSI 定義依標準、頻譜儀設定、特定語境的中繼方向，以及 VSWR／return loss 的適用界線。此審查是內容和推理核對，不是實機射頻驗收，也不代表任何公司無線電設備的實際定義。
