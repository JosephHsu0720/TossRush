# 街頭版本檢查紀錄

日期：2026-11-12（台北）。

## 已確認

- 套用報告與場景序列化資料順序一致：Corner、Street、Basketball、Carnival。
- Street 200 分、空心／碰撞額外分數為 0；手持索引 3，PlayerView 有四個手持物引用。
- 四台車分兩個相反方向，速度 4.2／3.6。桶蓋有碰撞面，矩形開口半寬／半深為 0.62／0.52。
- 檢查場景、StreetStage、StreetTarget、TrashBag、PlayerView、GameHUD、WindIndicator 的 GUID 引用，均可解析至 Assets 或已安裝套件。
- HUD 有 WindIndicator；箭頭有三個核心線段、三個光暈線段與風速文字引用。
- CleanShotFeedback 的 Rays 預設 inactive，首頁／結算程式亦停用特效根物件。
- 已用 Unity 6000.3.11f1 的編譯器與組件確認：Editor runtime、非 Editor runtime、資產製作程式、24 項測試程式及檢查選單可編譯。Unity 序列化欄位的 CS0649 依 Unity 賦值情況處理。
- 共用 ProjectileFlight.cs 的數學檢查 37 項通過，包含 32 組極端位置／風速／風向；最差正規化開口偏差 0.729，小於可通過上限 1。

## 補齊事項

- 更新仍描述 Paper Trail／舊順序的 README 與玩法文件。
- 加入 24 項 Play Mode 檢查選單，支援 domain reload 後重新掛接、儲存 JSON／XML 報告。
- 回呼清理延至下一個 Editor update，避免派送中修改回呼集合；重用並清理本工具的 TestRunnerApi 實例。
- 可攜式數學檢查產物導向 Temp，補上 csproj 的 gitignore 例外。

## 尚未確認

目前執行環境仍拒絕 Unity Editor 通訊端存取（10013），重啟 server 後相同。因此這不是 Play Mode 通過報告。

尚待 **TossRush → Run Prototype Checks** 產生 StreetPlayMode.json／.xml，確認實際 Rigidbody 預測誤差、桶蓋滾入、車流與完整回合；另需 Game View 試玩確認構圖、旋轉可讀性、描邊及箭頭視覺。

舊 PlayModeResults.xml 的 18 項與 OutlineResults.xml 的單項通過均早於街頭改造，不能代表目前 24 項已通過。

## 重跑數學檢查

```powershell
dotnet run --project Docs/Validation/StreetFlightMath/StreetFlightMath.csproj --configuration Release
```

此入口直接編譯專案飛行模型，以數學替身提供 Vector3／重力依賴；不驗證 Unity 碰撞、滾動、介面或車流。
