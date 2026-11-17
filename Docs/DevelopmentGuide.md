# TossRush 開發指南

A 20-second first-person throwing game prototype made with Unity.

在 20 秒內，將紙團、垃圾袋、籃球、寶特瓶、報紙與飛鏢投向不同目標，挑戰更高分。每次命中後切換情境，利用飛行軌跡預覽、風向與場景碰撞調整投擲路線。

目前為開發中原型，已開始提供 WebGL 發布建置；成品、瀏覽器實測與 itch.io 私人草稿的最新狀態見 [發布檢查清單](Release/ReleaseChecklist.md)。Windows 發行版本尚未建置。下方歷次驗證紀錄中的發布狀態保留當時結果。

## 開啟專案

1. 安裝 Unity Hub 與 **Unity 6000.3.11f1**（與 ProjectSettings 及本次 WebGL 建置一致）。
2. Clone 或下載此 repository，在 Unity Hub 加入專案根目錄。
3. 使用指定版本開啟，等待 Unity 還原套件與匯入資產。
4. 依下方步驟開啟場景並試玩。

主要依賴：URP 17.3.0、Input System 1.20.0、uGUI／TextMeshPro 2.0.0，以及 Editor 操作用的 Pipeline。Test Framework 不再直接引用，但仍由 Pipeline／Collections 等套件間接依賴。版本以 [manifest.json](../Packages/manifest.json) 與 [packages-lock.json](../Packages/packages-lock.json) 為準，由 Unity Package Manager 還原。

目前以桌面滑鼠操作為主；行動裝置與瀏覽器相容性尚待驗證。

## 在 Editor 試玩

1. 開啟 `Assets/TossRush/Scenes/TossRush.unity`。
2. 按 **Play**，再按 **LET'S TOSS**。移動滑鼠瞄準，左鍵投擲；細線預覽飛行路徑。
3. **Esc** 釋放滑鼠，點擊場景重新捕捉；20 秒仍繼續倒數。
4. 右上角的向外／向內四角圖示切換畫面大小。Editor 切換 Game View 最大化／還原；執行檔分支切換全螢幕／視窗。遊玩中先按 Esc 再點擊。
5. 結算後按 **ONE MORE ROUND** 或 **R** 重玩。

固定測試單一關卡：在 Unity 上方選 **TossRush → 固定關卡測試 → 房間／街頭／籃球／園遊會（寶特瓶）／住家（信箱）／酒吧（飛鏢）**，再按 Play 和 LET'S TOSS。命中後會重新準備同一關，保留位置隨機變化；園遊會通過一次後，下次會加入上下移動。20 秒結束後按 R 可繼續測試，重新一回合會重置園遊會解鎖。遊玩中選另一關會立刻重新開始回合；選 **恢復正常輪替** 回到第一關與正常輪替。這個固定模式只在 Editor 生效。

| 順序 | 關卡 | 投擲物／目標 | 計分 |
|---|---|---|---|
| 1 | Corner Shots：房間角落 | 紙團／垃圾桶 | 基礎 100；無碰撞 +150 |
| 2 | Curbside Spin：街頭 | 黑色垃圾袋／開蓋輪式垃圾桶 | 基礎 200；無碰撞 +150 |
| 3 | Crosswind Court | 籃球／籃框 | 基礎 150；無碰撞 +150 |
| 4 | Moving Midway | 翻轉寶特瓶／左右移動洞口 | 基礎 200；無碰撞 +150 |
| 5 | Morning Delivery：住家門口 | 一疊報紙／信箱 | 沿瞄準方向平直出手；基礎 200；無碰撞 +150 |
| 6 | Bar / Darts：酒吧 | 飛鏢／標靶隨機區域 | 沿視線直射；當次黃色外框內基礎 100；無碰撞 +150 |

只有命中才切換，沒投進可繼續重投；六關共用 20 秒，轉場不暫停。最高分保存在本機 PlayerPrefs。

酒吧關卡接在住家後，命中當次目標後回到房間。每次進場、固定酒吧模式命中後，以及重玩新回合，均會重新隨機選取區域，首次也不固定紅心；相鄰兩次選取不重複。範圍包含紅心、中央綠環，以及 20 個數字扇區各自的內單倍、三倍、外單倍、雙倍區，共 82 區。黃色描邊、計分範圍與畫面提示使用同一份選區資料，只有該區域能得分；背面、牆面與其他區域均不計分。飛鏢維持 22 m/s 沿視線出手，受重力影響，無風。酒吧的攝影機、手持飛鏢與出手點一起向前移約 2.34 m，至標靶平面的投擲距離先由約 8.35 m 縮為 6.68 m，再縮短目前距離的 10% 至約 6.01 m（原本的 72%，累計縮短 28%）；離開酒吧時還原。漏投飛鏢停留在撞擊表面，轉場或重玩時回收；此關不提供反彈分。更新入口為 **TossRush → Apply Random Dartboard Targets**；完整建立入口為 **TossRush → Add Bar Darts Stage**，驗證結果見 [酒吧飛鏢檢查](Validation/DartsStage.md)。

每次碰撞後的法線向外相對速度須嚴格超過門檻（街頭 0.1 m/s、其他關卡 1.5 m/s），才為該次投擲暫存 30 分（浮字顯示 PENDING）；門檻可在 StageData.minimumReboundSpeed 調整。命中時連同基礎分一次結算，落空不加分。任何碰撞仍取消無碰撞獎勵。街頭每次砸到行人立即扣 10 分，行人不提供正向反彈分。

房間的書本、燈具、書櫃與邊桌，球場的架上籃球、球架、記分板與訓練器材，園遊會的攤位、玩偶與疊罐，住家的鞋子、鞋架、盆栽與包裹均可被推動或擊落。地面、房屋與主要進球目標維持固定支撐。街頭仍保留 10 位會干擾投擲的行人，不增加額外雜物；三桶改為黑、藍、綠色，參考 [Barnet Council 的一般垃圾、回收與園藝垃圾桶配色](https://www.barnet.gov.uk/recycling-and-waste/household-recycling-and-waste/what-put-your-bins)。配色只供辨識，三桶皆可得分。

園遊會兩側共 3 隻玩偶、6 個疊罐，每個被玩家或連帶碰撞擊倒後立即 +10 分，無須投進目標，落空也保留；每次場景重設前只算一次。獎品接觸不暫存 +30 碰撞分，其他物體的碰撞規則照常。未受玩家影響的自然沉降不加分。

第五關報紙以 34 m/s 沿視線出手，不額外上拋；左右及上下瞄準都改變出手方向，之後仍受重力下墜。兩片窗戶被擊中後會連框脫落，重新開始或進場時復位。

書架放在左側空處，與原有桌椅保持間隔；每本書按實際高度放在層板上，避免開場碰撞重疊而散落。信箱的計分範圍與實際側壁、下方門板及上方門楣對齊，並追蹤沿入口邊緣滑入的報紙；反向進入或直接出現在箱內不算命中。

信箱對斜向報紙使用實際穿過洞口的箱形截面判定，並在物理步驟之間插補旋轉。擦邊旋轉後已完全進入箱內、向下落的報紙，會沿用這次正面入箱記錄計分；不要求整疊報紙的平面投影同時留在狹窄洞口內。僅伸入一角、外側擦撞、反向穿出或重用投擲物的舊記錄均不加分。擦邊碰撞仍取消無碰撞獎勵。

住家場景採明亮的夏日庭園氛圍：漸層藍天、暖色日光、草坪、玫瑰／薰衣草花圃、綠籬、成熟樹群、石牆與遠處石造住宅。庭園造景不新增碰撞，保留左右信箱投遞路線與原本可擊落物件。每關的天空、主光源及攝影機遠距離會在切離時還原，再交由下一關套用自己的環境；住家重建入口為 **TossRush → Apply European Garden Atmosphere**。

其餘四關各有獨立環境：房間使用暖色牆面、木地板、閱讀窗景、地毯和吊燈；街頭追加商店樓層、條紋遮雨棚、屋頂設備與遠處天際線，前景仍保留原本行人和三個垃圾桶；球場改用戶外社區球場場線、圍網、看台、照明架與公園背景；園遊會使用傍晚天空、彩旗燈串、遠處帳篷與摩天輪。新增造景沒有碰撞體，原有場景邊界則以對應圍網呈現。切關先停用前一關再啟用新關，讓各關天空與日光正確交接。重建入口為 **TossRush → Apply All Stage Theme Scenery**。

信箱每次進場會在左側或右側鋪石區隨機出現（x 為 -2.75～-2.25 或 2.25～2.75 m），避開正門與中央走道，前後距離固定 3.4 m。盆栽與鞋架移至兩側，留出投遞路線。街頭與籃框每次進場分別在左右 ±0.55／±0.7 m 內變化，固定前後距離並整組移動，保留桶蓋／桶口及籃板／籃框的相對打板角度。園遊會先只有左右移動，通過園遊會後的下一次進場才加入上下移動，並將兩軸移動速度提高 25%；之後不再累加加速。其他關卡成功不解鎖，重玩新回合重置。加速倍率可透過 StageData.motionSpeedAfterFirstClear 調整，目標位置預測共用同一倍率。

街頭關的垃圾袋自轉並側向迴旋，預覽使用相同飛行模型。接觸物體後停止額外旋轉升力，由摩擦與重力處理桶蓋滑落／滾入。四台固定重用的車輛分兩個相反方向行駛。

垃圾袋碰撞時壓扁、側向鼓起並縮小碰撞體，離開表面後緩慢回復；向外回彈保留 55% 並限制為 0.22 m/s，桶蓋彈性降為 0.06。這是吸能與變形的柔軟近似；重玩／回收會還原形狀，不影響共用網格。

風向使用帶局部霓虹明暗變化的箭頭，保留數字風速。背板關卡名稱／編號已移除；空心放射線只在有效命中時顯示，首頁與結算不顯示。

有風時，畫面周邊會出現沿風向流動的藍色短曲線；風越強，線條更密、流動更快且更明顯。風線依照攝影機方向與螢幕比例投影，保留中央瞄準區，不影響碰撞與投擲力。無風（含住家）、選單、結算和轉場時隱藏。12 條 LineRenderer 預先保存在 PlayerView prefab，遊玩中重用；重建入口為 **TossRush → Apply Wind Lines And Carnival Speed**。

街頭風線使用兩倍寬度，提升在行人與建築背景前的辨識度；其他關卡保持原本粗細。各關可透過 StageData.windLineWidthMultiplier 調整純視覺寬度，不改變實際風力。

## 技術內容

- **投擲與預測**：共用飛行模型計算軌跡預覽與飛行受力，包含重力、風力，以及垃圾袋的旋轉升力。
- **計分與回合**：20 秒共用倒數、方向性開口判定、空心／碰撞獎勵、命中轉場與本機最高分。
- **Prefab 與資料設定**：場景、目標、投擲物、車輛及 UI 以 Prefab 保存；關卡和物理參數使用 ScriptableObject。
- **物件重用**：預先建立投擲物池；四台車輛循環移動，重玩時重設場景狀態。
- **視覺回饋**：URP 卡通材質、目標外輪廓、風向箭頭、UI 回饋與空心命中特效。
- **開發工具**：場景與資產工具、清除高分及 Play Mode 檢查入口。

## 專案結構

| 路徑 | 內容 |
|---|---|
| `Assets/TossRush/Runtime/` | 遊戲流程、投擲、計分、車流與 UI 程式 |
| `Assets/TossRush/Scenes/` | 原型場景 |
| `Assets/TossRush/Prefabs/` | 玩家視角、場景、目標、投擲物與 UI |
| `Assets/TossRush/Data/` | 關卡資料與投擲物物理參數 |
| `Assets/TossRush/Shaders/` | 專案建立的 Shader |
| `Assets/TossRush/ThirdParty/Kenney/` | Kenney 素材與各包原始授權 |
| `Assets/TossRush/Editor/` | Editor 工具與驗證入口 |
| `Docs/` | 玩法、素材來源、調整與驗證紀錄 |

## 調整與工具

### 音樂與音效

投擲使用 Woven SFX 的 **fast-whoosh**（約 0.395 秒，CC0）。碰撞（Impact）、扣分（Penalty）與獎品（Reward）維持停用，對應未用音效已移除。BGM、開始、命中與結算提示保留。

首頁、遊玩、轉場與結算共用同一個循環 BGM：Juhani Junkala／SubspaceAudio 的 **Level 1**（CC0）。音樂和音效使用獨立 AudioSource；開始新回合與切關不重新啟動 BGM。

首頁右下角直接顯示喇叭圖示與整體音量 slider；拖曳立即調整 BGM 和音效，點喇叭可靜音／恢復先前音量，設定保存在本機並在重新啟動後套用。控制只出現在首頁，與全螢幕按鈕並列。

場景中 GameDirector 的 `GameAudio` 可調整 `Music Volume`（預設 0.12）及 `Effects Volume`（預設 0.8）。音效播放時 BGM 短暫降至 0.035，再自動恢復。開始／命中／結算分別使用 Interface Sounds 的 open／confirmation／close。

**TossRush → Apply CC0 Game Audio** 可重新套用引用，重複執行不會增加音樂來源。Editor 若聽不到，先檢查 Game 視窗的 Mute Audio；此開關不代表遊戲未播放音樂。所有新增音源均允許免費非商用與商用，來源見 [授權檢查](Validation/AssetLicenseAudit.md)。

用途音效替換與 soft 音效被音樂蓋過的調整，已通過 2／2 音訊檢查與實際輸出量測，見 [音效調整紀錄](Validation/AudioRefinement.md)。

### 開發工具

- **TossRush → Clear High Score**：清除本機高分。
- 專案自動測試、舊驗證／預覽腳本已移除，保留 **TossRush → 固定關卡測試**。`Docs/Validation/` 內既有報告屬歷史紀錄，不代表目前仍提供相同測試入口。
- **TossRush → Apply Neighborhood And Rebound Update**：保存五關的門檻計分、人群、物件與信箱／報紙資產；重複執行不增加重複人群或物件。
- **TossRush → Update Scene Props And Sidewalk Crowd**：只更新各場景的主題擺設與人行道行人動線，移除先前的共用雜物。
- **TossRush → Apply Mailbox Bottle And Street Scoring**：保存歐式小型信箱、34 m/s 平拋報紙、柔軟垃圾袋、翻轉寶特瓶與街頭扣分／低反彈門檻。
- **TossRush → Repair Bin And Hoop Entry Detection**：套用街頭三桶 0.65 m 與籃框 0.6 m 的擦邊滾入追蹤；判定使用實際碰撞體中心與壓縮後半徑。
- **TossRush → Apply Target Variation And Progression**：保存左側信箱配置、受限的左右變化，以及園遊會通關後的上下移動。
- **Starting Stage Index**：0 房間、1 街頭、2 籃球、3 移動洞口、4 住家門口。
- `Assets/TossRush/Data/`：關卡分數、投速、上拋角度、位置／風向隨機範圍、投擲間隔。
- `Assets/TossRush/Data/Physics/TrashBag.asset`：垃圾袋質量、阻力、自轉、旋轉升力及存留時間。
- `Assets/TossRush/Prefabs/Targets/StreetTarget.prefab`：桶體、桶蓋碰撞面與矩形計分開口。
- `Assets/TossRush/Prefabs/Stages/StreetStage.prefab`：人行道、道路、車流及背景建築。
- `Assets/TossRush/Prefabs/Props/StreetCar.prefab`：共用車輛模型；場景實例設定方向與速度。
- `Assets/TossRush/Prefabs/UI/`：HUD、霓虹箭頭、全螢幕圖示與空心提示。
- `Assets/TossRush/Materials/TargetOutline.mat`：外框顏色及寬度，目前保留寬度 8。

**TossRush → Apply Street Stage Update** 會重新製作這輪專用資產並重設關卡順序，適合初始化，不是一般啟動按鈕；自行微調後不要隨意重按。

場景、車輛、目標、投擲物及 UI 都存為 Prefab；遊戲只預先建立投擲物池並重用，不逐項組裝場景或 UI。目前五種投擲物，預設最多 80 個池物件。

## 文件與驗證

2026-11-16 已完成五關各 20 次的投擲矩陣，100／100 案例符合檢查預期；其中 75 次從玩家位置發射、25 次為控制入口／時間的物理邊界案例。另擷取五關一般與極端視角共 10 張畫面，確認 CC0 BGM 實際有音訊輸出，並通過音樂跨轉場／重玩不中斷的檢查。完整 163 項為 162 通過、1 未通過：既有隨機街頭自動瞄準落在桶外，尚未修正該測試的路線選擇。詳見 [100 次測試報告](Validation/HundredThrowPlaytest.md) 與 [素材授權檢查](Validation/AssetLicenseAudit.md)。

- [街頭改造與目前驗證狀態](../Docs/StreetStageUpdate.md)
- [目前玩法與計分](../Docs/GameplayUpdate.md)
- [素材來源與授權](../Docs/AssetSources.md)
- [清理紀錄](../Docs/Cleanup.md)
- [Editor 歷次驗證](../Docs/Validation/EditorValidation.md)
- [街頭版本檢查紀錄](../Docs/Validation/StreetStageValidation.md)

已完成程式編譯、序列化資產引用檢查及飛行數學模型的 37 項檢查。實際桶蓋滾入、碰撞、UI 與完整流程，仍以最新 StreetPlayMode 報告和 Game View 試玩為準；舊版 18 項報告不代表街頭版本已通過。

最新目標變化與難度進度的 Play Mode 為 48/49 通過，見 [TargetProgressionPlayMode.json](Validation/TargetProgressionPlayMode.json)。左右極值的打板／借蓋進球、側邊信箱正常投遞、園遊會通過一次後的下一次進場解鎖上下移動與重玩重置，以及原有漏判修正均通過；剩餘為實際落在桶外的自動街頭投擲。尚未建置或發布。

## 開發方式

本專案採 AI 主導實作的協作方式：由本人決定玩法方向、進行試玩並提出修改回饋，AI 協助程式、Editor 工具、場景與 Prefab 製作、Shader、測試及文件整理。

外部家具、按鈕、字型與音效取自 Kenney；街景、車輛、垃圾桶、垃圾袋及其他基本幾何由專案內工具建立。未使用影像生成工具製作美術。完整來源與修改方式見 [素材來源](../Docs/AssetSources.md)。

## 授權與素材致謝

專案自行建立且有權授權的程式、文件與遊戲資產採用 [MIT License](../LICENSE)，允許使用、修改、散布與商用，散布時須保留 MIT 的著作權及授權聲明。

第三方內容排除於上述 MIT 授權範圍，保留原有條款：

| 內容 | 授權／條款 | 說明 |
|---|---|---|
| 專案自行建立的內容 | MIT | 不包含下列第三方內容與其衍生部分 |
| Kenney Furniture Kit、UI Pack、Impact Sounds | CC0 1.0 | 感謝 [Kenney](https://kenney.nl/)；保留各包原始 License.txt |
| Kenney Future 字型與其 TMP 字型圖集 | CC0 1.0（字型內容） | 字型來自 UI Pack；TMP 渲染程式仍依 Unity 授權 |
| `Assets/TextMesh Pro/` 的 Unity 提供內容 | Unity Companion License | 保留 Unity 授權聲明，使用時須符合其 Unity 相關條件 |
| Unity 引擎與 Package Manager 相依套件 | 各自原始條款 | 以各套件隨附 LICENSE／Third Party Notices 為準 |

完整說明見 [第三方授權聲明](../THIRD_PARTY_NOTICES.md) 與 [素材來源與修改紀錄](../Docs/AssetSources.md)。
