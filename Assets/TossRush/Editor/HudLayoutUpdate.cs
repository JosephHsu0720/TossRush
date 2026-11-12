using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace TossRush.Editor
{
    public static class HudLayoutUpdate
    {
        const string Path = "Assets/TossRush/Prefabs/UI/GameHUD.prefab";
        const string Menu = "TossRush/Apply HUD Layout Update";

        [Serializable]
        sealed class LayoutReport
        {
            public string appliedUtc;
            public string layout = "Lower-left stage, wind and timer; lower-right score; footer fullscreen icon.";
            public string validation = "Prefab layout applied. Game View visual verification is still required.";
        }

        [MenuItem(Menu)]
        static void ApplyFromMenu() => Debug.Log(Apply());
        [MenuItem(Menu, true)]
        static bool CanApply() => !EditorApplication.isPlayingOrWillChangePlaymode;

        public static string Apply()
        {
            if (!CanApply()) throw new InvalidOperationException("Exit Play Mode before editing the HUD prefab.");
            var root = PrefabUtility.LoadPrefabContents(Path);
            try {
                Configure(root.GetComponent<GameHUD>());
                if (!PrefabUtility.SaveAsPrefabAsset(root, Path)) throw new IOException("Could not save the HUD prefab.");
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/Validation");
                File.WriteAllText("Docs/Validation/HudLayoutApply.json", JsonUtility.ToJson(new LayoutReport { appliedUtc = DateTime.UtcNow.ToString("o") }, true));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return "HUD moved to lower corners. The upper-center throwing area is clear. Gameplay and scene assets were not rebuilt.";
        }

        static void Place(RectTransform rect, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        public static void Configure(GameHUD hud)
        {
            // Retain the existing TopBar object and references; it now forms the lower-left card.
            var left = (RectTransform)hud.stageText.transform.parent;
            Place(left, hud.playPanel.transform, Vector2.zero, Vector2.zero, new Vector2(24, 100), new Vector2(320, 150));
            Color background = new Color(.025f, .045f, .08f, .72f);
            var leftImage = left.GetComponent<UnityEngine.UI.Image>();
            if (leftImage) { leftImage.color = background; leftImage.raycastTarget = false; }
            Place(hud.stageText.rectTransform, left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -12), new Vector2(288, 28));
            hud.stageText.fontSize = 16; hud.stageText.enableAutoSizing = false;
            hud.stageText.alignment = TextAlignmentOptions.MidlineLeft;
            if (hud.windIndicator) {
                Place((RectTransform)hud.windIndicator.transform, left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -47), new Vector2(288, 30));
                Place(hud.windIndicator.arrow, hud.windIndicator.transform, new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(26, 0), new Vector2(56, 26));
            }
            Place(hud.timerText.rectTransform, left, Vector2.zero, Vector2.zero, new Vector2(16, 22), new Vector2(180, 44));
            hud.timerText.fontSize = 38; hud.timerText.enableAutoSizing = false;
            hud.timerText.alignment = TextAlignmentOptions.MidlineLeft;
            Place((RectTransform)hud.timerFill.transform.parent, left, Vector2.zero, Vector2.zero, new Vector2(16, 14), new Vector2(288, 4));

            var right = hud.playPanel.transform.Find("ScorePanel") as RectTransform;
            if (!right) {
                var go = new GameObject("ScorePanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                right = go.GetComponent<RectTransform>();
            }
            Place(right, hud.playPanel.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 100), new Vector2(260, 104));
            var rightImage = right.GetComponent<UnityEngine.UI.Image>(); rightImage.color = background; rightImage.raycastTarget = false;
            Place(hud.highScoreText.rectTransform, right, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -12), new Vector2(228, 24));
            hud.highScoreText.fontSize = 12; hud.highScoreText.enableAutoSizing = false;
            hud.highScoreText.alignment = TextAlignmentOptions.MidlineRight;
            Place(hud.scoreText.rectTransform, right, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 14), new Vector2(228, 52));
            hud.scoreText.fontSize = 40; hud.scoreText.enableAutoSizing = false;
            hud.scoreText.alignment = TextAlignmentOptions.MidlineRight;

            var footer = (RectTransform)hud.hintText.transform.parent;
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1, 0); footer.pivot = new Vector2(.5f, 0);
            footer.offsetMin = new Vector2(24, 24); footer.offsetMax = new Vector2(-88, 68);
            var hint = hud.hintText.rectTransform;
            hint.anchorMin = Vector2.zero; hint.anchorMax = Vector2.one; hint.pivot = new Vector2(.5f, .5f);
            hint.offsetMin = new Vector2(12, 0); hint.offsetMax = new Vector2(-12, 0);
            hud.hintText.fontSize = 13; hud.hintText.enableAutoSizing = false;
            var fullscreen = hud.GetComponentInChildren<FullscreenButton>(true);
            if (fullscreen) Place((RectTransform)fullscreen.transform, hud.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(44, 44));

            Place((RectTransform)hud.feedback.transform, hud.playPanel.transform, new Vector2(.5f, 0), new Vector2(.5f, .5f), new Vector2(0, 86), new Vector2(800, 28));
            hud.feedbackText.fontSize = 20;
            hud.feedbackText.rectTransform.anchorMin = Vector2.zero; hud.feedbackText.rectTransform.anchorMax = Vector2.one;
            hud.feedbackText.rectTransform.offsetMin = Vector2.zero; hud.feedbackText.rectTransform.offsetMax = Vector2.zero;
            hud.cleanShotEffect.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 176);
        }
    }
}
