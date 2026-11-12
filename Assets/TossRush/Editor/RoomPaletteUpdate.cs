using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    // Explicit Editor operation: never changes assets automatically on import or in Play Mode.
    public static class RoomPaletteUpdate
    {
        const string Root = "Assets/TossRush/";
        const string Folder = Root + "Materials/RoomPalette";
        const string Menu = "TossRush/Apply Room Contrast Palette";

        [Serializable]
        sealed class ApplyReport
        {
            public string appliedUtc;
            public int targetRenderers, sceneRenderers;
            public string[] updatedPrefabs;
        }

        sealed class Palette
        {
            public Material wall, wood, secondary, accent, dark, foliage, floorInlay, targetBody, targetRim;
            public readonly Dictionary<string, Material> scene = new Dictionary<string, Material>();
        }

        [MenuItem(Menu)]
        static void ApplyFromMenu() => Debug.Log(Apply());

        [MenuItem(Menu, true)]
        static bool CanApply() => !EditorApplication.isPlayingOrWillChangePlaymode;

        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the room palette.");

            string[] prefabs = new List<string> {
                Root + "Prefabs/Targets/RoomStageTarget.prefab",
                Root + "Prefabs/Targets/CornerTarget.prefab",
                Root + "Prefabs/Stages/RoomStage.prefab",
                Root + "Prefabs/Stages/CornerStage.prefab"
            }.FindAll(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)).ToArray();
            if (prefabs.Length == 0) throw new InvalidOperationException("No room prefabs were found.");
            foreach (string path in prefabs)
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
                    throw new FileNotFoundException("Required prefab is missing.", path);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/StageToon.shader");
            if (!shader) throw new InvalidOperationException("Stage Toon shader is missing.");

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder(Root + "Materials", "RoomPalette");
            Palette palette = CreatePalette(shader);
            var report = new ApplyReport { appliedUtc = DateTime.UtcNow.ToString("o"), updatedPrefabs = prefabs };

            foreach (string path in prefabs) {
                if (path.Contains("/Targets/")) UpdateTarget(path, palette, report);
                else UpdateStage(path, palette, report);
            }
            AssetDatabase.SaveAssets();

            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Validation", "RoomPaletteApply.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            return "Room contrast palette applied to stages 1 and 4: " + report.sceneRenderers +
                " scene renderers, " + report.targetRenderers + " target renderers. Geometry, colliders and scoring were unchanged.";
        }

        static Palette CreatePalette(Shader shader)
        {
            var p = new Palette {
                wall = Material(shader, "RoomWall", "#D4C5B4"),
                wood = Material(shader, "RoomWood", "#AF8B70"),
                secondary = Material(shader, "RoomSecondary", "#B9A68D"),
                accent = Material(shader, "RoomAccent", "#BC885F"),
                dark = Material(shader, "RoomDark", "#5E5147"),
                foliage = Material(shader, "RoomFoliage", "#7E8268"),
                floorInlay = Material(shader, "RoomFloorInlay", "#BFA48C"),
                targetBody = Material(shader, "RoomTargetBody", "#126EBB", true),
                targetRim = Material(shader, "RoomTargetRim", "#C3EAF6", true)
            };
            p.scene.Add("Wood", p.wood);
            p.scene.Add("Mint", p.secondary);
            p.scene.Add("Cream", p.wall);
            p.scene.Add("Gold", p.accent);
            p.scene.Add("Coral", p.accent);
            p.scene.Add("Ink", p.dark);
            p.scene.Add("Midnight", p.dark);
            p.scene.Add("Leaves", p.foliage);
            p.scene.Add("Court", p.wood);
            return p;
        }

        static Material Material(Shader shader, string name, string hex, bool hero = false)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            ColorUtility.TryParseHtmlString(hex, out Color color);
            material.SetColor("_BaseColor", color);
            material.SetColor("_ShadowTint", hero ? new Color(.42f, .60f, .78f, 1) : new Color(.67f, .61f, .54f, 1));
            material.SetFloat("_Steps", 3);
            material.SetFloat("_Rim", hero ? .06f : .02f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static bool IsToon(Material material) => material && material.shader && material.shader.name == "TossRush/Stage Toon";

        static void ColorTarget(Transform root, Palette palette, ApplyReport report)
        {
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) {
                if (!IsToon(renderer.sharedMaterial)) continue;
                if (renderer.name != "BinLip" && renderer.name != "Bin Bottom" && renderer.name != "Bin Slat") continue;
                renderer.sharedMaterial = renderer.name == "BinLip" ? palette.targetRim : palette.targetBody;
                report.targetRenderers++;
            }
        }

        static void UpdateTarget(string path, Palette palette, ApplyReport report)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                ColorTarget(root.transform, palette, report);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void UpdateStage(string path, Palette palette, ApplyReport report)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try {
                var stage = root.GetComponent<StageView>();
                if (!stage || !stage.movingTarget) throw new InvalidOperationException("Stage target reference is missing: " + path);
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) {
                    if (renderer.transform.IsChildOf(stage.movingTarget)) continue;
                    Material[] materials = renderer.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < materials.Length; i++) {
                        if (!IsToon(materials[i])) continue;
                        Material replacement;
                        switch (renderer.name) {
                            case "Left Wall": case "Right Wall": case "Corner Right Wall": case "Backdrop":
                                replacement = palette.wall; break;
                            case "Floor Inlay": replacement = palette.floorInlay; break;
                            case "Bank Shot Panel": case "Ricochet Ramp": case "Corner Photograph": case "Poster":
                                replacement = palette.accent; break;
                            default:
                                if (!palette.scene.TryGetValue(materials[i].name, out replacement)) continue;
                                break;
                        }
                        materials[i] = replacement;
                        changed = true;
                    }
                    if (!changed) continue;
                    renderer.sharedMaterials = materials;
                    report.sceneRenderers++;
                }
                // Explicitly update stage instances too, so existing material overrides cannot mask the palette.
                ColorTarget(stage.movingTarget, palette, report);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
