# 主題圖片第四版：遊戲道具與新視角

日期：2026-11-16。以遊戲房間、球場、園遊會的 Gameplay 圖作參考，重製一套新視角的宣傳圖片。先前各版保留；本次沒有上傳或變更 itch.io 網站。

## 上傳檔案

| 欄位 | 檔案 | 尺寸／格式 | 本版變更 |
|---|---|---|---|
| Banner | [Banner-Props-LowAngle-960x300.png](Media/Theme/V4/Banner-Props-LowAngle-960x300.png) | 960×300 PNG | 貼近房間地板的斜角近景，紙團、籃球、水瓶為主角；搭配米白與金黃 TOSSRUSH 標題，目標物不加 outline。 |
| Background | [Background-Carnival-Overhead-1920x1080.png](Media/Theme/V4/Background-Carnival-Overhead-1920x1080.png) | 1920×1080 PNG | 改為俯視園遊會，中央直線磚道，左右攤位、獎品、燈串、樹木與摩天輪；完整不透明場景，沒有羽化或中央遮罩。 |
| Embed BG | [EmbedBG-Upward-Sky-960x540.jpg](Media/Theme/V4/EmbedBG-Upward-Sky-960x540.jpg) | 960×540 JPEG | 從紙簍向天空仰望；左下露出藍色桶緣，右上紙團，雲集中四周，中央保留低干擾藍天供 Run game 按鈕顯示。 |

[三張上傳檔 ZIP](Media/Theme/V4/TossRush-Theme-Images-V4.zip) 只包含上表三張圖片，不含主圖與預覽。

## 使用與檢查

- Banner 已檢查標題拼字、整體裁切、前景道具與沒有目標 outline。
- Background 已修正初稿被上下邊緣切斷的樹木與帳篷，改為草地與固定寬度磚道銜接。可使用垂直重複；已製作 [兩張上下排列預覽](Media/Theme/V4/Background-Overhead-RepeatY-Preview.png)。上下列 RGB 平均差約 5.91／255、最大差 58／255，仍有輕微色差與磚紋接縫，不是逐像素無縫材質。
- Embed BG 保留天空與雲的方向，將遊戲道具放在角落；尚未套用網站檢查實際按鈕效果。
- 三張皆為參考實際遊戲內容生成的宣傳重製圖，不是未修改的 Gameplay 截圖。
- 使用內建 image_gen 工具；[完整提示詞與參考圖](ThemeImagePromptsV4.md)。主圖保存在同一資料夾的 `*-Master.png`，上傳版只做置中裁切、縮放與格式壓縮。
- 可使用 Tools/ExportItchThemeImagesV4.ps1 從保留主圖重新匯出。
