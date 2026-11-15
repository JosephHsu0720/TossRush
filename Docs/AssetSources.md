# 素材來源與授權

專案自行建立且有權授權的內容採用根目錄的 [MIT License](../LICENSE)。下列第三方內容及其衍生部分保留原始授權，不在專案 MIT 授權範圍內；完整聲明見 [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md)。

取得日期：2026-11-11。只匯入下列實際使用的檔案，未匯入整包預覽圖、替代格式或其餘素材。

## 既有外部素材

| 素材 | 原始來源 | 作者／授權 | 專案內用途與修改 |
|---|---|---|---|
| Furniture Kit | https://kenney.nl/assets/furniture-kit | Kenney；CC0 1.0；包內 License 標為 2.0 | desk、chairDesk、bookcaseOpen、books、pottedPlant、lampRoundFloor、bench 共七個 FBX。調整比例與位置，改用本專案卡通材質統一配色；未匯入原始色盤貼圖。 |
| UI Pack 2.0：按鈕 | https://kenney.nl/assets/ui-pack | Kenney；CC0 1.0 | `PNG/Yellow/Default/button_rectangle_depth_flat.png`，設定為 Sprite／九宮格切片；用於開始、重玩按鈕，搭配 UI Feedback Shader。 |
| UI Pack 2.0：Kenney Future 字型 | https://kenney.nl/assets/ui-pack | Kenney；包內 CC0 1.0 授權 | `Font/Kenney Future.ttf`；製作動態 TMP SDF 字型，供 UI 與舞台標題共用。 |
| Interface Sounds 1.0 | https://kenney.nl/assets/interface-sounds | Kenney；CC0 1.0 | `open_001.ogg` 開始、`confirmation_001.ogg` 命中、`close_001.ogg` 結算。保留原始 License.txt。先前的 impactBell、impactSoft 與 pluck 未使用素材已於清理時移除。 |
| fast-whoosh | https://www.woven.video/sfx/whoosh-sound-effect | Woven SFX；CC0 1.0 | 2026-11-16 下載原始 44.1 kHz、16-bit 立體聲 WAV，長約 0.395 秒，作為出手音效；未裁切原檔。來源／授權保存於 ThirdParty/WovenSFX/License.txt。 |
| 5 Chiptunes (Action)：Level 1 | https://opengameart.org/content/5-chiptunes-action | Juhani Junkala／SubspaceAudio；CC0 1.0 | 2026-11-16 從作者提供的 ZIP 匯入原始 WAV，不裁切；作為全流程循環 BGM。保留作者 `INFO.txt`；Unity 匯入使用 Vorbis 壓縮，不改寫原檔。 |

Kenney 三份原始 `License.txt` 保存在對應資料夾；Woven SFX 的來源與授權紀錄亦一併保留。CC0 說明：https://creativecommons.org/publicdomain/zero/1.0/ 。Kenney 官方使用說明：https://kenney.nl/support 。

## 自行建立的原型內容

- 桶體、籃框／籃網、移動洞口、舞台結構、紙團／球／沙包的基本幾何：在 Editor 以 Unity primitives 與簡單環形 Mesh 組合並保存為資產。
- Stage Toon、UI Feedback Shader、遊戲程式與 UI 排版：本次開發建立。
- 未使用影像生成工具製作美術，未使用外部人物、商標或其他貼圖。外部音樂僅使用上表明列的 CC0 曲目。

## 引擎與第三方程式

Unity、URP、Input System、uGUI／TextMeshPro、Unity Test Framework 與 Pipeline 等套件，仍遵循各自套件內的 LICENSE／Third Party Notices。它們不是 CC0，不在上述素材授權範圍內。

TextMeshPro 的必要設定、Shader 及字形渲染程式有保留；範例 Liberation Sans 字型及 Emoji 素材已移除。所有遊戲文字明確指定本專案的 Kenney Future 字型。


本次四關更新未新增外部素材：角落牆面、借彈板、斜板、小物 Prefab、目標 Outline Shader 與空心 UI 放射線特效皆使用本專案基本幾何／程式；家具仍沿用上述 Kenney CC0 七個模型。

外輪廓缺線修正：Target Outline／Target Silhouette Mask Shader、輪廓遮罩與擴張 Mesh、TargetBlue 材質皆由本專案建立，沒有新增外部素材。

全螢幕／還原圖示：由本專案以 uGUI Image 線段組成並儲存於 FullscreenButton.prefab，未使用外部圖示素材。

首頁音量喇叭圖示由本專案以 uGUI 向量網格繪製，滑桿由 uGUI 元件組成，未新增外部圖片或圖示字型。

街頭垃圾袋改造：人行道、道路、建築、車輛、輪式開蓋垃圾桶與黑色垃圾袋均由本專案以基本幾何／自製 Mesh 建立，沒有新增外部美術包、貼圖或套件。使用者的垃圾桶照片僅作外形參考，照片本身及商標／文字未匯入遊戲。霓虹風向箭頭由 Prefab 內的 uGUI Image 線段組成。
