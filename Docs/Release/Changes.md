# 本次發布準備變更

日期：2026-11-16（台北）。範圍：TossRush 的 WebGL／itch.io 參賽準備。既有第六關飛鏢、關卡素材與玩法調整不是本次新增功能。

## 遊戲與發布流程

| 變更 | 目的 |
|---|---|
| GameDirector 的回合期限改用 realtimeSinceStartupAsDouble | 卡頓、時間比例為零或分頁背景停更後，不會取得額外投擲／計分時間；回到更新時直接結算。 |
| Web 平台停用 Unity 內的全螢幕按鈕，改由網頁範本的原生按鈕處理 | 在真正的使用者點擊事件內請求全螢幕；被瀏覽器拒絕時顯示提示，不讓錯誤中斷遊戲。Editor 與桌面版本的原有路徑保留。 |
| 新增 TossRush WebGL 範本 | 自適應畫面、載入進度、失敗說明、可鍵盤操作的全螢幕按鈕、較保守的高 DPI 渲染。 |
| 啟用網頁偏好資料自動同步 | 採用 Unity 的自動 persistentDataPath 同步模式，避免手動同步的棄用訊息。 |
| 新增 WebReleaseBuild 與 JamSceneBuildCheck | 提供選單建置；檢查 20 秒、首關、正常輪替；建立獨立時間戳輸出、複製通知、封裝並檢查 itch.io 限制。 |
| 保存 Web Player 設定 | 1280×720、Gzip + fallback、128 MB 初始記憶體、資料快取、Minimal stripping；設定實際透過 Unity API 保存。 |
| 更新 README 與開發指南 | Unity 版本統一為實際 ProjectSettings／建置使用的 6000.3.11f1，加入發布清單入口，區別歷次 Editor 與本次 Web 驗證。 |

Unity 在本次 Web 建置時，另自動更新 URP 預設 Volume、著色器預篩選與 Graphics／URP 全域設定的序列化資料。這些是建置時產生的設定更新。工作目錄原有的 IDE、套件鎖定檔與動態字型等變更未當成本次新功能，也沒有覆蓋或提交其他工作的變更。

## 新增交付資料

- [ReleaseChecklist.md](ReleaseChecklist.md)：逐項狀態、證據與後續接續步驟。
- [ItchPage.md](ItchPage.md)：雙語介紹、欄位設定、操作、素材致謝與 AI 揭露。
- [HostQuestion.md](HostQuestion.md)：詢問主辦 AI 使用資格的英文草稿；未傳送。
- [LastBuild.json](LastBuild.json)：最新 ZIP、網頁資料夾、版本、大小與建置結果。
- [ZipAudit.json](ZipAudit.json)：ZIP 的 SHA-256、檔案數、解壓大小、最大檔案、路徑長度與通知清單；63 個檔案，未包含 C# 原始碼、Library 或 Temp。
- [JamPlayMode.json](JamPlayMode.json)：12/12 發布玩法檢查；包含真實 20 秒回合、暫停／停更截止、轉場及重玩重設。
- [BrowserValidation.md](BrowserValidation.md)：瀏覽器實測與未驗證範圍。
- [Media/](Media/)：630×500 封面、六關 1280×720 截圖及結算畫面；封面現已依本人要求改為以 Gameplay 房間截圖為參考的影像生成版本，搭配 TOSSRUSH 標題。舊版程式繪製封面保存在 Cover-Abstract-630x500.png；製作紀錄見 CoverGameplay.md。
- `Tools/serve-web.mjs`：僅綁定 127.0.0.1 的本機預覽，含 iframe 預覽頁。
- `Tools/ValidateJamRelease.cs`：可重複執行的回合檢查。
- `Tools/CreateReleaseCover.ps1`：從選定的 Gameplay 封面主圖匯出 630×500 版本。

## itch.io 上的變更與保留狀態

- 建立 [TossRush 私人草稿](https://redjojo235.itch.io/tossrush)，[編輯頁](https://itch.io/game/edit/5380150)。
- HTML 遊戲、免費且不收款、960×540 嵌入、點擊後載入、啟用 itch.io 全螢幕、未宣稱手機支援。
- 已保存雙語介紹、操作說明、致謝、封面與五張截圖，並重新開頁核對介紹保存成功。
- 已揭露 AI：Graphics、Text & Dialog、Code，說明人類負責方向／試玩與 AI 主導大量實作。
- **遊戲 ZIP 沒有上傳成功**：平台要求先驗證帳號 Email；本人決定先保留草稿與成品。
- 沒有公開發布、加入 Jam、提交 Jam 或發送訊息給主辦。

## 驗證界線

後續新增：依本人要求製作 Edit Theme 的 Banner、Background、Embed BG 三張場景圖片，分別匯出 960×300 PNG、1920×1080 JPEG、960×540 JPEG；這些是影像生成的宣傳場景重製圖。檔案、尺寸依據、提示詞與上傳建議見 [ThemeImages.md](ThemeImages.md)，本次未套用至網站。

本機瀏覽器測試不代表 itch.io iframe 已通過。未完成 Firefox／Safari／手機與真人最終聽感及體驗確認；未取得主辦 AI 參賽資格回覆。舊的 Editor 自動投擲矩陣曾有一條街頭自動瞄準路線未通過，本次沒有將其改標為通過；詳見既有 HundredThrowPlaytest.md。
