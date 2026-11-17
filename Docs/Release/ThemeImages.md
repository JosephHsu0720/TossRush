# itch.io 主題場景圖片

最新 [第七版：房間關卡](ThemeImagesV7.md)：統一暖日光、木地板與鼠尾草綠，Banner 紙團投簍、Background 俯視家具、Embed BG 簡化牆面。Background 小於 500 KB。

先前 [第六版](ThemeImagesV6.md)：沿用第五版藍調園遊會配色，Banner 加入投擲動感、背景縮小場景裝飾、Embed BG 改為乾淨天空；Background 小於 500 KB。[第五版紀錄](ThemeImagesV5.md)。

先前 [第四版](ThemeImagesV4.md)：低角度道具近景 Banner、俯視園遊會 Background、從紙簍仰望天空的 Embed BG，三張均使用遊戲場景與道具作參考。

先前 [第三版](ThemeImagesV3.md)：取消羽化遮罩的完整園遊會場景，另附直向重複構圖替代版，以及天空＋雲 Embed BG。Banner 沿用第二版無 outline 版本。

目前另有依實際頁面回饋製作的 [第二版](ThemeImagesV2.md)：Banner 無目標 outline、園遊會可垂直重複背景、簡化 Embed BG。以下保留第一版的紀錄與檔案。

日期：2026-11-16。已查看目前的 [TossRush 草稿頁](https://redjojo235.itch.io/tossrush) 與開啟中的 Edit Theme。當時 Banner／Background／Embed BG 均尚未上傳；內容欄與遊戲區寬度為 960 px，遊戲框明確設定為 960×540。

以下尺寸是依目前版面選定的製作尺寸，**不是 itch.io 三個欄位的硬性尺寸規定**。本次只產出檔案，沒有上傳圖片或保存網站主題變更。

| 上傳欄位 | 檔案 | 尺寸／格式 | 用途與構圖 |
|---|---|---|---|
| Banner | [Banner-960x300.png](Media/Theme/Banner-960x300.png) | 960×300，PNG，不透明 | 房間場景、米白／金黃色 TOSSRUSH 標題、右側藍桶與紙團。寬幅標題圖；若之後改內容欄寬度，需重新檢查縮放。 |
| Background | [Background-1920x1080.jpg](Media/Theme/Background-1920x1080.jpg) | 1920×1080，JPEG，不透明 | 黃昏庭園；左右邊緣是樹木、花叢、信箱與房屋，中央低對比深藍。用作整頁底圖，中央內容欄覆蓋後仍能看到兩側裝飾。建議置中、不重複，以 #101c26 接底；不是無縫平鋪圖。 |
| Embed BG | [EmbedBG-960x540.jpg](Media/Theme/EmbedBG-960x540.jpg) | 960×540，JPEG，不透明 | 球場實景參考圖，移除 HUD 與文字，中央留給 itch.io 原生 Run game 按鈕。用於啟動前的遊戲框背景。 |

## 來源與揭露

三張均使用內建影像生成工具，以本專案的 Gameplay 截圖和已完成封面作參考，屬於宣傳用的場景重製圖；並非未修改的實際遊戲截圖。未使用網路上的第三方照片。

完整提示詞與參考圖對應見 [ThemeImagePrompts.md](ThemeImagePrompts.md)。

參考來源：

- Banner：Media/Cover-Gameplay-Master.png、Media/Gameplay-Room.png。
- Background：Media/Gameplay-Garden.png。
- Embed BG：Media/Gameplay-Court.png、Media/Cover-Gameplay-Master.png。

原始生成主圖保存在 Media/Theme 的三個 `*-Master.png`。上傳用檔案只做中央裁切、尺寸正規化與格式壓縮，沒有改動生成主圖。可透過 `Tools/ExportItchThemeImages.ps1` 重新匯出。

itch.io 的 AI generation disclosure 已選 Graphics；新增的三張主題圖片也應納入圖像生成使用說明。

## 上傳後的檢查

1. 將三張上傳用檔案各自放入相同名稱的 Edit Theme 欄位；不要把 Master 主圖誤當 Cover 上傳。
2. 背景圖若出現位置／平鋪／適應螢幕選項，優先採置中、不重複，並檢查兩側庭園沒有被過度裁切。圖中沒有文字或必要資訊，窄螢幕裁切不會遺失操作說明。
3. Banner 會取代頁面原本的文字標題，因此已將 TOSSRUSH 放入圖內。
4. 確認 Embed BG 上的 Run game 按鈕清楚；按下啟動後顯示實際遊戲，不應把背景圖誤認為遊戲內畫面。
5. 預覽桌面與窄視窗後，再保存主題。既有 BG2 白色內容欄與深色文字可先維持，讓外圍深色背景與內容清楚區隔。

官方參考：[Designing your page](https://itch.io/docs/creators/design#images-banner-background)、[HTML5 embed customization](https://itch.io/docs/creators/html5#customizing-how-your-game-is-embedded)。
