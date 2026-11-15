# 素材授權檢查（2026-11-16）

清理更新：下表保留初次稽核證據；目前未使用的 Impact Sounds 六個音效及 pluck 已移除，新增 Woven SFX 的 CC0 fast-whoosh 作投擲音。最新實際檔案與雜湊以 [AssetLicenseInventory.csv](AssetLicenseInventory.csv) 為準，來源與授權見 [WovenSFX/License.txt](../../Assets/TossRush/ThirdParty/WovenSFX/License.txt) 及 [官方素材頁](https://www.woven.video/sfx/whoosh-sound-effect)。

檢查目前工作目錄的外部模型、圖片、字型和聲音，對照隨附授權、既有來源紀錄及官方素材頁。逐檔 SHA-256 和大小見 [AssetLicenseInventory.csv](AssetLicenseInventory.csv)。這是可追溯的來源／授權檢查，並非對所有潛在第三方權利的法律保證。

| 內容 | 檢查證據 | 結論 |
|---|---|---|
| 七個 Kenney Furniture Kit FBX | 包內 License.txt、[官方頁](https://kenney.nl/assets/furniture-kit) | CC0；目前模型使用方式符合授權 |
| Kenney Future.ttf、按鈕 PNG、其 TMP 圖集 | UI Pack License.txt、[官方頁](https://kenney.nl/assets/ui-pack) | CC0；圖集保留字型來源，TMP 程式另依 Unity 條款 |
| 六個 Kenney Impact Sounds OGG | 原始包 License.txt、[官方頁](https://kenney.nl/assets/impact-sounds)；新增四個由官方下載介面取得 | CC0；投擲、命中、碰撞、扣分、開始和結算皆可使用 |
| 四個 Kenney Interface Sounds OGG | 原始包 License.txt、[官方頁](https://kenney.nl/assets/interface-sounds)；檔案雜湊與官方下載包一致 | CC0；取代鈴聲作為開始、命中、擊倒與結算提示。三個舊 impactBell 保留但已不引用 |
| Level 1 WAV | 作者 ZIP 的 INFO.txt、[作者發布頁](https://opengameart.org/comment/97266) | Juhani Junkala／SubspaceAudio 明確釋出 CC0；僅匯入這一首與 INFO.txt |
| TMP 必要資源 | Docs/Licenses/Unity-uGUI-LICENSE.md 與目前 PackageCache uGUI LICENSE.md 一致 | Unity Companion License；保留原授權，限符合其 Unity 相關條件的用途 |
| 引擎與 UPM 相依套件 | manifest、lock 與目前套件內 LICENSE／Third Party Notices | 不屬專案 MIT／CC0；發行仍須隨實際散布內容保留通知 |
| 程式建立的關卡幾何、人物、車輛及材質 | 既有 AssetSources.md 與專案 Editor 建立工具 | 未發現新增來源不明的外部二進位美術；來源判斷依專案紀錄，無法獨立證明所有歷史創作過程 |

[Kenney 官方 FAQ](https://kenney.nl/support) 確認資產頁內容允許個人、教育及商用，不強制署名；禁止以其 logo 暗示官方作品。本專案保留來源致謝，未匯入 Kenney logo。

CC0 允許免費非商用，也允許商用、修改和散布；它與「只能非商用」不同。依 [CC0 官方說明](https://creativecommons.org/publicdomain/zero/1.0/)，商標、專利、肖像等其他權利不因 CC0 自動移轉，也不得暗示作者背書。

未新增照片、商標、外部人物或生成圖片。既有垃圾桶參考照片依專案記錄未匯入遊戲。尚未建置發行包，因此此次結論只涵蓋目前專案素材與引用，未驗證發行包內通知的完整性。
