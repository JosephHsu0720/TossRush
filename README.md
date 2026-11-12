# TossRush

[繁體中文](README_tw.md) | [English](README_en.md)

A 20-second first-person throwing game made with Unity.

Toss paper balls, trash bags, basketballs, and beanbags into different targets to chase a high score in just 20 seconds! Each successful throw takes you to the next scene. Use the trajectory preview, account for the wind, and bounce shots off the scenery to find your aim.

## Features

- Four settings: a room corner, a curbside trash bin, a basketball court with crosswinds, and a moving target opening.
- Clean-shot and bounce bonuses, with distinct throwing behavior such as curving trash bags.
- Cartoon-style environments, target outlines, and hit feedback.

## Play in the Editor

This is a prototype in development, currently intended for playtesting in the Unity Editor.

1. Open the project with **Unity 6000.3.11f1** and wait for packages and assets to finish importing.
2. Open `Assets/TossRush/Scenes/TossRushPrototype.unity`.
3. Enter Play Mode, then click **LET'S TOSS**.

| Input | Action |
|---|---|
| Move mouse / Left click | Aim / Throw |
| Esc | Release the cursor; the timer keeps running |
| R | Play again after the round ends |

## Development

Built with Unity, C#, and URP. I direct the gameplay, playtest, and provide feedback, while AI leads implementation, including code and scene creation.

## License and Credits

Original project content that we have the right to license is released under the [MIT License](LICENSE). Furniture, buttons, fonts, and sound effects are provided by **Kenney (CC0)**. Unity and TextMeshPro content remains under its respective original license terms.

See [Asset Sources](Docs/AssetSources.md) and [Third-Party Notices](THIRD_PARTY_NOTICES.md) for details (in Traditional Chinese).

For development settings, technical details, and validation status, see the [Development Guide](Docs/DevelopmentGuide.md) (in Traditional Chinese).
