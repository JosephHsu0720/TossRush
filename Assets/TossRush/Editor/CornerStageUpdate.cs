using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    // Authoring only: the mesh and both visual instances are saved into prefabs.
    public static class CornerStageUpdate
    {
        const string Root = "Assets/TossRush/";
        const string Menu = "TossRush/Apply Corner Stage Update";
        const string Request = "Temp/TossRushCornerUpdate.request";
        const string ReportPath = "Docs/Validation/CornerStageApply.json";

        [Serializable]
        sealed class Report
        {
            public string appliedUtc;
            public Vector3 oldRampEuler, rampEuler;
            public string validation;
        }

        // A request is local, explicit and consumed once. Merely importing this tool does nothing.
        [InitializeOnLoadMethod]
        static void ReadRequest() => EditorApplication.delayCall += () => {
            if (!File.Exists(Request) || File.ReadAllText(Request).Trim() != "corner-paper-ramp-v1") return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try { Debug.Log(Apply()); }
            catch (Exception exception) { Debug.LogException(exception); }
        };

        [MenuItem(Menu)]
        static void ApplyFromMenu() => Debug.Log(Apply());
        [MenuItem(Menu, true)]
        static bool CanApply() => !EditorApplication.isPlayingOrWillChangePlaymode;

        public static string Apply()
        {
            if (!CanApply()) throw new InvalidOperationException("Exit Play Mode before updating CornerStage.");
            var report = UpdateRamp();
            var mesh = MakePaperMesh();
            var visual = MakePaperVisual(mesh);
            UpdateVisual(Root + "Prefabs/Projectiles/PaperBall.prefab", "Visual", visual);
            UpdateVisual(Root + "Prefabs/PlayerView.prefab", "Main Camera/Held PaperBall/Visual", visual);
            AssetDatabase.SaveAssets();
            report.appliedUtc = DateTime.UtcNow.ToString("o");
            report.validation = "Ramp direction checked with flight math. Play Mode bank shots and Game View appearance still require verification.";
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            return "CornerStage updated: ramp " + report.rampEuler + ", crumpled paper visual on held and thrown objects.";
        }

        static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

        static Report UpdateRamp()
        {
            const string path = Root + "Prefabs/Stages/CornerStage.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try {
                var ramp = Find(root.transform, "Ricochet Ramp");
                var report = new Report {
                    oldRampEuler = ramp.localEulerAngles,
                    rampEuler = new Vector3(-35, -50, 0)
                };
                // The bin is to the right of this ramp. Negative yaw points its top
                // normal toward the bin; the original +15 degrees bounced paper left.
                ramp.localRotation = Quaternion.Euler(report.rampEuler);
                if (!PrefabUtility.SaveAsPrefabAsset(root,path)) throw new IOException("Could not save CornerStage.");
                return report;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Mesh MakePaperMesh()
        {
            // Subdivided octahedron with shared, deterministic folds and flat triangle normals.
            var vertices = new List<Vector3> { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            var triangles = new List<int> { 0,4,3, 0,2,4, 0,5,2, 0,3,5, 1,3,4, 1,4,2, 1,2,5, 1,5,3 };
            for (int level = 0; level < 3; level++) {
                var edges = new Dictionary<(int,int), int>();
                int Mid(int a, int b) {
                    var key = a < b ? (a,b) : (b,a);
                    if (edges.TryGetValue(key, out int index)) return index;
                    index = vertices.Count; vertices.Add((vertices[a] + vertices[b]).normalized); edges.Add(key,index); return index;
                }
                var next = new List<int>();
                for (int i = 0; i < triangles.Count; i += 3) {
                    int a = triangles[i], b = triangles[i+1], c = triangles[i+2], ab = Mid(a,b), bc = Mid(b,c), ca = Mid(c,a);
                    next.AddRange(new[] { a,ab,ca, ab,b,bc, ca,bc,c, ab,bc,ca });
                }
                triangles = next;
            }
            for (int i = 0; i < vertices.Count; i++) {
                Vector3 v = vertices[i];
                float fold = Mathf.Sin(v.x * 27 + v.z * 11) * Mathf.Cos(v.y * 23 - v.x * 7);
                float wrinkle = Mathf.Sin(v.y * 43 + v.z * 17);
                vertices[i] = v * (.435f + .045f * fold + .02f * wrinkle);
            }
            var flat = new List<Vector3>();
            var indices = new List<int>();
            for (int i = 0; i < triangles.Count; i += 3) {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i+1]], c = vertices[triangles[i+2]];
                Vector3 center = (a+b+c)/3;
                center -= center.normalized * (.012f + .018f * Mathf.Abs(Mathf.Sin(i * 1.7f)));
                foreach (Vector3 v in new[] { a,b,center, b,c,center, c,a,center }) { indices.Add(flat.Count); flat.Add(v); }
            }
            var generated = new Mesh { name = "CrumpledPaper" };
            generated.SetVertices(flat); generated.SetTriangles(indices, 0); generated.RecalculateNormals(); generated.RecalculateBounds();
            string path = Root + "Meshes/CrumpledPaper.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { AssetDatabase.CreateAsset(generated,path); return generated; }
            EditorUtility.CopySerialized(generated,mesh); UnityEngine.Object.DestroyImmediate(generated); EditorUtility.SetDirty(mesh); return mesh;
        }

        static GameObject MakePaperVisual(Mesh mesh)
        {
            string materialPath = Root + "Materials/CrumpledPaper.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material) {
                material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/StageToon.shader"));
                AssetDatabase.CreateAsset(material,materialPath);
            }
            material.SetColor("_BaseColor", new Color(.97f,.96f,.91f));
            material.SetColor("_ShadowTint", new Color(.63f,.65f,.66f));
            material.SetFloat("_Steps",5); material.SetFloat("_Rim",.015f); EditorUtility.SetDirty(material);
            var root = new GameObject("PaperBallVisual",typeof(MeshFilter),typeof(MeshRenderer));
            try {
                root.layer = 8;
                root.GetComponent<MeshFilter>().sharedMesh = mesh;
                root.GetComponent<MeshRenderer>().sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(root,Root + "Prefabs/Projectiles/PaperBallVisual.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void UpdateVisual(string path, string childPath, GameObject visual)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try {
                var old = root.transform.Find(childPath);
                if (!old) old = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Held PaperBall").Find("Visual");
                if (!old) throw new InvalidOperationException("Paper visual not found in " + path);
                Transform parent = old.parent;
                Vector3 position = old.localPosition, scale = old.localScale;
                Quaternion rotation = old.localRotation;
                int sibling = old.GetSiblingIndex();
                UnityEngine.Object.DestroyImmediate(old.gameObject);
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(visual, parent);
                replacement.name = "Visual";
                replacement.transform.SetLocalPositionAndRotation(position,rotation);
                replacement.transform.localScale = scale;
                replacement.transform.SetSiblingIndex(sibling);
                if (!PrefabUtility.SaveAsPrefabAsset(root,path)) throw new IOException("Could not save " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}

