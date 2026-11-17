# TossRush：20 Second Game Jam 2026 發布檢查

更新：2026-11-16（台北）。本次目標為發布準備；本人選擇先保留私人草稿與本機成品，不繼續帳號驗證／正式上架／Jam 提交。

## 逐項狀態

| 項目 | 狀態 | 處理與證據 |
|---|---|---|
| 六種情境共用 ≤20 秒 | 已通過 | 場景預設 20 秒、首關開始；改用實際經過時間，轉場不重設期限。`JamPlayMode.json` 12/12 通過。 |
| 每局獨立 | 已通過 | 重玩清除分數、投擲數、命中數與六關解鎖進度；只保留最高分及音量偏好。 |
| 發布設定 | 已處理 | 1280×720、Gzip + 解壓後備、128 MB 初始記憶體、資料快取、Minimal managed stripping、無開發版符號。 |
| WebGL 成品與 ZIP | 已建置，0 錯誤／0 警告 | 最終 ZIP 19,701,754 bytes（約 19.7 MB）；63 個檔案，解壓約 20.3 MB；根目錄有 index.html；詳見 LastBuild.json、ZipAudit.json。 |
| 授權與致謝 | 已整理 | ZIP 內包含專案及第三方聲明、CC0 原文、素材原始授權、各已註冊套件提供的授權通知；未新增外部美術或音訊。 |
| Chrome 載入、開始、投擲、結算、重玩、Esc | 已實測 | 本機 WebGL 成品可載入；實際投擲後結算顯示 1 THROWS；釋放滑鼠後倒數繼續。 |
| 瀏覽器全螢幕 | 拒絕時的保護已驗證；成功進出待本人驗收 | 改由網頁按鈕在使用者點擊事件直接請求；拒絕時顯示提示，不中斷 Unity；詳見 BrowserValidation.md。 |
| 其他瀏覽器／手機 | 尚未完成 | 本機未安裝 Firefox；Safari 不可用。未宣稱手機支援，itch.io Mobile friendly 關閉。 |
| 封面與截圖 | 已完成、已上傳草稿 | 630×500 封面；六關各 1280×720 實景截圖，草稿選用房間、球場、園遊會、庭園、飛鏢五張。 |
| 雙語介紹與操作說明 | 已保存、已重新開頁核對 | [私人草稿](https://redjojo235.itch.io/tossrush)，[編輯頁](https://itch.io/game/edit/5380150)；內容見 ItchPage.md。 |
| AI 使用揭露 | 已保存 | Yes；Graphics、Text & Dialog、Code；Sounds 未勾選；敘明 AI 主導大量實作與人類方向／試玩。 |
| 遊戲 ZIP 上傳 | 帳號驗證未完成，依本人選擇暫停此步驟 | itch.io 回覆「Please verify your email address before uploading a file」。遊戲檔未上傳成功，草稿目前不能直接玩。 |
| Public 與 Jam 提交 | 未執行 | 草稿保留 Draft，尚未加入 Jam／提交作品。須先完成 Email 驗證及取得 AI 資格確認。 |
| 人類最終試玩／內容標記 | 待本人確認 | 酒吧場景、滑稽物理碰撞、命中特效與音訊聽感，應由本人試玩後填 Audience & Content。 |

## 本機成品與重建

最新資料見 [LastBuild.json](LastBuild.json)：`output` 是可服務的網頁資料夾；`archive` 是要上傳的 ZIP。`Builds/`、`Artifacts/` 不進 Git，請另外保留 ZIP。

Unity 選單：

1. `TossRush → Release → Apply Web Settings`：保存 Web 發布設定。
2. `TossRush → Release → Build itch.io ZIP`：建立獨立時間戳資料夾、複製通知、封裝、檢查平台限制並更新 LastBuild.json。請先退出 Play Mode，處理未保存場景。

本機預覽：在專案根目錄執行 `node Tools/serve-web.mjs`，開啟 `http://127.0.0.1:8765/preview`。只綁定本機；不可雙擊 index.html 以 file:// 方式測試。若該連接埠已有預覽，可指定 `node Tools/serve-web.mjs <output> 8767`。

重新執行倒數檢查：開啟正常場景、進入 Play Mode，再透過 Unity 的 `script run` 執行 `Tools/ValidateJamRelease.cs` 的 `ValidateJamRelease.Begin`。檢查會恢復最高分、時間比例及測試設定；結果寫入 JamPlayMode.json。此檢查的控制命中僅驗證切關與期限，不代表人類完整瞄準試玩。

## 之後接續的順序

1. 本人在 itch.io 完成 RedJOJO 帳號的 Email 驗證。
2. 使用 [HostQuestion.md](HostQuestion.md) 詢問主辦 AI 分工是否被接受；本次未替本人發送訊息。
3. 到草稿編輯頁上傳 LastBuild.json 指向的 ZIP，勾選「This file will be played in the browser」。
4. 保存後在 itch.io 的 iframe 實際測試載入、滑鼠、倒數、音訊、全螢幕、重玩與偏好保存；本機通過不代表 itch.io 已通過。
5. 完成本人最終試玩及內容標記，再設為 Public。
6. 若主辦確認資格，到 Jam 加入並 Submit your project；確認參賽列表出現 TossRush。單純發布遊戲頁不等於已提交 Jam。

## 規則來源

- [Jam 規則與 FAQ](https://itch.io/jam/20-second-game-jam-2026)：免費瀏覽器版本需託管 itch.io；每局 ≤20 秒；主題可選；AI 使用要揭露。AI 可否參賽的最終判斷交由主辦。
- [itch.io HTML5 上傳文件](https://itch.io/docs/creators/html5)：ZIP 包含 index.html；預設解壓後 ≤500 MB、單檔 ≤200 MB、≤1000 個檔案、路徑 ≤240 字元。
- [itch.io 遊戲頁文件](https://itch.io/docs/creators/getting-started)：封面建議 630×500，截圖建議 3–5 張。

活動已公告延長一週；請以活動頁首倒數確認最後時間，並預留上傳與修正時間。
