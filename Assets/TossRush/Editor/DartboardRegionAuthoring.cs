using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    public static class DartboardRegionAuthoring
    {
        const string Root = "Assets/TossRush/";
        const string LayoutPath = Root + "Data/DartboardLayout.asset";

        public static void Configure(Transform target)
        {
            var stage = target.GetComponentInParent<StageView>();
            var layout = AssetDatabase.LoadAssetAtPath<DartboardLayout>(LayoutPath);
            if (!layout)
            {
                layout = ScriptableObject.CreateInstance<DartboardLayout>();
                AssetDatabase.CreateAsset(layout, LayoutPath);
            }
            BuildLayout(layout, target.Find("Twenty sector face").localPosition.z - .002f);
            var selector = target.GetComponent<DartboardTarget>();
            if (!selector) selector = target.gameObject.AddComponent<DartboardTarget>();
            selector.layout = layout; selector.scoreGate = stage.gate;
            foreach (string name in new[] { "OutlineMask", "Outline" })
            {
                var child = target.Find(name);
                if (!child) { child = new GameObject(name).transform; child.SetParent(target, false); }
                var filter = child.GetComponent<MeshFilter>();
                if (!filter) filter = child.gameObject.AddComponent<MeshFilter>();
                var renderer = child.GetComponent<MeshRenderer>();
                if (!renderer) renderer = child.gameObject.AddComponent<MeshRenderer>();
                bool mask = name == "OutlineMask";
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/" + (mask ? "TargetSilhouetteMask" : "TargetOutline") + ".mat");
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                if (mask) selector.maskFilter = filter; else selector.outlineFilter = filter;
            }
            // The saved preview is just a preview; ResetStage always chooses at runtime, including the first entry.
            selector.maskFilter.sharedMesh = layout.regions[2].mask;
            selector.outlineFilter.sharedMesh = layout.regions[2].outline;
            stage.gate.name = "Selected region score plane";
            stage.data.title = "BAR / DARTS";
            stage.data.instruction = "DARTS / HIT THE YELLOW TARGET / CLEAN +150";
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/PlayerView.prefab").GetComponent<ThrowController>();
            float baselineLaunchZ = player.transform.InverseTransformPoint(player.throwOrigin.position).z;
            float targetZ = stage.transform.InverseTransformPoint(stage.gate.transform.position).z;
            // First 20% closer, then another 10% of the remaining distance: .8 * .9 = .72.
            stage.data.playerForwardOffset = (targetZ - baselineLaunchZ) * .28f;
            EditorUtility.SetDirty(stage.data);
            var line = stage.transform.Find("The Copper Flight Bar/Throw line");
            if (line) line.localPosition = new Vector3(0, .016f, -4.3f + stage.data.playerForwardOffset);
            var label = stage.transform.Find("The Copper Flight Bar/Target instruction");
            if (label) label.GetComponent<TMP_Text>().text = "HIT THE YELLOW TARGET";
            var chalkboard = stage.transform.Find("The Copper Flight Bar/Chalkboard lettering");
            if (chalkboard) chalkboard.GetComponent<TMP_Text>().text = "TONIGHT\nDARTS CLUB\nFOLLOW YELLOW";
        }

        static void BuildLayout(DartboardLayout layout, float depth)
        {
            var regions = new List<DartboardLayout.Region>
            {
                new DartboardLayout.Region { label = "BULLSEYE", outerRadius = .20f, endAngle = 360 },
                new DartboardLayout.Region { label = "OUTER BULL", innerRadius = .20f, outerRadius = .29f, endAngle = 360 }
            };
            int[] numbers = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };
            float[] edges = { .29f, .59f, .65f, .95f, 1.015f };
            string[] names = { "INNER SINGLE", "TRIPLE", "OUTER SINGLE", "DOUBLE" };
            for (int sector = 0; sector < 20; sector++)
            for (int band = 0; band < 4; band++)
                regions.Add(new DartboardLayout.Region
                {
                    label = numbers[sector] + " " + names[band],
                    innerRadius = edges[band], outerRadius = edges[band + 1],
                    startAngle = sector * 18 - 9, endAngle = sector * 18 + 9
                });
            var savedMeshes = AssetDatabase.LoadAllAssetsAtPath(LayoutPath).OfType<Mesh>().ToDictionary(mesh => mesh.name);
            for (int i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                var fill = Shape(region.innerRadius, region.outerRadius, region.startAngle, region.endAngle, depth);
                var outside = Shape(i == 0 ? .29f : 1.015f, 1.275f, 0, 360, depth);
                var mask = new Mesh();
                mask.CombineMeshes(i == 0
                    ? new[] { new CombineInstance { mesh = fill, transform = Matrix4x4.identity },
                        new CombineInstance { mesh = outside, transform = Matrix4x4.identity } }
                    : new[] { new CombineInstance { mesh = fill, transform = Matrix4x4.identity } });
                var sourceVertices = fill.vertices; var sourceIndices = fill.triangles;
                var positions = new Vector3[sourceVertices.Length * 16];
                var uv = new Vector2[positions.Length]; var indices = new int[sourceIndices.Length * 16];
                for (int copy = 0; copy < 16; copy++)
                {
                    float angle = copy * Mathf.PI * 2 / 16;
                    for (int vertex = 0; vertex < sourceVertices.Length; vertex++)
                    {
                        positions[copy * sourceVertices.Length + vertex] = sourceVertices[vertex];
                        uv[copy * sourceVertices.Length + vertex] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    }
                    for (int index = 0; index < sourceIndices.Length; index++) indices[copy * sourceIndices.Length + index] = sourceIndices[index] + copy * sourceVertices.Length;
                }
                var outline = new Mesh { vertices = positions, uv = uv, triangles = indices };
                outline.RecalculateBounds();
                region.mask = SaveMesh(mask, "Region" + i + "Mask", layout, savedMeshes);
                region.outline = SaveMesh(outline, "Region" + i + "Outline", layout, savedMeshes);
                Object.DestroyImmediate(fill); Object.DestroyImmediate(outside);
            }
            layout.regions = regions.ToArray(); EditorUtility.SetDirty(layout);
        }

        static Mesh SaveMesh(Mesh source, string name, DartboardLayout layout, Dictionary<string, Mesh> savedMeshes)
        {
            if (savedMeshes.TryGetValue(name, out var saved))
            {
                saved.Clear(); saved.vertices = source.vertices; saved.uv = source.uv; saved.triangles = source.triangles;
                saved.RecalculateBounds(); saved.UploadMeshData(false); EditorUtility.SetDirty(saved);
                Object.DestroyImmediate(source); return saved;
            }
            source.name = name; AssetDatabase.AddObjectToAsset(source, layout); return source;
        }

        static Mesh Shape(float inner, float outer, float start, float end, float depth)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt((end - start) / 3));
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < steps; i++)
            {
                float a = Mathf.Lerp(start, end, i / (float)steps) * Mathf.Deg2Rad;
                float b = Mathf.Lerp(start, end, (i + 1) / (float)steps) * Mathf.Deg2Rad;
                int n = vertices.Count;
                vertices.Add(new Vector3(Mathf.Sin(a) * inner, Mathf.Cos(a) * inner, depth));
                vertices.Add(new Vector3(Mathf.Sin(a) * outer, Mathf.Cos(a) * outer, depth));
                vertices.Add(new Vector3(Mathf.Sin(b) * outer, Mathf.Cos(b) * outer, depth));
                vertices.Add(new Vector3(Mathf.Sin(b) * inner, Mathf.Cos(b) * inner, depth));
                triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); return mesh;
        }
    }
}
