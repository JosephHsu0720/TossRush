# TossRush

[繁體中文](README_tw.md) | [English](README.md)

使用 Unity 製作的 20 秒第一人稱投擲遊戲。

在 20 秒內，將紙團、垃圾袋、籃球、寶特瓶、報紙與飛鏢投向不同目標，挑戰最高分！命中後切換場景，利用軌跡預覽、風向與碰撞找到投擲路線。

## 遊戲特色

- 六種情境：房間角落、街頭垃圾桶、側風籃球場、園遊會移動洞口、住家信箱與酒吧飛鏢。
- 酒吧飛鏢每次進場（含首次）隨機選出一個標靶區域，黃色描邊標示當次目標；只有命中該區域才計分，投擲距離累計縮短 28%。
- 空心命中與借彈加分，搭配垃圾袋迴旋等不同投擲手感。
- 卡通風格場景、目標描邊與命中回饋。
- CC0 循環音樂，以及投擲、命中、開始與結算音效。
- 首頁喇叭圖示與音量滑桿，可靜音並記住整體音量。投擲使用短 whoosh，碰撞、扣分與獎品音效停用。

## 試玩

目前為開發中原型，以 Unity Editor 試玩為主。

1. 使用 **Unity 6000.3.24f1** 開啟專案，等待套件與資產匯入。
2. 開啟 `Assets/TossRush/Scenes/TossRush.unity`。
3. 按下 Play，再點選 **LET'S TOSS**。

| 操作 | 功能 |
|---|---|
| 移動滑鼠／左鍵 | 瞄準／投擲 |
| Esc | 釋放游標，倒數持續 |
| R | 結算後重玩 |

## 開發方式

使用 Unity／C#／URP。由本人負責玩法方向、試玩與修改回饋，AI 主導程式與場景等實作。

## 授權與致謝

專案自行建立且有權授權的內容採 [MIT License](LICENSE)。家具、按鈕、字型與介面提示音來自 **Kenney（CC0）**；投擲 whoosh 來自 **Woven SFX（CC0）**；音樂採 **Juhani Junkala／SubspaceAudio 的 Level 1（CC0）**。Unity／TextMeshPro 內容保留原授權。

詳見 [素材來源](Docs/AssetSources.md) 與 [第三方授權聲明](THIRD_PARTY_NOTICES.md)。

開發設定、技術內容與驗證狀態請見 [開發指南](Docs/DevelopmentGuide.md)。
