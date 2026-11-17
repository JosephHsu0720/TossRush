# 主題圖片第三版：完整園遊會與天空

日期：2026-11-16。依本人回饋，取消 Background 的羽化／中央遮罩，改為完整場景；Embed BG 改為天空與雲。圖片保存在獨立 V3 資料夾，前兩版保留，本次未修改網站。

## 上傳檔案

| 欄位 | 檔案 | 尺寸／格式 | 適用方式 |
|---|---|---|---|
| Background：完整場景 | [Background-Carnival-Full-1920x1080.jpg](Media/Theme/V3/Background-Carnival-Full-1920x1080.jpg) | 1920×1080 JPEG | 現有左右攤位、彩旗、燈串、摩天輪構圖延伸成完整園遊會，中央是實際道路，沒有羽化遮罩。此版有透視近大遠小，建議置中、不重複。 |
| Background：直向重複構圖替代版 | [Background-Carnival-Full-RepeatY-1920x1080.png](Media/Theme/V3/Background-Carnival-Full-RepeatY-1920x1080.png) | 1920×1080 PNG | 同樣完整且無羽化，採平行道路構圖，上下排列時道路寬度一致，適合繼續使用 Repeat Y。 |
| Embed BG | [EmbedBG-Sky-960x540.jpg](Media/Theme/V3/EmbedBG-Sky-960x540.jpg) | 960×540 JPEG | 藍天與低多邊形白雲，雲集中於邊緣，中央留乾淨藍色供原生 Run game 按鈕顯示。 |

Banner 可繼續使用 V2 的無 outline 版本，本次沒有重做。

## 垂直排列檢查

完整透視場景沒有宣稱能無縫平鋪。若要保留前一版的垂直重複設定，請改選直向重複構圖替代版，而不是完整透視版。

替代版已產出 [上下兩張拼接預覽](Media/Theme/V3/Background-Full-RepeatY-Preview.png)，檢查道路寬度、攤位分布與銜接。其第一與最後列 RGB 色頻平均差約 3.99／255、最大差 51／255，並非逐像素無縫材質；圖案仍會呈現重複。

## 製作與檔案

- 使用內建影像生成工具，參考上一版園遊會背景與 Embed BG；這些是宣傳場景圖，不是原始 Gameplay 截圖。
- 所有區域均為不透明完整畫面，沒有新增遮罩或羽化處理。
- 生成主圖以 `*-Master.png` 保留；上傳版只做尺寸正規化與格式壓縮。
- [完整提示詞](ThemeImagePromptsV3.md)；重新匯出工具為 Tools/ExportItchThemeImagesV3.ps1。
- [三張新版圖片 ZIP](Media/Theme/V3/TossRush-Theme-Images-V3.zip) 包含完整園遊會、直向重複構圖替代版與天空 Embed BG；不包含原圖與拼接預覽。
