# Editor 驗證結果

2026-11-11，Unity 6000.3.11f1 / URP 17.3.0。

**18 通過、0 失敗**，完整 Play Mode 測試約 57.35 秒。原始報告：`PlayModeResults.xml`。

- 完整 20 秒未命中也不跳關；期限停止投擲／計分，Restart 正確重設。
- 紙團、側風籃球、移動洞口、個人房間角落可正常命中與切換。
- 原三關無碰撞投進 +100；角落無碰撞 +50、兩次其他物件碰撞 +60。
- 桶邊碰撞取消空心、不增加其他物件次數；物件池重用清除碰撞歷史。
- 真實 Rigidbody 接觸能推動小物，重玩恢復原位與速度。
- 目標描邊存在且無碰撞體，資訊位於頂部，中央資訊板／舞台進度條已移除。
- 空心特效在轉場遮罩上方顯示。
- HIGH SCORE／NEW HIGH SCORE、紀錄保存及 Editor 清除工具通過；測試後恢復原高分。
- 四組固定隨機種子，共 16 次從玩家投擲點的完整投擲；目標／風向範圍合法，共用資產未被修改。
- 三種物品的重力、側風反應、飛行預測、彈性差異及 48 個共用投擲物上限通過。

碰撞獎勵測試先觀察真實 OnCollisionEnter，再將同一投擲物移至投入口，以隔離驗證接觸歷史與計分；這不代替人類玩家對完整借彈路線、難度和趣味性的試玩。隨機關卡投擲測試則使用正常固定投速、玩家可達瞄準範圍及完整物理飛行。

尚未執行 WebGL／執行檔建置或 itch.io 發布。

## 外輪廓缺線與配色修正驗證

日期：2026-11-12（台北）。

- 四關 Game View 檢查通過：垃圾桶上下與左右緣連續、籃板／籃網整體外形完整、移動洞口外緣連續。桶身接縫、洞口內緣與籃網內部不描黃線；被場景物件遮住的部分仍接受深度遮蔽。
- 原有模型與碰撞體不變。補入實際可見零件外形，桶口／洞口與籃網的不可見封閉遮罩只影響描邊。畫面空間 16 方向擴張取代頂點法線擴張，輪廓深度向鏡頭偏移 0.035 世界單位，減少接地邊緣被地板切掉。
- 籃板與圓環換藍色材質、標記／網線／燈泡用薄荷綠，與金黃色描邊區隔。
- 寬度 0／6 以 MaterialPropertyBlock 實際預覽，分別隱藏／加粗；退出 Play 後保留共用材質目前的寬度 8（使用者調整值），未將其重設。
- 兩個 Shader 均無編譯錯誤；Editor Console 0 errors、0 warnings。確認 frameCount 持續變化後才檢查畫面。
- 重跑 TargetsHaveOutline_AndHUDLeavesCenterClear：1 通過、0 失敗（約 0.66 秒），報告 OutlineResults.xml。上方 18 項為前一輪玩法驗證，本輪未重跑整組。
- 測試後已退出 Play Mode，正常起始關卡維持 0；未建置遊戲版本。

## 全螢幕按鈕驗證

日期：2026-11-12（台北）。

- C# 重新編譯 0 errors、0 warnings。開始與遊玩畫面的按鈕可見，與分數／HIGH SCORE 不重疊；按鈕是 HUD 根節點的共用巢狀 Prefab，結算時保持顯示。
- EventSystem 與 GraphicRaycaster 有效；按鈕存在一個 Prefab 持久化 ToggleFullscreen 回呼。
- 以 Input System 虛擬滑鼠實際進行 UI raycast、按下與放開：Game View 成功最大化；Cursor 維持釋放狀態、Throws 保持 0。第二次觸發成功還原，標籤回復 MAXIMIZE。測試後移除虛擬滑鼠並還原原視窗狀態。
- Game View frameCount 持續更新，Editor Console 0 errors、0 warnings。退出 Play 後起始關卡仍為 0。
- 此次執行按鈕互動驗證，未重跑前述玩法測試。未建立遊戲版本；執行檔的 Screen.fullScreenMode 切換分支待實機版本驗證。

### 全螢幕圖示更新

- 移除文字 Label，改為 Prefab 內的 EnterFullscreenIcon／ExitFullscreenIcon，由每個狀態八個不接收 raycast 的 Image 線段組成；未增加貼圖、字型或外部素材。按鈕尺寸 44×44，分數區跟著調整預留空間。
- 重新編譯 0 errors／0 warnings。以實際 Input System 滑鼠事件確認點擊成功、兩個圖示依最大化狀態正確互斥顯示、點擊不捕捉游標也不投擲；分數／高分 RectTransform 與按鈕無重疊。
- 已檢視遊玩畫面的展開圖示及結算畫面的還原圖示。測試後還原視窗並退出 Play Mode；沒有新增遊戲版本建置。

## Room palette update — pending application

RoomPaletteUpdate.cs compiled against Unity 6000.3.11f1 and the current project assemblies. The current environment denied Editor socket access (10013); no live asset application or visual validation has occurred. See ../RoomPalettePlan.md. Run TossRush > Apply Room Contrast Palette outside Play Mode to apply the prepared colors. Completion is recorded by RoomPaletteApply.json.


## 街頭版本狀態

街頭資產已套用，順序、資料與 GUID 引用均已核對。Runtime、Editor 製作程式、24 項測試程式及檢查選單均可編譯；飛行數學模型 37 項檢查通過。

目前仍有 Editor 通訊端權限限制（10013），尚未取得新版本 Play Mode 報告。上方歷次 18 項／單項通過不能代表街頭版已通過。請以 TossRush → Run Prototype Checks 產生 StreetPlayMode.json／.xml，完整範圍見 StreetStageValidation.md。

## HUD 拋物線避讓排版

已準備 HudLayoutUpdate，Editor 製作程式編譯通過。選單為 TossRush → Apply HUD Layout Update，將關卡／風向／倒數移到左下、分數移到右下，圖示與提示移到頁尾，清空上方高拋區域；街頭重套工具已同步新排版。

套用狀態以 HudLayoutApply.json 為準，尚需 Game View 視覺確認。此輪沒有建置版本。詳細配置見 ../HudLayoutUpdate.md。
