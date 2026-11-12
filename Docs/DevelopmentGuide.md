# TossRush 開發指南

A 20-second first-person throwing game prototype made with Unity.

在 20 秒內，將紙團、垃圾袋、籃球與沙包投入不同目標，挑戰更高分。每次命中後切換情境，利用飛行軌跡預覽、風向與場景碰撞調整投擲路線。

目前為 Unity Editor 可試玩的開發中原型，尚未提供已驗證的 Windows／WebGL 發行版本。

## 開啟專案

1. 安裝 Unity Hub 與 **Unity 6000.3.11f1**。
2. Clone 或下載此 repository，在 Unity Hub 加入專案根目錄。
3. 使用指定版本開啟，等待 Unity 還原套件與匯入資產。
4. 依下方步驟開啟場景並試玩。

主要依賴：URP 17.3.0、Input System 1.19.0、uGUI／TextMeshPro 2.0.0、Unity Test Framework 1.6.0。版本以 [manifest.json](../Packages/manifest.json) 與 [packages-lock.json](../Packages/packages-lock.json) 為準，由 Unity Package Manager 還原。

目前以桌面滑鼠操作為主；行動裝置與瀏覽器相容性尚待驗證。

## 在 Editor 試玩

1. 點選 **TossRush → Open Prototype**，或開啟 `Assets/TossRush/Scenes/TossRushPrototype.unity`。
2. 按 **Play**，再按 **LET'S TOSS**。移動滑鼠瞄準，左鍵投擲；細線預覽飛行路徑。
3. **Esc** 釋放滑鼠，點擊場景重新捕捉；20 秒仍繼續倒數。
4. 右上角的向外／向內四角圖示切換畫面大小。Editor 切換 Game View 最大化／還原；執行檔分支切換全螢幕／視窗。遊玩中先按 Esc 再點擊。
5. 結算後按 **ONE MORE ROUND** 或 **R** 重玩。

| 順序 | 關卡 | 投擲物／目標 | 計分 |
|---|---|---|---|
| 1 | Corner Shots：房間角落 | 紙團／垃圾桶 | 基礎 100；空心 +50，或每次其他物件碰撞 +30 |
| 2 | Curbside Spin：街頭 | 黑色垃圾袋／開蓋輪式垃圾桶 | 投進 200；直接入桶與借桶蓋滾入相同 |
| 3 | Crosswind Court | 籃球／籃框 | 基礎 150；空心 +100 |
| 4 | Moving Midway | 沙包／左右移動洞口 | 基礎 200；空心 +100 |

只有命中才切換，沒投進可繼續重投；四關共用 20 秒，轉場不暫停。最高分保存在本機 PlayerPrefs。

街頭關的垃圾袋自轉並側向迴旋，預覽使用相同飛行模型。接觸物體後停止額外旋轉升力，由摩擦與重力處理桶蓋滑落／滾入。四台固定重用的車輛分兩個相反方向行駛。

風向使用帶局部霓虹明暗變化的箭頭，保留數字風速。背板關卡名稱／編號已移除；空心放射線只在有效命中時顯示，首頁與結算不顯示。

## 技術內容

- **投擲與預測**：共用飛行模型計算軌跡預覽與飛行受力，包含重力、風力，以及垃圾袋的旋轉升力。
- **計分與回合**：20 秒共用倒數、方向性開口判定、空心／碰撞獎勵、命中轉場與本機最高分。
- **Prefab 與資料設定**：場景、目標、投擲物、車輛及 UI 以 Prefab 保存；關卡和物理參數使用 ScriptableObject。
- **物件重用**：預先建立投擲物池；四台車輛循環移動，重玩時重設場景狀態。
- **視覺回饋**：URP 卡通材質、目標外輪廓、風向箭頭、UI 回饋與空心命中特效。
- **開發工具**：場景與資產工具、清除高分及 Play Mode 檢查入口。

## 專案結構

| 路徑 | 內容 |
|---|---|
| `Assets/TossRush/Runtime/` | 遊戲流程、投擲、計分、車流與 UI 程式 |
| `Assets/TossRush/Scenes/` | 原型場景 |
| `Assets/TossRush/Prefabs/` | 玩家視角、場景、目標、投擲物與 UI |
| `Assets/TossRush/Data/` | 關卡資料與投擲物物理參數 |
| `Assets/TossRush/Shaders/` | 專案建立的 Shader |
| `Assets/TossRush/ThirdParty/Kenney/` | Kenney 素材與各包原始授權 |
| `Assets/TossRush/Tests/` | Play Mode 測試 |
| `Assets/TossRush/Editor/` | Editor 工具與驗證入口 |
| `Docs/` | 玩法、素材來源、調整與驗證紀錄 |

## 調整與工具

- **TossRush → Clear High Score**：清除本機高分。
- **TossRush → Run Prototype Checks**：執行 24 項 Play Mode 測試，結果寫入 `Docs/Validation/StreetPlayMode.json`／`.xml`。退出 Play 後執行；編譯成功不代表測試已通過。
- **Starting Stage Index**：0 房間、1 街頭、2 籃球、3 移動洞口。
- `Assets/TossRush/Data/`：關卡分數、投速、上拋角度、位置／風向隨機範圍、投擲間隔。
- `Assets/TossRush/Data/Physics/TrashBag.asset`：垃圾袋質量、阻力、自轉、旋轉升力及存留時間。
- `Assets/TossRush/Prefabs/Targets/StreetTarget.prefab`：桶體、桶蓋碰撞面與矩形計分開口。
- `Assets/TossRush/Prefabs/Stages/StreetStage.prefab`：人行道、道路、車流及背景建築。
- `Assets/TossRush/Prefabs/Props/StreetCar.prefab`：共用車輛模型；場景實例設定方向與速度。
- `Assets/TossRush/Prefabs/UI/`：HUD、霓虹箭頭、全螢幕圖示與空心提示。
- `Assets/TossRush/Materials/TargetOutline.mat`：外框顏色及寬度，目前保留寬度 8。

**TossRush → Apply Street Stage Update** 會重新製作這輪專用資產並重設關卡順序，適合初始化，不是一般啟動按鈕；自行微調後不要隨意重按。

場景、車輛、目標、投擲物及 UI 都存為 Prefab；遊戲只預先建立投擲物池並重用，不逐項組裝場景或 UI。街頭版本有四種投擲物，預設最多 64 個池物件。

## 文件與驗證

- [街頭改造與目前驗證狀態](../Docs/StreetStageUpdate.md)
- [目前玩法與計分](../Docs/GameplayUpdate.md)
- [素材來源與授權](../Docs/AssetSources.md)
- [清理紀錄](../Docs/Cleanup.md)
- [Editor 歷次驗證](../Docs/Validation/EditorValidation.md)
- [街頭版本檢查紀錄](../Docs/Validation/StreetStageValidation.md)

已完成程式編譯、序列化資產引用檢查及飛行數學模型的 37 項檢查。實際桶蓋滾入、碰撞、UI 與完整流程，仍以最新 StreetPlayMode 報告和 Game View 試玩為準；舊版 18 項報告不代表街頭版本已通過。

截至 2026-11-12，街頭版本尚未取得完整的 24 項 Play Mode 通過報告。

## 開發方式

本專案採 AI 主導實作的協作方式：由本人決定玩法方向、進行試玩並提出修改回饋，AI 協助程式、Editor 工具、場景與 Prefab 製作、Shader、測試及文件整理。

外部家具、按鈕、字型與音效取自 Kenney；街景、車輛、垃圾桶、垃圾袋及其他基本幾何由專案內工具建立。未使用影像生成工具製作美術。完整來源與修改方式見 [素材來源](../Docs/AssetSources.md)。

## 授權與素材致謝

專案自行建立且有權授權的程式、文件與遊戲資產採用 [MIT License](../LICENSE)，允許使用、修改、散布與商用，散布時須保留 MIT 的著作權及授權聲明。

第三方內容排除於上述 MIT 授權範圍，保留原有條款：

| 內容 | 授權／條款 | 說明 |
|---|---|---|
| 專案自行建立的內容 | MIT | 不包含下列第三方內容與其衍生部分 |
| Kenney Furniture Kit、UI Pack、Impact Sounds | CC0 1.0 | 感謝 [Kenney](https://kenney.nl/)；保留各包原始 License.txt |
| Kenney Future 字型與其 TMP 字型圖集 | CC0 1.0（字型內容） | 字型來自 UI Pack；TMP 渲染程式仍依 Unity 授權 |
| `Assets/TextMesh Pro/` 的 Unity 提供內容 | Unity Companion License | 保留 Unity 授權聲明，使用時須符合其 Unity 相關條件 |
| Unity 引擎與 Package Manager 相依套件 | 各自原始條款 | 以各套件隨附 LICENSE／Third Party Notices 為準 |

完整說明見 [第三方授權聲明](../THIRD_PARTY_NOTICES.md) 與 [素材來源與修改紀錄](../Docs/AssetSources.md)。
