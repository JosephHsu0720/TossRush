using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    public static class StagePlayUpdate
    {
        const string Root = "Assets/TossRush/";

        [MenuItem("TossRush/Apply Three Targets And Collision Points")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            AddStreetTargets();
            UpdateHeldBag();
            UpdateCollisionHUD();
            foreach (string name in new[] { "Corner", "Street", "Basketball", "Carnival" })
            {
                var data = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/" + name + ".asset");
                data.pointsPerPropCollision = 30;
                data.instruction = name == "Street" ? "TRASH BAG / CHOOSE ANY BIN / EACH CONTACT +30" :
                    name == "Corner" ? "PAPER / CLEAN +50 / EACH CONTACT +30" :
                    name == "Basketball" ? "BASKETBALL / CLEAN +100 / EACH CONTACT +30" : "BEANBAG / CLEAN +100 / EACH CONTACT +30";
                if (name == "Street") data.projectileEulerAngles = new Vector3(0, 0, 90);
                EditorUtility.SetDirty(data);
                AddProps(name == "Corner" ? "CornerStage" : name == "Street" ? "StreetStage" : name + "Stage");
            }
            AssetDatabase.SaveAssets();
        }

        static void Edit(string relative, Action<GameObject> change)
        {
            string path = Root + relative;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                change(root);
                if (!PrefabUtility.SaveAsPrefabAsset(root, path)) throw new InvalidOperationException("Could not save " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void AddStreetTargets() => Edit("Prefabs/Stages/StreetStage.prefab", root =>
        {
            var stage = root.GetComponent<StageView>();
            string targetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(stage.movingTarget.gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            foreach (int side in new[] { -1, 1 })
            {
                string name = side < 0 ? "Extra Target Left" : "Extra Target Right";
                Transform existing = stage.movingTarget.Find(name);
                var target = existing ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.movingTarget);
                target.name = name;
                target.transform.SetLocalPositionAndRotation(new Vector3(side * 2.2f, 0, 0), Quaternion.identity);
                target.transform.localScale = Vector3.one;
                foreach (var gate in target.GetComponentsInChildren<ScoreGate>(true)) gate.stage = stage;
            }
        });

        static void UpdateHeldBag() => Edit("Prefabs/PlayerView.prefab", root =>
        {
            var held = root.GetComponentsInChildren<HeldItemSpin>(true).Single();
            held.transform.localRotation = Quaternion.Euler(0, 0, 90);
        });

        static void UpdateCollisionHUD() => Edit("Prefabs/UI/GameHUD.prefab", root =>
        {
            var hud = root.GetComponent<GameHUD>();
            Transform existing = hud.playPanel.transform.Find("Collision Points");
            var layer = existing ? existing.gameObject : new GameObject("Collision Points", typeof(RectTransform), typeof(CollisionPointFeedback));
            var rect = layer.GetComponent<RectTransform>();
            rect.SetParent(hud.playPanel.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = Vector2.one * .5f;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            hud.collisionFeedback = layer.GetComponent<CollisionPointFeedback>();
            if (hud.collisionFeedback.labels == null || hud.collisionFeedback.labels.Length == 0)
            {
                hud.collisionFeedback.labels = new TMP_Text[24];
                for (int i = 0; i < 24; i++)
                {
                    var go = new GameObject("Contact +30 " + i, typeof(RectTransform), typeof(TextMeshProUGUI));
                    go.transform.SetParent(rect, false);
                    var label = go.GetComponent<TextMeshProUGUI>();
                    label.font = hud.feedbackText.font;
                    label.fontSharedMaterial = hud.feedbackText.fontSharedMaterial;
                    label.fontSize = 32; label.fontStyle = FontStyles.Bold; label.alignment = TextAlignmentOptions.Center;
                    label.text = "+30"; label.raycastTarget = false;
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = Vector2.one * .5f;
                    label.rectTransform.sizeDelta = new Vector2(160, 60);
                    hud.collisionFeedback.labels[i] = label;
                    go.SetActive(false);
                }
            }
        });

        static void AddProps(string stageName) => Edit("Prefabs/Stages/" + stageName + ".prefab", root =>
        {
            if (root.transform.Find("Extra Physics Props")) return;
            var parent = new GameObject("Extra Physics Props").transform;
            parent.SetParent(root.transform, false);
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
            {
                bool ball = i == 2;
                PrimitiveType shape = ball ? PrimitiveType.Sphere : i == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                string propName = stageName == "StreetStage" ? (ball ? "Loose Ball" : i == 0 ? "Drink Can" : "Parcel") :
                    stageName == "BasketballStage" ? (ball ? "Practice Ball" : i == 0 ? "Water Bottle" : "Equipment Box") :
                    stageName == "CarnivalStage" ? (ball ? "Prize Ball" : i == 0 ? "Tin Can" : "Prize Box") :
                    (ball ? "Toy Ball" : i == 0 ? "Pencil Cup" : "Small Box");
                var go = GameObject.CreatePrimitive(shape);
                go.name = propName + " " + side;
                go.transform.SetParent(parent, false);
                Vector3 size = ball ? Vector3.one * .32f : i == 0 ? new Vector3(.19f, .20f, .19f) : new Vector3(.42f, .32f, .34f);
                float height = i == 0 ? size.y : size.y * .5f;
                go.transform.localPosition = new Vector3(side * (2.5f + i * .55f), height + .025f, -1.6f + i * 1.05f);
                go.transform.localScale = size;
                string material = ball ? "Coral" : i == 0 ? "Mint" : "Wood";
                go.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/" + material + ".mat");
                var body = go.AddComponent<Rigidbody>();
                body.mass = ball ? .18f : i == 0 ? .12f : .35f;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                go.AddComponent<PhysicsProp>();
            }
        });
    }
}
