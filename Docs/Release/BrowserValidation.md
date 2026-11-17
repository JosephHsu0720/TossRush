# WebGL 瀏覽器驗證

日期：2026-11-16（台北）。瀏覽器：Windows 上既有 Google Chrome。測試透過本機 HTTP，包含直接頁面及 iframe 預覽；不以雙擊 HTML 或 Editor 畫面代替。

## 已觀察到的結果

| 檢查 | 結果與範圍 |
|---|---|
| 載入 | 成功載入 Gzip + fallback 的 Unity 網頁成品，首頁六情境文字、目標描邊及音量控制可見。最終 ZIP 見 LastBuild.json。 |
| 開始與時間 | LET'S TOSS 可進入回合，畫面顯示 13.8s → 2.1s，接著 TIME'S UP 結算。 |
| 真實投擲 | 使用瀏覽器滑鼠事件投擲，可見飛行紙團；結算顯示 1 THROWS。此輪未成功命中，不代表六種情境在瀏覽器全部通關。 |
| Esc | 釋放游標後顯示 CLICK TO CAPTURE MOUSE / TIMER CONTINUES，倒數持續。 |
| 重玩 | ONE MORE ROUND 可重新進入回合，計時／分數重設。 |
| 介面 | 最終 1280×720 iframe 預覽可完整顯示首頁與控制，沒有水平／垂直捲軸；原生全螢幕按鈕不與音量控制重疊。 |
| 全螢幕拒絕處理 | 原本由 Unity 延後請求時，測試環境出現 WrongDocumentError／not granted 並中斷；最終改由 HTML 點擊事件直接請求。瀏覽器拒絕時顯示「Fullscreen is unavailable here. You can keep playing in this window.」，沒有再出現 Unity 錯誤對話框。 |
| 偏好資料 | 最終成品音量滑桿可調整，重新整理後保留位置。音訊聽感仍應由本人確認。 |

## 不應擴大解讀的範圍

- **全螢幕成功進入與退出尚未取得證據。** 自動化期間瀏覽器曾最小化、未在前景或有本人輸入，請求被拒絕；已確認拒絕時的保護路徑，沒有把它列為成功進入全螢幕。
- **itch.io 版本尚不能測試。** 平台要求帳號 Email 驗證，遊戲 ZIP 上傳失敗；本人選擇保留草稿與本機成品。草稿目前顯示 No file provided to embed，屬於尚未上傳的狀態，不能宣稱已可在 itch.io 玩。
- Firefox 未安裝；Safari 不可用。曾嘗試啟動既有 Edge，但未取得可操作視窗，未列入已測瀏覽器。
- 尚未做手機／觸控、第二款瀏覽器、非零最高分重整保存、瀏覽器內六關完整命中與真人音訊聽感確認。
- Editor 的 [JamPlayMode.json](JamPlayMode.json) 為另外 12/12 的期限／重玩檢查，不能代替上述瀏覽器或人類試玩。
- 最終建置報告為 0 errors／0 warnings。Editor Console 另保留重新編譯連線期間兩筆 Pipeline ObjectDisposedException；它們屬 Editor 操作橋接，Pipeline 已明確從發行包排除，並非遊戲執行錯誤。沒有把整個 Editor Console 宣稱為零錯誤。

## 本人之後可以直接驗收

1. 開啟本機預覽或上傳後的 itch.io 頁面，讓瀏覽器保持前景，點擊開始遊玩。
2. 試投並觀察目標命中、切關、聲音和倒數；Esc 釋放滑鼠後期限仍繼續。
3. 點網頁右下角的全螢幕按鈕，再退出；若瀏覽器拒絕，仍可使用視窗遊玩。
4. 調整首頁音量後重新整理，並取得非零最高分後重新整理，核對偏好與記錄。
5. 加測 Firefox／Safari 或其他可用瀏覽器；如未驗證觸控，繼續維持不支援手機的標記。
