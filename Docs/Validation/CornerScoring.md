# CornerStage 滾入計分修正

> 此為追加全關卡即時碰撞 +30 前的驗證快照；最新規則與測試見 [StageExpansion.md](StageExpansion.md)。

2026-11-12（台北），Unity 6000.3.11f1。

## 原因與修改

原本只在紙團中心由上往下穿過桶口平面的瞬間檢查完整半徑是否位於開口內。實際 Rigidbody 模擬重現紙團先在邊界穿越、再向內滾落，已進入桶內但沒有再次判定的情況。

ScoreGate 新增預設為 0 的 rollInDepth，僅 CornerStage prefab 設為 0.3 m。從開口範圍穿過平面、但完整紙團尚未進入的投擲物，會在這段深度內繼續追蹤；向下移動且完整進入開口才得分。離開開口範圍、回到平面上方、超出深度、回收投擲物或重設關卡都會清除追蹤。其他關卡仍使用原有平面判定。

保留既有基礎分、空心加分、其他物件碰撞加分、命中切關及一次計分流程。另修正測試設定中指向已不存在的 TossRushPrototype.unity 路徑，改為目前的 TossRush.unity。

## 驗證

- Unity 編譯：0 errors、0 warnings。
- Play Mode 專項：4/4 通過，包括三條桶緣滾入軌跡各得 100 分且不重複計分、向上／桶外／桶口下方拒絕計分、空心 150 分、兩次其他物件碰撞 160 分與碰撞歷史重設。
- 完整 Play Mode：24/26 通過。籃框方向、移動洞口、街頭桶蓋滾入、20 秒結算、重玩、投擲池等測試通過。結果見 [CornerScoringPlayMode.json](CornerScoringPlayMode.json)。
- 兩項失敗：RandomizedStages_CanBeScoredWithPlayerThrow_FromSeveralSeeds（等候得分逾時）、TargetRimContact_CancelsCleanButDoesNotEarnPropBonus（預期其他物件碰撞 0，實際 1）。
- 使用精確 prefab 備份與 finally 還原，暫時把 rollInDepth 設為 0 進行對照；上述兩項在原有平面判定下仍同樣失敗，未擴大修改處理。結果見 [CornerScoringBaseline.json](CornerScoringBaseline.json)。操作後已確認 CornerStage rollInDepth 回復 0.3。

以上為自動 Play Mode 與實際 Rigidbody 驗證，未進行人工操作手感／Game View 視覺驗收，亦未建置或發布遊戲。
