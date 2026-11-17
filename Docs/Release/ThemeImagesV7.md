# 第七版：暖日光房間關卡

日期：2026-11-17。使用內建 image_gen，以本專案 Gameplay-Room.png 為素材參考，重製成三張協調的宣傳圖片；不是原始遊戲截圖。本次未上傳網站。

## 視覺設計方法

- 色彩一致：暖米白、木棕、鼠尾草綠為大面積色，藍色紙簍與金黃標題為重點。
- 視覺層級：Banner 負責標題與紙團投簍的玩法；Background 家具集中兩側；Embed BG 以低干擾牆面襯托啟動按鈕。
- 材質與照明一致：低多邊形、霧面家具、午後暖日光。
- 構圖配合頁面：Banner 橫向 960×300、Background 1920×1080，Embed BG 960×540。上傳版已檢查裁切、標題、中央留白及整頁搭配。Background 無羽化遮罩，上下保留地板空間，但未宣稱逐像素無縫。

## 檔案

- [Banner](Media/Theme/V7/Banner-960x300.png)，960×300 PNG。
- [Background](Media/Theme/V7/Background-1920x1080.jpg)，1920×1080 JPEG，252,502 bytes，小於 500,000 bytes。
- [Embed BG](Media/Theme/V7/EmbedBG-960x540.jpg)，960×540 JPEG。
- [整組 ZIP](Media/Theme/V7/TossRush-Theme-V7-Room.zip)，只含三張上傳圖。
- [整頁搭配示意](Media/Theme/V7/Page-Preview.jpg)，按鈕為示意，不在上傳圖內。

主圖保存在同資料夾 *-Master.png；前版保留。重新匯出：Tools/ExportItchThemeImagesV7.ps1。

## 完整提示詞

### Banner

參考：Media/Gameplay-Room.png。

Use the attached actual TossRush room gameplay screenshot as scene and prop reference. Create a professional low-poly 3D game-page BANNER, wide 3.2:1 final 960x300. Visual design concept: a cozy playful room throwing challenge; warm ivory plaster, muted sage green curtain and rug, medium walnut wood, deep ink navy lettering outline, small warm yellow accents, blue wastebasket. Consistent matte faceted materials, soft late afternoon sunlight, gentle shadows. NOT carnival, no outdoor scene. Strong visual hierarchy and negative space. Exact title "TOSSRUSH" across left-center 65% width, TOSS warm ivory, RUSH warm golden yellow, chunky rounded block type with clean deep ink navy outline. On right side one white crumpled faceted paper ball in midair just above and left of the open blue slatted wastebasket, clear throwing moment. Room background: low bookshelf with cream teal coral books far left, a sage-curtained window behind title, wooden desk and small lamp far right, sage rug and wood floor below. Background understated and slightly softer than title and foreground. Maintain actual game's simple 3D props rather than photoreal furniture. Paper ball must read as paper not crystal. Important text and props inside inner 90% width/80% height for crop. No target outline, no yellow glow on objects, no HUD, no trajectory line, no extra words, no hands, no basketball or bottle. Full opaque edge-to-edge artwork.

### Background

參考：本版 Banner 主圖作為材質與光線基準。

Coordinated TossRush ROOM-LEVEL website artwork. Match supplied room reference's warm ivory, muted sage, walnut, ink navy and small blue accents, matte low-poly faceted 3D, soft late afternoon daylight. Reference image is ONLY for materials colors and prop identity. NO text, NO lettering or logos anywhere, NO HUD or target glow, NO carnival, NO yellow outlines. Create 16:9 1920x1080 full opaque ROOM FLOOR scene seen STRAIGHT DOWN in orthographic bird's eye view, not a front-facing room illustration. Website's central 960px column will hide middle, so all visible furnishing must be confined to outermost LEFT 22% and RIGHT 22% VERTICAL strips. Middle 56% uninterrupted warm medium brown parallel wooden floorboards. Left strip: small bookshelf seen from above with cream teal coral books, small sage cushioned chair, tiny potted plant, one small crumpled paper ball. Right strip: small wooden desk seen from above, cream lamp, notebook, blue slatted paper bin lower right, sage plant. Furniture understated, medium scale, sparse rather than tightly packed. Muted darker warm brown wood floor ensures white website panel reads cleanly; less bright and less contrast than banner. No large rug at center, no giant bookcase, NO title. Top and bottom 12% completely clear matching floorboards, same illumination and plank phase for vertical repeat; no props cut by top or bottom. No perspective convergence. FULL SCENE no vignette, feathering, fade, mask or central overlay.

### EmbedBG

參考：本版 Banner 主圖作為材質與光線基準。

Coordinated TossRush ROOM-LEVEL website artwork. Match supplied room reference's warm ivory, muted sage, walnut, ink navy and small blue accents, matte low-poly faceted 3D, soft late afternoon daylight. Reference image is ONLY for materials colors and prop identity. NO text, NO lettering or logos anywhere, NO HUD or target glow, NO carnival, NO yellow outlines. Create 16:9 launch screen 960x540. Minimal companion to busy Banner. Main image is the room's warm ivory plaster wall, smooth and matte. A small glimpse of sage curtain at extreme upper LEFT corner, soft broad window-light shadow near LEFT edge, tiny wooden wall shelf with two muted books at extreme lower RIGHT. Wall occupies 95% frame, central 70% entirely uncluttered uniform pale warm ivory, low contrast edge shadows. No window frame in center, no wall clock, no title, no button drawn, no blue wastebasket or paper ball. Real coral Run game button will overlay clean center, maintain visibility. Same room atmosphere but far fewer objects. No dramatic diagonal shadows through center, no floor, no vignette.

