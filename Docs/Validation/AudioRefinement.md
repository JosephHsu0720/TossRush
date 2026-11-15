# 音效用途與音量調整（2026-11-16）

針對 impactBell 不符合事件用途，以及 impactSoft_medium 被 BGM 蓋過的回饋，調整目前遊戲場景並保存引用。

| 用途 | 原音效 | 新音效 |
|---|---|---|
| 開始回合 | impactBell_heavy_001 | Interface Sounds / open_001 |
| 投進目標 | impactBell_heavy_000 | Interface Sounds / confirmation_001 |
| 擊倒園遊會獎品 | 與投進共用 impactBell_heavy_000 | Interface Sounds / pluck_001 |
| 結算 | impactBell_heavy_002 | Interface Sounds / close_001 |

四個新 OGG 與原始 License.txt 放在 `Assets/TossRush/ThirdParty/Kenney/interface-sounds/`。來源為 [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds)，CC0，允許非商用及商用；原檔未裁切或重製，雜湊已與官方 ZIP 核對一致。舊鈴聲保留，場景與執行程式已無引用。

投擲與碰撞繼續使用 impactSoft_medium_000／001。投擲播放增益由 0.25 改為 0.8，碰撞的播放增益由約 0.083～0.358 改為 0.4～0.8；BGM 平常音量由 0.18 降至 0.12。所有事件經同一音效播放入口處理，播放時 BGM 暫時降至 0.035，保留至少 0.16 秒，依音效長度最多 0.55 秒，再於約 0.25 秒內恢復。僅調音量，音樂樣本位置持續前進，不重啟歌曲。

驗證：

- Unity 編譯成功，Console 無錯誤。
- 兩項音訊 Play Mode 檢查 2／2 通過：檔案用途與場景引用、投擲／碰撞的音樂避讓及恢復、命中轉場與重玩持續播放、不增加音樂來源。見 [AudioRefinementPlayMode.json](AudioRefinementPlayMode.json)。
- 一般 Play 模式透過正式 TryThrow 觸發投擲，量測實際來源輸出。投擲短窗峰值 RMS 約 0.347，同期間 BGM 短窗峰值 RMS 約 0.063；音樂音量確實從 0.12 降至 0.035 後恢復 0.12，播放樣本持續前進。見 [AudioBalanceProbe.json](AudioBalanceProbe.json)。此為單次輸出量測，不代表每個音樂段落或裝置的主觀聽感都相同。
- 音效原始聲音資料可讀、非靜音，長度約 0.10～0.29 秒；見 [AudioClipLevels.json](AudioClipLevels.json)。

本次只重跑受音訊調整影響的檢查，先前的 100 次物理投擲報告為當時版本的記錄。未修改計分、投擲速度或碰撞規則。
