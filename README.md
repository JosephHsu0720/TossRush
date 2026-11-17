# TossRush

[繁體中文](README_tw.md) | [English](README.md)

A 20-second first-person throwing game made with Unity.

Toss paper balls, trash bags, basketballs, plastic bottles, newspapers, and darts at different targets to chase a high score in just 20 seconds! Each successful throw takes you to the next scene. Use the trajectory preview, account for the wind, and bounce shots off the scenery to find your aim.

## Features

- Six settings: a room corner, curbside trash bins, a basketball court with crosswinds, a moving carnival opening, a doorstep mailbox, and bar darts.
- Bar darts choose a random scoring region on every entry, including the first. A yellow outline marks the active target, and the throwing distance is 28% shorter overall.
- Clean-shot and bounce bonuses, with distinct throwing behavior such as curving trash bags.
- Cartoon-style environments, target outlines, and hit feedback.
- Looping CC0 music with throw, goal, start and result sounds.
- Home-screen speaker and volume slider with saved master volume and mute controls. Throws use a short CC0 whoosh; collision, penalty and prize sounds are disabled.

## Play in the Editor

This is a prototype in development. See the [release checklist](Docs/Release/ReleaseChecklist.md) for Web builds, browser validation, and the private itch.io draft.

1. Open the project with **Unity 6000.3.11f1** and wait for packages and assets to finish importing (the version recorded in ProjectSettings and used for the Web release build).
2. Open `Assets/TossRush/Scenes/TossRush.unity`.
3. Enter Play Mode, then click **LET'S TOSS**.

| Input | Action |
|---|---|
| Move mouse / Left click | Aim / Throw |
| Esc | Release the cursor; the timer keeps running |
| R | Play again after the round ends |

## Development

Built with Unity, C#, and URP. I direct the gameplay, playtest, and provide feedback, while AI leads implementation, including code and scene creation.

## License and Credits

Original project content that we have the right to license is released under the [MIT License](LICENSE). Furniture, buttons, fonts, and interface sounds are provided by **Kenney (CC0)**. Throw whoosh: **Woven SFX (CC0)**. Music: **Level 1 by Juhani Junkala / SubspaceAudio (CC0)**. Unity and TextMeshPro content remains under its respective original license terms.

See [Asset Sources](Docs/AssetSources.md) and [Third-Party Notices](THIRD_PARTY_NOTICES.md) for details (in Traditional Chinese).

For development settings, technical details, and validation status, see the [Development Guide](Docs/DevelopmentGuide.md) (in Traditional Chinese).
