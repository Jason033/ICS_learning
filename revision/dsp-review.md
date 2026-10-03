# 訊號、DSP與SDR：作者審查

八篇先補數學符號、取樣與資料表示，才讀C#及GNU Radio流程。保留原 `time-frequency`、`sdr-intro` URL，新增六篇。正文服務於公司程式閱讀、維護與除錯，不以運算元量取代基礎。

| 章節 | 核心機制與維護能力 | 可重建例題／模型 |
| --- | --- | --- |
| time-frequency | x[n]/t/fs、cos/弧度、j/Σ、DFT正負頻、DC/Nyquist、正逆尺度、窗與零填充。 | 四點X=[0,2,0,2]；Ndata400與Nfft800分開、50ms名義與49.875ms首末。`spectrum`。 |
| sampling-and-aliasing | 取樣與量化、不可辨別性、前濾波、實數Nyquist邊界、抽取與插值、相位狀態與時鐘率差。 | 8ksps下1k/7k phase0cos逐份同值；3k在降至4k後別名1k。`sampling`。 |
| filters-and-delay | FIR摺積、歷史／生命週期、頻率倍率與相位、線性相位群延遲；IIR基礎、成批與CPU分賬。 | 三tap輸出[.25,1,2,3]；重置後半[.75,2.5]；1k倍率.853553、2k僅−6.02dB。`filter`。 |
| noise-and-snr | mean/均方/RMS/方差、複數功率、SNR參考與相關交叉項、增益位置、削波、量化、頻帶與PSD。 | 均方1/.0625、12.0412dB；整體gain不改善SNR，削波error=y−s。`snr`。 |
| symbols-coding-and-ber | bit/symbol/sample、對映、成形/ISI/定時、FEC合法碼距離、交錯代價、錯誤分母。 | 實際三重複，分散2/12可修為0/4，集中兩錯誤判；九位置交錯手推。`coding`。 |
| iq-and-baseband | 兩軸j/phase、RF符號與½功率約定、正負complex頻、共軛/交換、頻移、格式與metadata。 | .6+j.8乘−j成.8−j.6；四點正／負旋轉DFT峰k1/k3，不乘2。`iq`。 |
| sdr-intro | SDR硬體／軟體邊界、stream type/rate/state、樣本/CPU/Throttle時鐘、GRC具體純模擬引數。 | 真Tone→FIR→抽取→DFT，32/8k→16/4k，peak2=1k；GNU Radio文件表另列未GUI實跑。`pipeline`。 |
| dsp-troubleshooting | 資料契約、代表點、顯示與實際變換、chunk等價、混疊/IQ/譯碼/削波多候選。 | 1k被錯軸標2k與錯尺度.5；FIR邊界.75vs2；8題新輸入推理。`diagnose`。 |

每篇至少兩worked-example、讀碼、實驗變因／預期失敗／復原、診斷分支與8題三層多段答案。共64題，頻率、時間、編碼與power問題都給輸入與單位。篇幅是警戒，不以長度宣稱學習效果或職稱。

來源已browse核對：GNU Radio FFT、Sample Rate、Rational Resampler、Filter Taps、Decimating FIR、IQ、Noise、PSK與官方FEC manual；GRC官方入門與FAQ（避開已廢棄3.7 Python生成教程）；FFTW DFT約定；Microsoft Complex與FromPolarCoordinates弧度；Analog Devices MT001/MT002及原著DSPGuide的DFT／摺積／取樣。每章sources指向對應原頁。正文為原創推導與固定新案例，非貼來源長文。

特別稽核：N/fs名義與(N−1)/fs首末；Ndata/Nfft分離；實數單側factor2僅非DC與偶N之非Nyquist；complex單旋轉不乘2；噪聲功率先平方；error統一y−s；三tap不滿足任意抗混疊（2k只−6dB）；RF包絡功率½需固定約定；code rate無單位；失敗frame不算正確bit；CPU1ms僅教學假設，不報真實效能。

已實際在隔離副本用 .NET 10.0.401 編譯、八模式all29項機制透過，預期輸出由實跑存檔。拿掉正確逆變換除N（改除1）應使`spectrum`退出1，說明相關驗收能抓尺度錯；證據 [dsp-model-verification.json](dsp-model-verification.json)。來源保持正確，無bin/obj。

未驗證：實際GNU Radio GUI、硬體RF/ADC/DAC、音效卡、現場噪聲與公司DLL。模型僅直接DFT、小FIR、已對齊三重複碼、固定tone和理想complex運算，非完整高效接收機。GRC操作檔案與真實C#實跑分開。圖資料、單位和生成公式在章節／繪圖說明，不能當裝置量測結果。跨作者審稿由主代理另記錄。

已另用matplotlib實際生成兩張standalone SVG（四點DFT、1k／7k混疊），Python檢查代表點，保存JSON並視覺檢查PNG預覽；沒有用任意裝飾曲線代替科學資料。生成器及白話說明在 `docs/assets/labs/dsp-plot-diagrams.py`、`dsp-plots-readme.md`；圖中英文軸搭配網站中文alt與caption。另有原生SVG資料率圖，參照實跑pipeline數量与rate。
