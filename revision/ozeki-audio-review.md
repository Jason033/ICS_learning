# Ozeki API 系列：本機音訊控制改寫審查

審查日期：2026-10-04

## 本次改寫範圍

舊系列多數篇幅放在 SIP 註冊、撥號與通話事件，和使用者確認的學習目標不符。新版將重心移到本機音訊裝置、資料 handler、路由、錄音和故障定位。電話只用來說明本機音訊跨越 call media sender/receiver 的邊界；SIP 註冊和完整 RTP 分析分別留在 VoIP 與 Wireshark 系列。

七個既有 lesson ID/URL 均保留，`contentVersion` 升為 3，移除舊版 `studyTime`。沒有新增未經驗證的公司 API、iCallAPI wrapper 或公司部署流程。程式片段不是同一個虛構公司系統的完整實作：精確 Ozeki 呼叫會標註公開文件版本；其餘以架構示意或 synthetic 診斷資料清楚標示。

## 建議目錄順序與摘要

依序建議為：

1. `documentation-and-version` — **先核對 Ozeki／音訊 SDK 的版本與契約**：從專案依賴、載入組件、.NET 目標框架和公司封裝辨認實際契約，避免把別版範例當成本地 API。
2. `read-a-call` — **從本機來源追到播放端：資料流與元件責任**：沿麥克風/檔案來源、處理節點、喇叭/錄音器畫出有方向的資料圖。
3. `media` — **用 MediaConnector 建媒體圖：分流、混音與錄音**：分辨一對多分流、多對一合流和錄音掛點，按版本驗證拓樸。
4. `lifecycle-and-events` — **啟停、切換裝置與資源生命週期**：明確指定 owner，處理裝置切換、釋放、插拔通知及晚到 callback。
5. `registration` — **格式轉換與觀測：資料存在不等於聲音可用**：計算具體 PCM 區塊量，追轉換位置，解讀量值與事件的證據界線。
6. `calls-and-events` — **電話中的音訊邊界：本機媒體如何交給通話**：分開檢查發送與接收方向的本機路由、call 歸屬和媒體活動。
7. `evidence-troubleshooting` — **本機音訊整合診斷：從症狀定位交接點**：以同一操作 ID 串起裝置、handler、錄音和通話證據，寫出下一個能區分原因的觀察。

這七篇建議列為同一個核心順序；本系列沒有另外標成選讀的章節。第一篇的版本盤點是 Ozeki API 具體範例的前置條件。若讀者只維護純本機音訊，可把第六篇當作越界參考；不要將它列為本機路由理解的先修課。目錄總摘要建議：**「從本機音訊裝置與資料路由開始，學會追查播放、錄音、格式及資源生命週期；電話只作為 media sender/receiver 邊界，SIP 與 RTP 細節另見 VoIP 系列。」**

## 官方技術資料核對

本次直接查看 Ozeki 官方下載/版本紀錄和官方 Media API 頁面。頁面間存在必須保留的版本 caveat：下載頁頂部列 Ozeki SDK 10.5.1、.NET 10 套件並標示 2026-07-08；同一發行頁的歷史區亦列有 11.x、12.x 和早期項目，產品/版本標籤不能簡化成一條無歧義的版號線。MediaConnector、Microphone、Speaker、PhoneCallAudioSender/Receiver 與 AudioMixer 參考頁明確標記 OzekiSDK 1.8.12.0。因此教材不宣稱這些舊頁 API 簽名能直接套用到下載頁任何新分支，並把先查本地 DLL、NuGet/部署項和 wrapper 設為第一篇。

已確認並用到的公開技術事實：

- Ozeki MediaHandlers 教學說明 handler 可代表來源或目的端，`MediaConnector` 建立來源到目的端的媒體路徑，教學涵蓋 microphone、speaker、檔案播放、錄音及 handler 啟停/釋放。該教學是公開產品範例，不是公司程式規格。
- Ozeki `MediaConnector` 1.8.12 參考列出單向 `Connect`、`Disconnect`、`Dispose`，並描述 source/destination 格式自動轉換。教材只把此行為標成 1.8.12 文件內容，不推定所有格式、wrapper 或其他版本相同。
- Ozeki `Microphone` 1.8.12 頁列 `MediaFormat`、`State`、`Initialized`、`Level`、`Muted`、`Start/Stop` 與相關事件；該頁對 Level 的 0–100 描述不等於分貝單位，故教材明確禁止把它當 dB。
- Ozeki `Speaker` 1.8.12 頁描述播放端、格式、狀態及設備方法。教材將「handler Started」與「實際聽見」分開驗收。
- Ozeki `AudioMixerMediaHandler` 參考和官方 FAQ 提供舊式多來源合流例子；官方發行紀錄提及 10.4.58 的多接收端轉送修正，以及 11.0.0 多來源可不經 mixer 的媒體改變。因此章節教拓樸概念並要求按實際版本測試，不把 mixer 宣稱為永遠必要或永遠多餘。
- 官方發行紀錄 10.3.193 提及多個使用者操作同一實體裝置時會相互影響，並列出 managed 操作方法。教材把它當成特定發行版本的共享資源案例，不推定任何公司 wrapper 已實作相同語義。
- `PhoneCallAudioSender.AttachToCall` 和 `PhoneCallAudioReceiver` 的舊版 API 參考用來說明電話媒體邊界。教材不把註冊、SIP transaction、RTP 封包格式或網路故障分析塞回本機音訊主線。

來源直連：

- [Ozeki SDK 下載與版本紀錄](https://www.voip-sip-sdk.com/p_7021-download-ozeki-voip-sip-sdk.html)
- [Ozeki MediaHandlers 教學](https://www.voip-sip-sdk.com/p_7536-how-to-use-media-handlers.html)
- [MediaConnector API（頁面標示 1.8.12）](https://voip-sip-sdk.com/doc/html/fd3ef57a-c7f3-df17-509b-9d11890938e5.htm)
- [Microphone API（頁面標示 1.8.12）](https://voip-sip-sdk.com/doc/html/1acde4be-300f-ff36-9df0-54b1d2a849b8.htm)
- [Speaker API（頁面標示 1.8.12）](https://voip-sip-sdk.com/doc/html/13fd25fe-5822-b770-de51-afd58fd7c14f.htm)
- [AudioMixerMediaHandler API（頁面標示 1.8.12）](https://voip-sip-sdk.com/doc/html/315fe8a4-449c-06a0-a5a8-c6c2721fde4d.htm)
- [PhoneCallAudioSender.AttachToCall（頁面標示 1.8.12）](https://voip-sip-sdk.com/doc/html/5743b1d9-48ea-6352-4019-09e205aa6191.htm)
- [PhoneCallAudioReceiver API（頁面標示 1.8.12）](https://voip-sip-sdk.com/doc/html/f5a4c883-82b4-f01a-1ed8-f319cb8d8a81.htm)
- [Ozeki 官方 FAQ：多來源 AudioMixer 範例](https://www.voip-sip-sdk.com/p_7227-frequently-asked-questions.html)
- [Microsoft `dotnet list package`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-list-package)
- [Microsoft `AssemblyName.GetAssemblyName`](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assemblyname.getassemblyname?view=net-10.0)

## 教材品質與邊界自查

七篇依「先確認 API 是誰 → 畫出本機資料路徑 → 決定分流/合流/錄音位置 → 管理資源 → 查格式和觀察點 → 接到電話媒體邊界 → 整合診斷」推進。每篇保留前文所需概念並在實際音訊程式脈絡中解釋 C# API，不重教變數/迴圈，也不使用高中程度的人物故事。練習涵蓋基礎辨義、具體計算、跨節點推理與診斷，且答案說明判斷和驗收。

所有案例 ID、狀態、資料率、檔案結果均為 synthetic 教學資料，沒有一項聲稱來自公司系統或實際量測。硬體僅談 OS 裝置選擇、權限、插拔和 handler 交接，不包含 ADC/DAC 電路、DSP 公式或硬體設計。未知事項明示為未知：公司 SDK/target framework、wrapper 是否包 Ozeki 或 iCallAPI、裝置共享方式、handler 執行緒契約、目前實際資料圖、發行版與部署 DLL 均尚未得知。

驗證結果：7 個 lesson JSON 均可 parse；全數 `contentVersion: 3` 且不含 `studyTime`；每篇有完整段落、5 層練習與多個來源；舊 lesson ID 保留。目錄檔尚未同步，依任務約定交由主協調者統一改寫。瀏覽器/硬體播放未在未知公司環境驗證，不把 JSON 結構檢查說成實機音訊驗收。

## 整合者的獨立覆核

我逐篇核對七篇的概念順序、程式片段、例題推論和答案，並抽查官方 API 頁面。這一組按「先認出自己讀的是哪個 SDK → 畫本機資料流 → 判斷路由和資料格式 → 管理生命週期 → 接上通話邊界 → 用同一操作的證據除錯」推進；沒有把公司 wrapper 當成 Ozeki，也沒有把開始呼叫、連線存在或檔案建立說成聲音已成功。

技術核對確認：Ozeki 公開 MediaConnector 1.8.12 頁明定 Connect 的資料由 sender 流向 receiver，並列出該頁版本及格式轉換說明；官方下載頁同時有 10.5.1/.NET 10 的下載項和分列的歷史版本，因而新版正文正確要求讀者先核對本地組件，不能把舊型別簽名套到任意版本。官方 API 還將 `PhoneCallAudioReceiver` 定義為繼承 `AudioSender`：它從電話接收資料，接著作為本機媒體圖的來源送往 Speaker。原章雖然接線方向正確，類別名稱容易讓讀者以為方向相反；已在[通話邊界篇](../docs/content/lessons/ozeki--calls-and-events.json)補上「相對電話／相對本機圖」兩種角色的說明，並把練習改成檢查這個型別關係。

對照來源：[Ozeki MediaConnector 1.8.12 參考](https://voip-sip-sdk.com/doc/html/fd3ef57a-c7f3-df17-509b-9d11890938e5.htm)、[PhoneCallAudioReceiver 1.8.12 參考](https://voip-sip-sdk.com/doc/html/f5a4c883-82b4-f01a-1ed8-f319cb8d8a81.htm)、[官方下載與版本紀錄](https://www.voip-sip-sdk.com/p_7021-download-ozeki-voip-sip-sdk.html)、[官方媒體串接教學](https://www.voip-sip-sdk.com/p_7536-how-to-use-media-handlers.html)。這些資料支持公開 SDK 的具體描述，不能證明公司部署版本或 wrapper 語義。

驗收仍有清楚邊界：JSON 與教材結構可以本機檢查；因讀者公司的 SDK、目標 .NET、包裝層及實際音訊設備未知，本組不宣稱公司程式可直接編譯，也未宣稱完成硬體播放驗收。主目錄已按教材建議順序更新，網站互動 smoke 需等其餘同步改寫系列的目錄版本一致後統一執行。
