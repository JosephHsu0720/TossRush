# 多目標、碰撞分數與場景小物件

2026-11-12（台北），Unity 6000.3.11f1，套用至目前 TossRush.unity 所連結的 prefabs 與四關資料。

## 內容

- StreetStage 保留原桶，新增左右各一桶，共三個同型目標。中心相距 2.2 m，桶間約 0.68 m 空隙，三個開口分別有 ScoreGate 並綁定同一 StageView。隨機偏移時整組移動，重玩會重設所有桶口追蹤，任一桶命中仍只切關一次。
- 手持垃圾袋初始 Z=90° 側拿，繞世界 Y 軸水平自轉。出手初始角度亦為 Z=90°，保留既有水平角速度；重玩或重新進關都歸回起始角度。
- 依使用者後續確認，所有關卡每次新碰撞接觸即時 +30，包含目標桶與場景；進球不重複結算碰撞分，空心獎勵維持原設定。只有投擲物碰撞可觸發，持續貼住同一物體不累加，錯誤 token、回收、進球、切關中與回合截止後不給分。未進球仍保留已取得的碰撞分。
- 固定 24 格 TMP 浮字重用，從實際接觸點投影到現有 Canvas；位置隨相機重新投影、上浮並於 0.85 秒淡出。畫面外／背後不顯示、不攔截輸入，首頁、結算及重玩清除。
- 每關額外六個小物件，共 24 個。使用一般動態 Rigidbody 與實體 Collider，保留原有小物件；重玩恢復位置、旋轉與速度。數量目前為房間 17、街頭 6、球場 12、園遊會 12。
- Authoring 入口為 TossRush → Apply Three Targets And Collision Points；重複套用不再增生目標、小物件或浮字。

## 驗證

- Unity 編譯 0 errors、0 warnings。
- 最終 Play Mode 29/30 通過，完整結果見 [StageExpansionPlayMode.json](StageExpansionPlayMode.json)。
- 新功能涵蓋：三桶各自的實際 Rigidbody 入桶、桶間間隙拒絕算進球、袋子起始角度與世界水平旋轉、重玩角度重設、四關實際接觸立即 +30、浮字世界座標投影、80 次顯示仍維持固定池大小、淡出／背後隱藏、小物件物理移動及重玩姿態還原。
- 既有 Corner 滾入、空心／碰撞計分、桶蓋滾入、籃框方向、移動洞口、20 秒結算與重玩等通過。舊桶緣測試改用可重現的桶緣接觸位置／隨機種子，避開舊測試位置先碰其他場景物體的問題，並按新的即時計分規則驗證。
- 唯一失敗：RandomizedStages_CanBeScoredWithPlayerThrow_FromSeveralSeeds，在 seed 127 的 Street 關卡等候進球逾時。此自動瞄準／隨機投擲測試在本次修改前也曾失敗，先前對照見 [CornerScoringBaseline.json](CornerScoringBaseline.json)。此次沒有調整關卡投擲力道、風或自動瞄準策略來掩蓋該失敗；不能宣稱所有隨機投擲都通過。
- Game View 確認三桶、桶間空隙、側拿袋子、小物件與真實碰撞後的 +30。截圖見 [StageExpansionStreet.png](StageExpansionStreet.png)。截圖為檢查多次接觸的位置，僅在該次 Play session 延長浮字壽命並暫停畫面；prefab 仍為 0.85 秒。結束已離開 Play Mode 並還原播放速度。

未建置／發布遊戲；未替代人工操作難度與長時間遊玩驗收。
