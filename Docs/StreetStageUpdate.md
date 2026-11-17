# 街頭垃圾袋改造

## 2026-11-17：三色分類

- 三個垃圾桶由左至右為紅、綠、藍，桶身與桶蓋使用對應顏色。
- 每次進入街頭關卡與成功擲出後，隨機準備下一個顏色的垃圾袋；手持預覽與該次實際擲出的袋子一致，飛行途中不換色。
- 從上方進入同色桶：原有基本 200 分、乾淨命中 +150 分與有效反彈獎勵照常計算，再進入下一關。
- 進入異色桶：固定扣 50 分，不增加命中次數、不領取該袋的命中或暫存反彈加分，也不換關；同一袋只結算一次，總分可為負數。
- 投空、由下往上經過開口、只碰桶身或桶蓋，不會觸發顏色進桶結算。原有行人扣分仍獨立生效。
- 配色資產已存入 Prefab 與材質。需要重新套用本次設定時，使用 `TossRush → Apply Street Colour Matching`。
- 驗證報告：`Validation/StreetColorPlayMode.json`；在 Play Mode 呼叫 `TossRush.Editor.StreetColorValidation.Begin()` 可重跑。
- Unity 編譯成功；68 項檢查通過，涵蓋九種袋／桶配色、手持與飛行材質、異色單次扣分及排除暫存反彈加分、投空／向上經過開口、高速入桶與其他五關共用計分。實際畫面：`Validation/StreetColorMatching.png`。

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
