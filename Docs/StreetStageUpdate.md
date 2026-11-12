# 街頭垃圾袋改造

`Validation/StreetStageApply.json` 記錄套用時間為 2026-11-12 15:26:45（台北）。目前順序：房間角落 → 街頭垃圾袋 → 籃球 → 移動圓環。

- 人行道、雙向道路、背景建築與四台重用車輛已存為 Prefab。
- 輪式矩形垃圾桶具有空心桶體、桶蓋碰撞面、輪子、把手，參考使用者照片的外形自行製作。
- 垃圾袋手持時自轉、飛行時迴旋：質量 0.8 kg，初始垂直角速度 12 rad/s，旋轉升力係數 0.025。
- 撞擊後停止額外迴旋力；必須真正從上往下進入矩形開口才得 200 分，觸碰桶蓋不加分。
- 背板世界文字移除、風向改霓虹箭頭、首頁與結算隱藏空心放射線。
- 舊 RoomStage.prefab 與 Room.asset 確認未被其他資產引用後已移除；RoomStageTarget.prefab 仍有相依性，保留。

## 編輯

一般調整請編輯資料與 Prefab。`TossRush → Apply Street Stage Update` 會重新製作這輪專用資產、重設順序，適合初始化；自行微調後不要隨意重按。

房間調色工具已改為只處理仍存在的房間資產，避免舊第一關刪除後報錯。

## 驗證

退出 Play，點選 **TossRush → Run Prototype Checks**。使用既有 Unity Test Framework 執行 24 項 Play Mode 測試，報告寫入 `Validation/StreetPlayMode.json`／`.xml`，不建立遊戲版本。

涵蓋原有流程、四種投擲物、隨機位置、旋轉預測、桶蓋實際接觸後滾入、矩形開口方向、雙向車流／重置、箭頭與首頁特效。高分由測試保存並還原。

已完成與尚待確認的範圍，見 [StreetStageValidation.md](Validation/StreetStageValidation.md)。
