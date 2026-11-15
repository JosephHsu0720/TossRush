# TossRush 100 次投擲與音訊驗證（2026-11-16）

最後一輪結果：**100／100 投擲案例符合檢查預期**，五關各 20／20。實際 36 次命中、64 次落空；通過表示判定與狀態符合案例要求，並非每次都投進。完整 163 項檢查為 **162 通過、1 未通過**（無略過），約 385.65 秒；100 次矩陣之外的流程／音訊檢查為 62／63。

唯一未通過項目為既有 `RandomizedStages_CanBeScoredWithPlayerThrow_FromSeveralSeeds`：seed 11 的街頭投擲在六次碰撞後落於桶外，未達自動瞄準要求的三秒內命中。保留失敗，未修改遊戲規則去配合測試。音樂跨命中、轉場與重玩持續播放、不增加重複 AudioSource 的新增回歸檢查已通過。

證據：[100 次摘要](HundredThrowSummary.json)、[逐次 CSV](HundredThrowSweep.csv)、[完整結果](ComprehensivePlayMode.json)、[完整 NUnit XML](ComprehensivePlayMode.xml)。

Unity 6000.3.24f1；場景 `Assets/TossRush/Scenes/TossRush.unity`；實際 Editor Play Mode、Rigidbody、碰撞與 ScoreGate 判定。未建置或發布遊戲。

## 投擲範圍

五關各 20 次，共 100 次；原始逐筆資料為 [HundredThrowSweep.csv](HundredThrowSweep.csv)。每一筆只接受一次投擲；75 個玩家投擲案例另檢查同幀再次投擲必須被間隔限制拒絕。

| 每關案例 | 次數 | 驗證方式 |
|---|---:|---|
| 正常玩家瞄準與不同隨機配置 | 4 | 從玩家手上呼叫正式 TryThrow；除街頭會受行人／桶蓋阻擋外，要求實際命中 |
| 左右與上下極限角度 | 8 | yaw ±38°、pitch -25°／30°，及單軸極值；允許自然落空，驗證有限座標、正向尺寸、最多一次命中 |
| 目標附近的偏角投擲 | 3 | 從正常瞄準左右偏 2°、上下偏 1.2°，觀察擦邊與落空；得分須符合接觸／空心規則 |
| 正向入口、高速入口 | 2 | 在入口前控制初始位置，以 8／34 m/s 進入；要求命中一次 |
| 反向穿出、遠離所有開口 | 2 | 反向只觀察初次穿出，避免把隨後合法回落當成誤判；外側案例避開街頭三個桶，要求不命中 |
| 倒數截止、結果與重玩 | 1 | 先投出再讓短測試回合逾時；要求停止投擲、顯示結果、回收物件、重玩歸零 |

其中 75 次從玩家位置經正式投擲控制器發射，25 次為控制初始位置／時間的物理邊界案例，不能將後者視為人工操作可達的路線。所有案例都使用真實物理，未直接呼叫 RegisterHit 製造命中。

每次記錄關卡、方式、yaw／pitch、預期／實際命中、碰撞次數、暫存分、總分、發射位置／速度與最後取樣位置。`expectedHits=-1` 表示不規定要投進或投失，仍檢查重複計分、分數公式、有限座標和音樂來源狀態；不能將這些案例解讀為每條飛行路線都有精確命中預測。

## 第一輪警示覆核

第一輪矩陣為 96 通過、4 警示，原始檔保存在 [HundredThrowSweep.Initial.csv](HundredThrowSweep.Initial.csv)。三個反向案例在向上穿出之後，於重力作用下重新從上方合法落入，因此得分正確；另一個「中央桶外側」案例剛好投進旁桶，亦屬有效進球。修正的是測試觀察時間與外側位置，未修改或放寬遊戲的進球判定。

第一輪全部 162 項檢查為 157 通過、5 未通過，其中四項為上述測試設計問題；另有一項既有隨機街頭自動瞄準未達到預期命中，完整證據保存在 [ComprehensivePlayMode.Initial.json](ComprehensivePlayMode.Initial.json)。該投擲最後位於桶外（五次碰撞），現有證據支持自然落空，未發現分數漏算；此自動瞄準流程仍需另外改善，不能宣稱所有既有測試均已通過。

## 畫面與音訊

- 修正首頁 `FOUR STAGES` 為 `FIVE STAGES`，並更新中英文 README 的關卡、投擲物、場景與 Unity 版本。
- 另外以正式 TryThrow 發射 10 次，擷取五關的一般／極端視角；為避免桌面滑鼠干擾，擷取期間停用滑鼠更新，固定合法瞄準角度並使用同一軌跡預覽方法。[對照圖](VisualSweep-ContactSheet.png) 左側為一般、右側為極端；每一列依序為房間、街頭、球場、園遊會、住家。
- 已逐張檢查，未見粉紅遺失材質、明顯破圖、HUD 擋住中央瞄準區；街頭極端視角碰到行人後顯示 -10，符合規則。目標在極端視角離開畫面屬正常瞄準結果。
- BGM 使用 Juhani Junkala／SubspaceAudio 的 Level 1（74.254 秒），獨立 AudioSource、2D、音量 0.18、循環。投擲、命中、新增碰撞、扣分、開始／結算與擊倒回饋沿用／擴充 Kenney CC0 音效。
- [VisualSweep.json](VisualSweep.json) 記錄十次切關／重新開始時同一音樂來源與持續增加的播放樣本位置；[AudioOutput.json](AudioOutput.json) 記錄一般 Play 模式實際非零音訊輸出。
- 音訊輸出第一次取樣需要暖機；初次零值未當成已播放的證據。Editor 原本的 Mute Audio 設定會於驗證後還原，試聽時需解除 Game 視窗靜音。

## 授權與限制

見 [素材授權檢查](AssetLicenseAudit.md)、[逐檔清單](AssetLicenseInventory.csv) 與 [音效來源完整性](AudioSourceIntegrity.json)。六個 Kenney OGG 均與官方下載包的 SHA-256 相同。音樂為作者包中的原始 WAV，作者 INFO.txt 與完整 CC0 法律文本均已保留。

此次沒有測試 Windows／WebGL 發行包、不同顯示比例、觸控操作或人工聽覺對所有音效的主觀混音評估。畫面是 1920×1080 樣本；100 次矩陣是自動化投擲與狀態檢查，不是逐次人工觀看完整影片，也無法排除所有幀間顯示問題。
