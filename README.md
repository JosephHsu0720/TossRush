# TossRush

[繁體中文](#traditional-chinese) | [English](#english)

<a name="traditional-chinese"></a>

## 繁體中文

A 20-second first-person throwing game made with Unity.

在 20 秒內，將紙團、垃圾袋、籃球與沙包投入不同目標，挑戰最高分！命中後切換場景，利用軌跡預覽、風向與碰撞找到投擲路線。

### 遊戲特色

- 四種情境：房間角落、街頭垃圾桶、側風籃球場與移動洞口。
- 空心命中與借彈加分，搭配垃圾袋迴旋等不同投擲手感。
- 卡通風格場景、目標描邊與命中回饋。

### 試玩

目前為開發中原型，以 Unity Editor 試玩為主。

1. 使用 **Unity 6000.3.11f1** 開啟專案，等待套件與資產匯入。
2. 開啟 `Assets/TossRush/Scenes/TossRushPrototype.unity`。
3. 按下 Play，再點選 **LET'S TOSS**。

| 操作 | 功能 |
|---|---|
| 移動滑鼠／左鍵 | 瞄準／投擲 |
| Esc | 釋放游標，倒數持續 |
| R | 結算後重玩 |

### 開發方式

使用 Unity／C#／URP。由本人負責玩法方向、試玩與修改回饋，AI 主導程式與場景等實作。

### 授權與致謝

專案自行建立且有權授權的內容採 [MIT License](LICENSE)。家具、按鈕、字型與音效來自 **Kenney（CC0）**；Unity／TextMeshPro 內容保留原授權。

詳見 [素材來源](Docs/AssetSources.md) 與 [第三方授權聲明](THIRD_PARTY_NOTICES.md)。

開發設定、技術內容與驗證狀態請見 [開發指南](Docs/DevelopmentGuide.md)。

---

<a name="english"></a>

## English

[繁體中文](#traditional-chinese) | [English](#english)

A 20-second first-person throwing game made with Unity.

Toss paper balls, trash bags, basketballs, and beanbags into different targets to chase a high score in just 20 seconds! Each successful throw takes you to the next scene. Use the trajectory preview, account for the wind, and bounce shots off the scenery to find your aim.

### Features

- Four settings: a room corner, a curbside trash bin, a basketball court with crosswinds, and a moving target opening.
- Clean-shot and bounce bonuses, with distinct throwing behavior such as curving trash bags.
- Cartoon-style environments, target outlines, and hit feedback.

### Play in the Editor

This is a prototype in development, currently intended for playtesting in the Unity Editor.

1. Open the project with **Unity 6000.3.11f1** and wait for packages and assets to finish importing.
2. Open `Assets/TossRush/Scenes/TossRushPrototype.unity`.
3. Enter Play Mode, then click **LET'S TOSS**.

| Input | Action |
|---|---|
| Move mouse / Left click | Aim / Throw |
| Esc | Release the cursor; the timer keeps running |
| R | Play again after the round ends |

### Development

Built with Unity, C#, and URP. I direct the gameplay, playtest, and provide feedback, while AI leads implementation, including code and scene creation.

### License and Credits

Original project content that we have the right to license is released under the [MIT License](LICENSE). Furniture, buttons, fonts, and sound effects are provided by **Kenney (CC0)**. Unity and TextMeshPro content remains under its respective original license terms.

See [Asset Sources](Docs/AssetSources.md) and [Third-Party Notices](THIRD_PARTY_NOTICES.md) for details (in Traditional Chinese).

For development settings, technical details, and validation status, see the [Development Guide](Docs/DevelopmentGuide.md) (in Traditional Chinese).
