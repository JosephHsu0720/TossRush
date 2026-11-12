# 拋物線避讓版 HUD

新的排版已準備在 `TossRush → Apply HUD Layout Update`，程式編譯通過。Editor 通訊端目前仍受環境權限限制，是否已套用請以 `Validation/HudLayoutApply.json` 為準；Game View 尚待實際檢查。

## 排版

以 1280×720 為參考解析度，將原本橫跨上方的 TopBar 改成左下資訊卡。保留元件引用與物件名稱，沒有在執行期建立 UI。

| 區域 | 內容 | 參考範圍（自畫面左下計算） |
|---|---|---|
| 左下 | 關卡名稱、霓虹風向箭頭、風速、倒數與時間條 | x 24–344，y 100–250 |
| 右下 | 高分與目前分數 | 靠右 24，底部 100，260×104 |
| 底部 | 操作提示 | 左 24、右 88，底部 24，44 高 |
| 右下角 | 全螢幕／還原圖示 | 靠右 24，底部 24，44×44 |
| 下方中央 | 得分與空心提示 | 分別位於 y 86／176 |

中央與上方不再放置橫跨畫面的資訊底板或倒數，讓高拋與旋轉飛行的路線可見。側卡、提示與圖示使用螢幕邊緣錨點，配合既有 CanvasScaler。

## 套用與調整

1. 退出 Play Mode。
2. 點選 **TossRush → Apply HUD Layout Update**。
3. 進入 Play，確認四關的高拋路線、右下分數與全螢幕按鈕。

工具只更新 `Assets/TossRush/Prefabs/UI/GameHUD.prefab`，不重新製作關卡。之後可直接編輯該 Prefab 的 TopBar（目前為左下卡）、ScorePanel、BottomBar 與 FullscreenButton。

街頭資產重套工具也已同步採用這個排版，避免未來重套街頭關卡時回復上方資訊帶。
