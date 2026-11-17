# itch.io 主題圖片第二版

日期：2026-11-16。依本人提供的實際頁面預覽調整；三張均已匯出獨立新版，舊版保留。本次沒有上傳或修改 itch.io 主題。

| 欄位 | 建議上傳檔案 | 尺寸／格式 | 變更 |
|---|---|---|---|
| Banner | [Banner-NoOutline-960x300.png](Media/Theme/V2/Banner-NoOutline-960x300.png) | 960×300 PNG | 移除藍色垃圾桶的黃色 outline，保留桶身、場景與米白／金黃色標題。 |
| Background | [Background-Carnival-RepeatY-1920x1080.png](Media/Theme/V2/Background-Carnival-RepeatY-1920x1080.png) | 1920×1080 PNG | 園遊會條紋攤位、玩偶／疊罐、彩旗、燈串與摩天輪氣氛；中央保持低對比深藍，頂底留暗色過渡，適合垂直重複。 |
| Embed BG | [EmbedBG-Simple-960x540.jpg](Media/Theme/V2/EmbedBG-Simple-960x540.jpg) | 960×540 JPEG | 移除中央球場、籃框、鐵網與亮色目標，改為單純深藍底，只在角落保留淡線條與小籃球，讓原生紅色 Run game 按鈕清楚。 |

## Background 設定與檢查

- 建議 **水平置中、垂直重複（Repeat Y / Vertically repeat）**，不要選水平與垂直都重複。
- 若要保持圖樣比例，先以原尺寸顯示，不使用拉伸填滿整頁；窄螢幕只會裁切非必要側邊裝飾。
- 建議底色 `#0C202B`；可延續既有白色內容欄與深色文字。
- 已製作兩張上下拼接的 [重複預覽](Media/Theme/V2/Background-RepeatY-Preview.png)，視覺檢查沒有明顯硬接縫。
- 匯出 PNG 的第一與最後一列，RGB 各色頻平均差約 0.59／255、最大差 4／255；並非逐像素完全相同。留有足夠暗色過渡，使上下排列平順。

## 製作紀錄

- 使用內建影像生成工具修改 Banner，並以本專案園遊會截圖與本人提供的頁面截圖作為其他圖片的參考。三張都是宣傳圖片，並非未修改的遊戲截圖。
- 主圖及上傳成品保存在 `Media/Theme/V2/`，提示詞見 [ThemeImagePromptsV2.md](ThemeImagePromptsV2.md)。
- `Tools/ExportItchThemeImagesV2.ps1` 可從主圖重新匯出指定尺寸。
- 上傳用三張圖片包在 [TossRush-Theme-Images-V2.zip](Media/Theme/V2/TossRush-Theme-Images-V2.zip)；拼接預覽和 Master 不在此上傳包內。
