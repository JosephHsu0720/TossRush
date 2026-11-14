using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TossRush.Editor
{
    public static class WindAndProgressionUpdate
    {
        const string Root = "Assets/TossRush/";
        [MenuItem("TossRush/Apply Wind Lines And Carnival Speed")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var data = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/Carnival.asset");
            data.motionSpeedAfterFirstClear = 1.25f; EditorUtility.SetDirty(data);
            var street = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/Street.asset");
            street.windLineWidthMultiplier = 2; EditorUtility.SetDirty(street);
            string materialPath = Root + "Materials/WindStreak.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material) { material = new Material(Shader.Find("TossRush/Wind Streak")); material.name = "WindStreak"; AssetDatabase.CreateAsset(material, materialPath); }
            string path = Root + "Prefabs/PlayerView.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var controller = root.GetComponent<ThrowController>();
                var old = controller.view.transform.Find("Wind Streaks");
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var effect = new GameObject("Wind Streaks", typeof(WindStreaks)).GetComponent<WindStreaks>();
                effect.transform.SetParent(controller.view.transform, false); effect.gameObject.layer = 9;
                effect.thrower = controller; effect.streaks = new LineRenderer[12];
                for (int i = 0; i < effect.streaks.Length; i++)
                {
                    var line = new GameObject("Wind line " + (i + 1), typeof(LineRenderer)).GetComponent<LineRenderer>();
                    line.transform.SetParent(effect.transform, false); line.gameObject.layer = 9;
                    line.useWorldSpace = false; line.positionCount = 6; line.enabled = false;
                    line.sharedMaterial = material; line.widthMultiplier = .012f;
                    line.numCapVertices = 3; line.numCornerVertices = 2;
                    line.textureMode = LineTextureMode.Stretch; line.alignment = LineAlignment.View;
                    line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
                    line.lightProbeUsage = LightProbeUsage.Off; line.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    line.sortingOrder = -20; effect.streaks[i] = line;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();Debug.Log("Saved blue wind streaks and one-time 25% carnival speed increase.");
        }
    }
}
