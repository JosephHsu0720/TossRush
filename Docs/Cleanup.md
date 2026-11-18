# 專案清理紀錄

## 2026-11-19：移除已完成的固定關卡驗證腳本

`StageToolsValidation.cs` 與其 `.meta` 已移除；它僅用於驗證前次整合，不是固定關卡測試工具的執行相依。保留 `Docs/Validation/StageTools.json` 作為歷史驗證結果。

## 2026-11-18：測試工具整合與一次性 Editor 清理

- 「固定關卡測試」移入 **TossRush → 測試工具**，提供六個關卡選擇、「固定此關卡」及「恢復正常輪替」。遊玩中切換會重新開始一般模式，包含從特殊模式切回測試關卡。
- 移除圖片中的一次性 Add／Apply／Repair／Refresh／Update 及 Open Prototype 選單。刪除 15 份僅供一次性製作使用的 Editor 腳本與各自 `.meta`，含只被這些工具使用的 DartboardRegionAuthoring 輔助類別。
- 遊戲場景、Prefab、材質、網格、音效與關卡資料保持原樣；保留測試工具、紀錄操作、驗證程式及可重複使用的 Release 建置工具。
- 程式編譯通過，場景與 36 份 Prefab 沒有遺失腳本引用。固定關卡檢查 16／16、紀錄工具 9／9 通過，共 25 項；驗證後還原原有紀錄與測試設定。結果見 `Docs/Validation/StageTools.json`、`TestTools.json`，畫面見 `TestTools.png`。

日期：2026-11-11。

## 已移除

| 類別 | 項目 | 原因 |
|---|---|---|
| URP 範本教學 | TutorialInfo、Readme.asset | 原型不需要起始教學及其 Editor 腳本、圖示。 |
| 範例場景 | Assets/Scenes/SampleScene.unity | 入口已改為 TossRushPrototype。 |
| 預設輸入設定 | InputSystem_Actions.inputactions | 遊戲直接讀取 Mouse／Keyboard；UI 使用 InputSystemUIInputModule。已清除 Project Wide Actions 的設定引用。 |
| 未用渲染設定 | Mobile_RPAsset、Mobile_Renderer、SampleSceneProfile | 所有品質層級統一使用 PC URP 設定；已清除範例 Volume 引用。 |
| TMP 範例素材 | Liberation Sans 字型／範例材質、EmojiOne 圖與 Sprite Asset | 改用 Kenney Future；已更新 TMP 預設字型並移除 Emoji 引用。 |
| 未用套件 | AI Navigation、Unity Version Control（collab-proxy）、Rider 整合、Multiplayer Center、Timeline、Visual Scripting | 本版無導航、多人、時間軸或視覺化腳本需求；用 Package Manager API 移除。 |
| 素材包內容 | 未選取的模型、貼圖、預覽圖、其他格式、其他 UI／音效 | 僅選擇性匯入 14 個檔案（含三份授權），約 304 KiB。 |

刪除已匯入資產前，檢查原型場景、Graphics／Quality、URP 全域設定與 TMP Settings 的依賴。套件相依性由 Unity Package Manager 解算。

## 保留的必要項目

- URP 與其 PC Renderer、全域設定及仍被引用的預設 Volume Profile。
- Input System、uGUI／TMP、Physics、Audio、Test Framework 及 Pipeline。
- Visual Studio 整合，方便維護 C# 專案。
- TMP 共用 Shader 與必要設定／斷行資料。這些是文字渲染基礎，不屬於遊戲美術包。
- Unity 內建模組與 URP／Pipeline 的相依套件；不以刪除底層模組或生成快取來冒險縮減專案。

Library、Temp、Logs 是 Unity 快取／測試中間產物，不作為遊戲素材交付，也不納入版本控制。未建立遊戲版本、執行檔或 WebGL 輸出。


## 四關更新追加清理

- 移除已完成資產製作用途的 `PrototypeAuthoring.cs` 場景產生器，保留精簡的 Open Prototype／Clear High Score Editor 選單。
- 移除 16 個未引用的 TMP Shader／Shader Graph 變體及 White 材質，保留目前字型使用的 Mobile SDF、必要 fallback 與共用 include。
- 從 manifest 移除 adaptiveperformance、ai、androidjni、cloth、director、terrain、terrainphysics、tilemap、unityanalytics、vehicles、video、vr、wind、xr 共 14 個內建模組的直接引用；必要相依由 Package Manager 自行保留。
- 描邊保留一個不可見遮罩與一個輪廓 Renderer；改用預存的 16 方向擴張 Mesh，避免法線造成缺線。兩個垃圾桶共用資產，移除 CornerTarget 重複的遮罩／輪廓物件。
- 輪廓 Mesh 只儲存位置與方向 UV，焊接重複頂點；圓環燈泡用低面數遮罩，輪廓頂點從 106,656 減為 14,496，原本可見模型不變。新增 TargetBlue 小型材質，沒有新增套件、貼圖或執行期產生網格。
- 第四關重用紙團物件池，不新增投擲物種類或池容量。新增可互動小物也共用四個 Prefab。

## 街頭改造

套用報告確認已移除未引用的 RoomStage.prefab 與 Room.asset。RoomStageTarget.prefab 仍有相依資產，保留；紙團與房間材質仍供 Corner 使用。新增垃圾袋、輪式開蓋垃圾桶、車輛、街頭舞台與箭頭 Prefab，未新增貼圖或套件。車輛烘成共用多材質 Mesh，四台實例循環重用；場景與 UI 不在執行期組裝。投擲物增加至四種，預設池上限 64。

數學測試產物不加入版本控制或原始碼壓縮檔。
