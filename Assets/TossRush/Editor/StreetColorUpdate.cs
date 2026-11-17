using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    public static class StreetColorUpdate
    {
        const string Root = "Assets/TossRush/";

        [MenuItem("TossRush/Apply Street Colour Matching")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var colors = new[] { new Color(.85f, .12f, .10f), new Color(.12f, .62f, .22f), new Color(.08f, .35f, .88f) };
            var names = new[] { "Red", "Green", "Blue" };
            var bags = new Material[3];
            var bins = new Material[3];
            for (int i = 0; i < 3; i++)
            {
                bags[i] = Material("Bag" + names[i], "BagFold", colors[i]);
                bins[i] = Material("Bin" + names[i], "BinBlue", colors[i]);
            }
            string visualPath = Root + "Prefabs/Projectiles/TrashBagVisual.prefab";
            var visual = PrefabUtility.LoadPrefabContents(visualPath);
            try
            {
                var colorVisual = visual.GetComponent<TrashColorVisual>();
                if (!colorVisual) colorVisual = visual.AddComponent<TrashColorVisual>();
                colorVisual.surfaces = visual.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray();
                colorVisual.red = bags[0]; colorVisual.green = bags[1]; colorVisual.blue = bags[2];
                colorVisual.Apply(TrashColor.Red);
                PrefabUtility.SaveAsPrefabAsset(visual, visualPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(visual); }

            string stagePath = Root + "Prefabs/Stages/StreetStage.prefab";
            var root = PrefabUtility.LoadPrefabContents(stagePath);
            try
            {
                var stage = root.GetComponent<StageView>();
                var gates = stage.movingTarget.GetComponentsInChildren<ScoreGate>(true).OrderBy(g => g.transform.position.x).ToArray();
                if (gates.Length != 3) throw new InvalidOperationException("Street requires exactly three bins.");
                for (int i = 0; i < gates.Length; i++)
                {
                    gates[i].trashColor = (TrashColor)(i + 1);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(gates[i]);
                    var marker = gates[i].GetComponentInParent<TargetMarker>();
                    foreach (var renderer in marker.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (renderer.GetComponentInParent<TargetMarker>() != marker) continue;
                        if (renderer.name != "Wheeled Bin Body" && renderer.name != "Roll Surface") continue;
                        renderer.sharedMaterial = bins[i];
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, stagePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            var data = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/Street.asset");
            data.matchTrashColors = true; data.wrongBinPenalty = 50;
            data.instruction = "MATCH BAG + BIN COLOUR / WRONG BIN -50 / LID REBOUND +30 / PEDESTRIAN -10";
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("Saved red, green and blue bags/bins with colour matching and a 50-point wrong-bin penalty.");
        }

        static Material Material(string name, string source, Color color)
        {
            string path = Root + "Materials/Street/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Street/" + source + ".mat"));
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
