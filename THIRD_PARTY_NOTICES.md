# Third-party notices / 第三方授權聲明

本專案根目錄的 [MIT License](LICENSE) 僅適用於專案自行建立且有權授權的內容。第三方素材、程式、字型與其衍生部分保留原始授權，不因收錄在本 repository 而改為 MIT。此聲明與下列原始授權應隨相關內容一併保留。

## Kenney 素材：CC0 1.0 Universal

作者／提供者：[Kenney](https://kenney.nl/)。以下素材包隨附的 License.txt 均指定 Creative Commons Zero（CC0 1.0）：

| 素材包 | 專案使用的檔案 | 原始授權 |
|---|---|---|
| [Furniture Kit](https://kenney.nl/assets/furniture-kit)，包內版本 2.0 | bench、bookcaseOpen、books、chairDesk、desk、lampRoundFloor、pottedPlant 的 FBX | [License.txt](Assets/TossRush/ThirdParty/Kenney/furniture-kit/License.txt) |
| [UI Pack](https://kenney.nl/assets/ui-pack)，2.0 | button_rectangle_depth_flat.png、Kenney Future.ttf | [License.txt](Assets/TossRush/ThirdParty/Kenney/ui-pack/License.txt) |
| [Interface Sounds](https://kenney.nl/assets/interface-sounds)，1.0 | open_001.ogg、confirmation_001.ogg、close_001.ogg | [License.txt](Assets/TossRush/ThirdParty/Kenney/interface-sounds/License.txt) |

Kenney 字型衍生的 `Assets/TossRush/Fonts/KenneyFuture.asset` 中，字形／圖集內容亦按 CC0 1.0 提供；這不包含 Unity 的 TMP 程式或 Shader。修改用途與來源詳見 [AssetSources.md](Docs/AssetSources.md)。

CC0 允許複製、修改、散布及商用，無須強制署名；本專案自願保留來源與致謝。CC0 不授予他人的商標權，也不代表原作者為此遊戲背書。

- [CC0 1.0 說明](https://creativecommons.org/publicdomain/zero/1.0/)
- [CC0 1.0 法律文本](https://creativecommons.org/publicdomain/zero/1.0/legalcode)
- [CC0 1.0 法律文本離線副本](Docs/Licenses/CC0-1.0.txt)
- [Kenney 官方使用說明](https://kenney.nl/support)

## Juhani Junkala／SubspaceAudio 音樂：CC0 1.0

`Assets/TossRush/ThirdParty/SubspaceAudio/Juhani Junkala [Retro Game Music Pack] Level 1.wav` 來自作者發布的 [5 Chiptunes (Action)](https://opengameart.org/content/5-chiptunes-action)，使用作者提供 ZIP 中的原始檔；取得日期為 2026-11-16。

作者隨包的 [INFO.txt](Assets/TossRush/ThirdParty/SubspaceAudio/INFO.txt) 明確聲明這些曲目以 CC0 釋出。適用 [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)，允許免費非商用及商用、修改與散布，不要求強制署名；本專案自願保留作者與來源。此授權不延伸到作者其他付費音樂包。

## Woven SFX fast-whoosh：CC0 1.0

投擲音效 `Assets/TossRush/ThirdParty/WovenSFX/fast-whoosh.wav` 來自 [Woven SFX whoosh 素材頁](https://www.woven.video/sfx/whoosh-sound-effect)，該頁明確以 CC0 1.0 提供，允許非商用及商用。2026-11-16 下載原始 WAV，未修改；保留 [來源與授權紀錄](Assets/TossRush/ThirdParty/WovenSFX/License.txt) 及上述 CC0 法律文本。先前未使用的 Kenney Impact Sounds 與 pluck 檔案已移除。

## Unity uGUI／TextMeshPro

`Assets/TextMesh Pro/` 內保留的必要資源，包括 TMP Shader、include 檔、換行資料及預設設定，來自 Unity `com.unity.ugui` 2.0.0 所附的 TMP Essential Resources；這些內容不屬於本專案的 MIT 授權。

目前安裝的 `com.unity.ugui` 2.0.0 指定 **Unity Companion License for Unity-dependent projects**。原始套件授權聲明的副本保存在 [Docs/Licenses/Unity-uGUI-LICENSE.md](Docs/Licenses/Unity-uGUI-LICENSE.md)，完整條款見 [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license)。使用、修改或散布相關內容時，須遵守其與有效 Unity 引擎授權相關的使用條件並保留通知。

範例 Liberation Sans 字型與 Emoji 素材未包含於本專案；遊戲字型使用上述 Kenney Future。

## Unity 引擎與套件相依性

Unity 引擎不包含於此 repository。`Packages/manifest.json` 與 `Packages/packages-lock.json` 記錄的套件由 Unity Package Manager 還原，不受本專案 MIT 授權覆蓋。

主要直接依賴包括 URP 17.3.0、Input System 1.20.0、uGUI 2.0.0、Pipeline 1.0.0-pre.1，以及 Unity 內建必要模組。Visual Studio Editor 已移除；Test Framework 不再直接引用，但仍由其他套件間接依賴。各套件與其內含第三方內容，均以該版本所附的 LICENSE／Third Party Notices 為準。

發行遊戲版本時，需同時保留實際隨版本散布之內容所要求的授權及第三方通知。
