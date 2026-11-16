using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TossRush.Editor
{
    // Saved authoring assets, following the same pipeline as the other five stages.
    public static class DartsStageUpdate
    {
        const string Root = "Assets/TossRush/";
        const string Palette = Root + "Materials/Bar/";
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Palette + name + ".mat");

        [MenuItem("TossRush/Apply Dartboard Outline")]
        public static void ApplyOutline()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            EditPrefab("Prefabs/Stages/DartsStage.prefab", root => AddTargetOutline(root.GetComponent<StageView>().movingTarget));
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TossRush/Apply Random Dartboard Targets")]
        public static void ApplyRandomTargets() => ApplyOutline();

        [MenuItem("TossRush/Refresh Dartboard Colors")]
        public static void RefreshBoardFace()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            BoardMesh();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TossRush/Add Bar Darts Stage")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!AssetDatabase.IsValidFolder(Palette.TrimEnd('/'))) AssetDatabase.CreateFolder(Root + "Materials", "Bar");
            ColorMat("Walnut", new Color(.25f, .12f, .075f));
            ColorMat("Oak", new Color(.52f, .29f, .13f));
            ColorMat("Plaster", new Color(.13f, .22f, .21f));
            ColorMat("Brass", new Color(.91f, .64f, .26f));
            ColorMat("Black", new Color(.035f, .045f, .05f));
            ColorMat("Ivory", new Color(.94f, .87f, .69f));
            ColorMat("Red", new Color(.88f, .10f, .12f));
            ColorMat("Green", new Color(.04f, .38f, .27f));
            ColorMat("Steel", new Color(.61f, .69f, .71f));
            ColorMat("Amber", new Color(1, .65f, .22f), true);
            ColorMat("Neon", new Color(.24f, .94f, .77f), true);
            ColorMat("Burgundy", new Color(.35f, .045f, .07f));
            var visual = MakeDartVisual();
            var projectile = MakeProjectile(visual);
            var data = LoadOrCreate<StageData>("Data/Darts.asset");
            data.title = "BAR / BULLSEYE";
            data.instruction = "DARTS / HIT THE RED BULLSEYE / CLEAN +150";
            data.accent = new Color(1, .65f, .26f);
            data.points = 100; data.cleanShotBonus = 150; data.pointsPerPropCollision = 0;
            data.heldItemIndex = 5; data.launchSpeed = 22; data.throwLoft = 0; data.horizontalThrow = true;
            data.gravityScale = 1; data.projectileRadius = .018f; data.targetRadius = .20f;
            data.randomize = false; data.wind = Vector3.zero; data.windSpeedRange = Vector2.zero;
            data.windInfluence = 0; data.motionAmplitude = 0; data.throwInterval = .32f;
            data.projectilePrefab = projectile;
            var profile = LoadOrCreate<ProjectilePhysicsProfile>("Data/Physics/Dart.asset");
            profile.displayName = "Steel-tip dart"; profile.mass = .024f;
            profile.referenceArea = .00015f; profile.dragCoefficient = .35f;
            profile.initialSpin = Vector3.zero; profile.angularDamping = 0; profile.flightLifetime = 5;
            data.physicsProfile = profile;
            EditorUtility.SetDirty(data); EditorUtility.SetDirty(profile);
            MakeStage(data);
            EditPrefab("Prefabs/PlayerView.prefab", player => AddHeld(player.GetComponent<ThrowController>(), visual));
            EditPrefab("Prefabs/UI/GameHUD.prefab", UpdateMenu);
            AssetDatabase.SaveAssets();
            UpdateScene(visual);
            Debug.Log("Bar darts stage saved and appended to the six-stage rotation.");
        }

        static T LoadOrCreate<T>(string relative) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Root + relative);
            if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, Root + relative); }
            return asset;
        }

        static void ColorMat(string name, Color color, bool unlit = false)
        {
            var material = Mat(name);
            if (!material)
            {
                material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "TossRush/Stage Toon"));
                AssetDatabase.CreateAsset(material, Palette + name + ".mat");
            }
            material.SetColor("_BaseColor", color); EditorUtility.SetDirty(material);
        }

        static Transform Group(Transform parent, string name, Vector3 position)
        {
            var result = new GameObject(name).transform;
            result.SetParent(parent, false); result.localPosition = position; return result;
        }

        static GameObject Part(Transform parent, string name, Vector3 position, Vector3 size, string material,
            PrimitiveType shape = PrimitiveType.Cube, bool solid = false, Vector3 angles = default)
        {
            var part = GameObject.CreatePrimitive(shape); part.name = name;
            part.transform.SetParent(parent, false); part.transform.localPosition = position;
            part.transform.localScale = size; part.transform.localEulerAngles = angles;
            part.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
            if (!solid) UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        static GameObject Save(GameObject root, string relative)
        {
            try { return PrefabUtility.SaveAsPrefabAsset(root, Root + relative); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void EditPrefab(string path, Action<GameObject> action)
        {
            var root = PrefabUtility.LoadPrefabContents(Root + path);
            try { action(root); PrefabUtility.SaveAsPrefabAsset(root, Root + path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static GameObject MakeDartVisual()
        {
            var root = new GameObject("DartVisual");
            Part(root.transform, "Steel point", new Vector3(0, 0, .17f), new Vector3(.013f, .075f, .013f), "Steel", PrimitiveType.Cylinder, angles: new Vector3(90, 0, 0));
            Part(root.transform, "Brass barrel", new Vector3(0, 0, .035f), new Vector3(.043f, .065f, .043f), "Brass", PrimitiveType.Cylinder, angles: new Vector3(90, 0, 0));
            for (int i = 0; i < 6; i++)
                Part(root.transform, "Grip band " + i, new Vector3(0, 0, -.012f + i * .017f), new Vector3(.047f, .002f, .047f), "Black", PrimitiveType.Cylinder, angles: new Vector3(90, 0, 0));
            Part(root.transform, "Tail shaft", new Vector3(0, 0, -.105f), new Vector3(.017f, .065f, .017f), "Steel", PrimitiveType.Cylinder, angles: new Vector3(90, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                var fin = Group(root.transform, "Flight " + i, new Vector3(0, 0, -.18f));
                fin.localRotation = Quaternion.Euler(0, 0, i * 90);
                Part(fin, "Crimson flight", new Vector3(.043f, 0, 0), new Vector3(.086f, .006f, .105f), "Red", angles: new Vector3(0, -22, 0));
                Part(fin, "Ivory stripe", new Vector3(.065f, -.004f, -.025f), new Vector3(.03f, .008f, .025f), "Ivory");
            }
            return Save(root, "Prefabs/Projectiles/DartVisual.prefab");
        }

        static Projectile MakeProjectile(GameObject visual)
        {
            var root = new GameObject("DartProjectile"); root.layer = 8;
            var body = root.AddComponent<Rigidbody>(); body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.interpolation = RigidbodyInterpolation.Interpolate;
            var tip = root.AddComponent<SphereCollider>(); tip.radius = .018f; tip.center = new Vector3(0, 0, .235f);
            root.AddComponent<Projectile>(); root.AddComponent<DartFlight>();
            PrefabUtility.InstantiatePrefab(visual, root.transform);
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 8;
            return Save(root, "Prefabs/Projectiles/DartProjectile.prefab").GetComponent<Projectile>();
        }

        static void MakeStage(StageData data)
        {
            var root = new GameObject("DartsStage"); var stage = root.AddComponent<StageView>(); stage.data = data;
            var room = Group(root.transform, "The Copper Flight Bar", Vector3.zero);
            Part(room, "Floor", new Vector3(0, -.13f, -1.5f), new Vector3(14, .25f, 13), "Walnut", solid: true);
            for (int i = -11; i <= 11; i++)
                Part(room, "Oak floorboard " + i, new Vector3(i * .6f, .002f, -1.5f), new Vector3(.575f, .012f, 13), i % 3 == 0 ? "Walnut" : "Oak");
            Part(room, "Back wall", new Vector3(0, 2.1f, 3.4f), new Vector3(14, 4.2f, .25f), "Plaster", solid: true);
            Part(room, "Ceiling", new Vector3(0, 4.25f, -1.5f), new Vector3(14, .22f, 13), "Walnut");
            foreach (int side in new[] { -1, 1 })
            {
                Part(room, "Side wall", new Vector3(side * 6.8f, 2.1f, -1.5f), new Vector3(.2f, 4.2f, 13), "Plaster", solid: true);
                Part(room, "Wall wainscot", new Vector3(side * 6.65f, .7f, -1.5f), new Vector3(.16f, 1.4f, 13), "Walnut");
                Part(room, "Brass wall rail", new Vector3(side * 6.5f, 1.4f, -1.5f), new Vector3(.04f, .045f, 13), "Brass");
                for (int i = 0; i < 3; i++)
                {
                    float z = -3 + i * 2.6f;
                    Part(room, "Ceiling beam", new Vector3(0, 4.03f, z), new Vector3(13.5f, .25f, .2f), "Oak");
                    var lamp = Group(room, "Pendant", new Vector3(side * 3.3f, 3.45f, z));
                    Part(lamp, "Cable", new Vector3(0, .4f, 0), new Vector3(.025f, .8f, .025f), "Black");
                    Part(lamp, "Shade", Vector3.zero, new Vector3(.65f, .12f, .65f), "Brass", PrimitiveType.Cylinder);
                    Part(lamp, "Warm diffuser", new Vector3(0, -.13f, 0), new Vector3(.46f, .045f, .46f), "Amber", PrimitiveType.Cylinder);
                    var light = new GameObject("Warm light").AddComponent<Light>(); light.transform.SetParent(lamp, false);
                    light.transform.localPosition = new Vector3(0, -.25f, 0); light.type = LightType.Point;
                    light.color = new Color(1, .65f, .32f); light.range = 5; light.intensity = 2;
                }
            }
            Part(room, "Back paneling", new Vector3(0, .55f, 3.24f), new Vector3(14, 1.1f, .12f), "Walnut");
            for (int i = -10; i <= 10; i++)
                Part(room, "Panel seam", new Vector3(i * .65f, .55f, 3.16f), new Vector3(.035f, 1.05f, .025f), "Oak");
            Part(room, "Throw line", new Vector3(0, .016f, -4.3f), new Vector3(2.1f, .015f, .09f), "Brass");
            MakeBar(room); MakeSeating(room);
            var target = Group(root.transform, "Dartboard", new Vector3(0, 1.9f, 2.95f));
            target.gameObject.AddComponent<TargetMarker>(); stage.movingTarget = target; stage.authoredRadius = data.targetRadius;
            Part(target, "Walnut backboard", new Vector3(0, 0, .15f), new Vector3(2.65f, 2.7f, .16f), "Walnut", solid: true);
            Part(target, "Brass surround", new Vector3(0, 0, .03f), new Vector3(2.55f, .06f, 2.55f), "Brass", PrimitiveType.Cylinder, angles: new Vector3(90, 0, 0));
            Part(target, "Number rim", Vector3.zero, new Vector3(2.47f, .04f, 2.47f), "Black", PrimitiveType.Cylinder, angles: new Vector3(90, 0, 0));
            var face = Group(target, "Twenty sector face", new Vector3(0, 0, -.045f));
            face.gameObject.AddComponent<MeshFilter>().sharedMesh = BoardMesh();
            face.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { Mat("Black"), Mat("Ivory"), Mat("Red"), Mat("Green"), Mat("Steel") };
            var impact = target.gameObject.AddComponent<BoxCollider>(); impact.center = new Vector3(0, 0, .04f); impact.size = new Vector3(2.42f, 2.42f, .16f);
            int[] numbers = {20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5};
            for (int i = 0; i < 20; i++)
            {
                float angle = i * Mathf.PI / 10;
                Label(target, "Number " + numbers[i], numbers[i].ToString(), new Vector3(Mathf.Sin(angle) * 1.13f, Mathf.Cos(angle) * 1.13f, -.065f), .13f, Mat("Ivory").color);
            }
            var gate = Group(target, "Red bullseye score plane", new Vector3(0, 0, -.10f)).gameObject.AddComponent<ScoreGate>();
            gate.entryNormal = Vector3.back; gate.stage = stage; stage.gate = gate;
            AddTargetOutline(target);
            Label(room, "Bar sign", "THE COPPER FLIGHT", new Vector3(0, 3.7f, 3.1f), .36f, new Color(.3f, 1, .8f));
            Label(room, "Target instruction", "HIT THE YELLOW TARGET", new Vector3(0, .37f, 3.08f), .17f, new Color(1, .74f, .34f));
            Save(root, "Prefabs/Stages/DartsStage.prefab");
        }

        static void AddTargetOutline(Transform target) => DartboardRegionAuthoring.Configure(target);

        static void MakeBar(Transform room)
        {
            Part(room, "Bar counter", new Vector3(-4.2f, .64f, .65f), new Vector3(3.8f, 1.28f, 1.35f), "Walnut", solid: true);
            Part(room, "Polished countertop", new Vector3(-4.2f, 1.32f, .65f), new Vector3(4.05f, .14f, 1.55f), "Oak", solid: true);
            Part(room, "Brass counter trim", new Vector3(-4.2f, 1.29f, -.15f), new Vector3(4.05f, .06f, .05f), "Brass");
            for (int shelf = 0; shelf < 2; shelf++)
            {
                float y = 1.9f + shelf * .8f;
                Part(room, "Bottle shelf", new Vector3(-4.2f, y, 3.0f), new Vector3(3.8f, .12f, .45f), "Oak");
                for (int i = 0; i < 7; i++) Bottle(room, new Vector3(-5.7f + i * .5f, y + .06f, 2.95f), i);
            }
            for (int i = 0; i < 3; i++)
            {
                Bottle(room, new Vector3(-5.1f + i * .8f, 1.4f, .8f), i);
                Stool(room, new Vector3(-5.2f + i * 1.1f, 0, -.8f));
            }
        }

        static void Bottle(Transform room, Vector3 position, int index)
        {
            var bottle = Group(room, "Bar bottle " + index, position);
            string color = index % 3 == 0 ? "Green" : index % 3 == 1 ? "Burgundy" : "Brass";
            Part(bottle, "Body", new Vector3(0, .19f, 0), new Vector3(.17f, .19f, .17f), color, PrimitiveType.Cylinder);
            Part(bottle, "Neck", new Vector3(0, .43f, 0), new Vector3(.075f, .08f, .075f), color, PrimitiveType.Cylinder);
            Part(bottle, "Cream label", new Vector3(0, .2f, -.087f), new Vector3(.12f, .16f, .012f), "Ivory");
            Part(bottle, "Cap", new Vector3(0, .52f, 0), new Vector3(.085f, .025f, .085f), "Brass", PrimitiveType.Cylinder);
        }

        static void Stool(Transform room, Vector3 pos)
        {
            var stool = Group(room, "Bar stool", pos);
            Part(stool, "Cushion", new Vector3(0, .77f, 0), new Vector3(.56f, .08f, .56f), "Burgundy", PrimitiveType.Cylinder, true);
            Part(stool, "Pedestal", new Vector3(0, .36f, 0), new Vector3(.09f, .36f, .09f), "Brass", PrimitiveType.Cylinder, true);
            Part(stool, "Foot", new Vector3(0, .04f, 0), new Vector3(.5f, .025f, .5f), "Black", PrimitiveType.Cylinder);
        }

        static void MakeSeating(Transform room)
        {
            for (int i = 0; i < 2; i++)
            {
                var table = Group(room, "Pub table", new Vector3(4.2f, 0, 1.2f - i * 3));
                Part(table, "Round top", new Vector3(0, 1.05f, 0), new Vector3(1.5f, .07f, 1.5f), "Oak", PrimitiveType.Cylinder, true);
                Part(table, "Base", new Vector3(0, .5f, 0), new Vector3(.15f, .5f, .15f), "Black", PrimitiveType.Cylinder);
                Bottle(table, new Vector3(.2f, 1.12f, .1f), i);
                Stool(room, new Vector3(3.2f, 0, 1.2f - i * 3));
                Stool(room, new Vector3(5.2f, 0, 1.2f - i * 3));
            }
            Part(room, "Chalkboard frame", new Vector3(4.35f, 2.7f, 3.14f), new Vector3(2.45f, 1.5f, .12f), "Oak");
            Part(room, "Chalkboard", new Vector3(4.35f, 2.7f, 3.06f), new Vector3(2.27f, 1.32f, .05f), "Black");
            Label(room, "Chalkboard lettering", "TONIGHT\nDARTS CLUB\nFOLLOW YELLOW", new Vector3(4.35f, 2.7f, 3.01f), .23f, new Color(.96f, .88f, .67f));
        }

        static void Label(Transform parent, string name, string text, Vector3 pos, float size, Color color)
        {
            var label = Group(parent, name, pos).gameObject.AddComponent<TextMeshPro>();
            label.font = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/UI/GameHUD.prefab").GetComponent<GameHUD>().feedbackText.font;
            label.text = text; label.fontSize = size * 10; label.alignment = TextAlignmentOptions.Center;
            label.color = color; label.rectTransform.sizeDelta = new Vector2(6, 2);
        }

        static Mesh BoardMesh()
        {
            var vertices = new List<Vector3>(); var triangles = Enumerable.Range(0, 5).Select(_ => new List<int>()).ToArray();
            void Ring(float inner, float outer, float start, float end, int material)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt((end - start) * 80 / (Mathf.PI * 2)));
                for (int step = 0; step < steps; step++)
                {
                    float a = Mathf.Lerp(start, end, step / (float)steps), b = Mathf.Lerp(start, end, (step + 1) / (float)steps);
                    int n = vertices.Count;
                    float z = material == 4 ? -.001f : 0;
                    vertices.Add(new Vector3(Mathf.Sin(a) * inner, Mathf.Cos(a) * inner, z));
                    vertices.Add(new Vector3(Mathf.Sin(a) * outer, Mathf.Cos(a) * outer, z));
                    vertices.Add(new Vector3(Mathf.Sin(b) * outer, Mathf.Cos(b) * outer, z));
                    vertices.Add(new Vector3(Mathf.Sin(b) * inner, Mathf.Cos(b) * inner, z));
                    triangles[material].AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
                }
            }
            for (int i = 0; i < 20; i++)
            {
                float a = (i - .5f) * Mathf.PI / 10, b = (i + .5f) * Mathf.PI / 10;
                Ring(.2f, .29f, a, b, 3); Ring(.29f, .59f, a, b, i % 2);
                Ring(.59f, .65f, a, b, i % 2 == 0 ? 2 : 3);
                Ring(.65f, .95f, a, b, i % 2); Ring(.95f, 1.015f, a, b, i % 2 == 0 ? 2 : 3);
                Ring(.29f, 1.015f, a - .0015f, a + .0015f, 4);
                Ring(0, .198f, a, b, 2);
            }
            foreach (float r in new[] { .2f, .29f, .59f, .65f, .95f, 1.015f }) Ring(r - .002f, r + .002f, 0, Mathf.PI * 2, 4);
            var mesh = new Mesh { name = "Dartboard", subMeshCount = 5 };
            mesh.SetVertices(vertices); for (int i = 0; i < 5; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Root + "Meshes/Dartboard.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing)
            {
                // Refresh native buffers and submesh ranges together; copying serialized data
                // alone can leave old GPU ranges and show another sector's material.
                existing.Clear(); existing.vertices = mesh.vertices; existing.normals = mesh.normals;
                existing.subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++) existing.SetTriangles(mesh.GetTriangles(i), i);
                existing.RecalculateBounds(); existing.UploadMeshData(false); EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh); return existing;
            }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        static void AddHeld(ThrowController controller, GameObject visual)
        {
            var held = controller.view.transform.Find("Held Dart");
            if (!held)
            {
                held = Group(controller.view.transform, "Held Dart", new Vector3(.34f, -.24f, .65f));
                held.localRotation = Quaternion.Euler(-8, -15, -12); held.localScale = Vector3.one * 1.5f;
                PrefabUtility.InstantiatePrefab(visual, held);
            }
            foreach (var child in held.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 9;
            held.gameObject.SetActive(false);
            controller.heldItems = controller.heldItems.Where(item => item && item != held.gameObject).Concat(new[] { held.gameObject }).ToArray();
        }

        static void UpdateMenu(GameObject root)
        {
            foreach (var label in root.GetComponent<GameHUD>().menuPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.text.Contains("FIVE STAGES")) label.text = label.text.Replace("FIVE STAGES", "SIX STAGES");
                if (label.name == "StageLegend") label.text = "ROOM / STREET / COURT / MIDWAY / DOORSTEP / BAR\nCLEAN +150 / REBOUND +30 ON GOAL";
            }
        }

        static void UpdateScene(GameObject visual)
        {
            string path = Root + "Scenes/TossRush.unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<GameDirector>(true)).Single();
                var darts = game.stages.FirstOrDefault(s => s.data && s.data.name == "Darts");
                if (!darts)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Stages/DartsStage.prefab"), scene);
                    darts = instance.GetComponent<StageView>(); darts.transform.SetPositionAndRotation(game.stages[0].transform.position, game.stages[0].transform.rotation);
                    darts.gameObject.SetActive(false);
                    game.stages = game.stages.Concat(new[] { darts }).ToArray();
                }
                AddHeld(game.thrower, visual); UpdateMenu(game.hud.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(game.thrower);
                EditorUtility.SetDirty(game); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
