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
    // Rebuildable authored assets: the special stage stays outside the regular rotation.
    public static class NightMarketUpdate
    {
        const string Root = "Assets/TossRush/";
        const string Palette = Root + "Materials/NightMarket/";
        public static readonly int[] RowPoints = { 20, 50, 100, 250, 500, 1000 };
        public static readonly int[] RowSpacing = { 1, 1, 2, 2, 3, 3 };
        public static readonly int[] RowCounts = { 11, 11, 7, 7, 5, 5 };
        public static readonly float[] RowHeights = { .20f, .65f, 1.10f, 1.55f, 2.00f, 2.45f };
        static readonly float[] Depths = { -1.8f, -.65f, .85f, 2.4f, 4.1f, 5.8f };
        static readonly string[] Names = { "SMALL FIGURES", "LARGE FIGURES", "WATER BOTTLES", "TIN CANS", "BEER CANS", "PREMIUM BOTTLES" };
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Palette + name + ".mat");

        [MenuItem("TossRush/Add Night Market Special Mode")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!AssetDatabase.IsValidFolder(Palette.TrimEnd('/'))) AssetDatabase.CreateFolder(Root + "Materials", "NightMarket");
            MakePalette();
            var visual = MakeRingVisual();
            var projectile = MakeProjectile(visual);
            var data = LoadOrCreate<StageData>("Data/NightMarket.asset");
            data.title = "NIGHT MARKET / RING TOSS";
            data.instruction = "FOLLOW THE LANDING RING / GREEN = READY / LEFT CLICK TO THROW";
            data.accent = new Color(1, .68f, .23f);
            data.ringToss = true; data.changingWind = true; data.windChangeSpeed = .18f;
            data.points = 20; data.cleanShotBonus = 0; data.pointsPerPropCollision = 0;
            data.launchSpeed = 15.5f; data.throwLoft = 18; data.horizontalThrow = false;
            data.gravityScale = 1; data.projectileRadius = .03f; data.targetRadius = 1;
            data.randomize = false; data.motionAmplitude = 0; data.verticalMotionAmplitude = 0;
            data.wind = new Vector3(.9f, 0, .2f); data.windSpeedRange = new Vector2(.5f, 2.1f);
            data.windInfluence = 1; data.windLineWidthMultiplier = .8f; data.throwInterval = .32f;
            data.projectilePrefab = projectile; data.projectileEulerAngles = new Vector3(-18, 0, 0);
            var profile = LoadOrCreate<ProjectilePhysicsProfile>("Data/Physics/PlasticRing.asset");
            profile.displayName = "Lightweight plastic ring"; profile.mass = .075f;
            profile.referenceArea = .006f; profile.dragCoefficient = .75f;
            profile.initialSpin = Vector3.zero; profile.angularDamping = .3f; profile.flightLifetime = 5;
            var surface = LoadOrCreateSurface(); profile.surfaceMaterial = surface;
            data.physicsProfile = profile;
            MakeStage(data);
            EditPrefab("Prefabs/PlayerView.prefab", root => data.heldItemIndex = AddHeld(root.GetComponent<ThrowController>(), visual));
            EditPrefab("Prefabs/UI/GameHUD.prefab", root => ConfigureMenu(root.GetComponent<GameHUD>()));
            EditorUtility.SetDirty(data); EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            UpdateScene(visual, data);
            Debug.Log("Night market special mode authored: six staggered prize rows, separate menu entry and ring projectile.");
        }

        static T LoadOrCreate<T>(string relative) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Root + relative);
            if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, Root + relative); }
            return asset;
        }

        static PhysicsMaterial LoadOrCreateSurface()
        {
            string path = Root + "Data/Physics/PlasticRingSurface.physicMaterial";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (!material) { material = new PhysicsMaterial("Plastic ring surface"); AssetDatabase.CreateAsset(material, path); }
            material.dynamicFriction = .55f; material.staticFriction = .6f; material.bounciness = .16f;
            EditorUtility.SetDirty(material); return material;
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

        static void MakePalette()
        {
            ColorMat("Ink", new Color(.025f, .035f, .09f));
            ColorMat("Indigo", new Color(.09f, .13f, .26f));
            ColorMat("Asphalt", new Color(.16f, .18f, .25f));
            ColorMat("Paving", new Color(.27f, .3f, .36f));
            ColorMat("Wood", new Color(.42f, .22f, .13f));
            ColorMat("Cream", new Color(.97f, .89f, .69f));
            ColorMat("Red", new Color(.89f, .13f, .20f));
            ColorMat("Teal", new Color(.06f, .57f, .56f));
            ColorMat("Pink", new Color(.98f, .37f, .59f));
            ColorMat("Gold", new Color(1, .68f, .20f));
            ColorMat("Steel", new Color(.68f, .76f, .79f));
            ColorMat("Water", new Color(.28f, .68f, .91f));
            ColorMat("Green", new Color(.13f, .51f, .28f));
            ColorMat("Amber", new Color(.65f, .25f, .07f));
            ColorMat("Ring", new Color(1, .44f, .15f), true);
            ColorMat("Glow", new Color(1, .80f, .35f), true);
            ColorMat("Neon", new Color(.25f, 1, .86f), true);
            ColorMat("NeonPink", new Color(1, .28f, .61f), true);
            if (!Mat("AimGuide")) AssetDatabase.CreateAsset(new Material(Shader.Find("TossRush/Ring Guide")), Palette + "AimGuide.mat");
        }

        static Transform Group(Transform parent, string name, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }

        static GameObject Part(Transform parent, string name, Vector3 position, Vector3 size, string material,
            PrimitiveType primitive = PrimitiveType.Cube, bool solid = false, Vector3 angles = default)
        {
            var part = GameObject.CreatePrimitive(primitive); part.name = name;
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

        static void EditPrefab(string relative, Action<GameObject> action)
        {
            var root = PrefabUtility.LoadPrefabContents(Root + relative);
            try { action(root); PrefabUtility.SaveAsPrefabAsset(root, Root + relative); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Mesh RingMesh()
        {
            string path = Root + "Meshes/PlasticRing.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) return existing;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int segments = 64, sides = 10;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                for (int j = 0; j <= sides; j++)
                {
                    float b = j * Mathf.PI * 2 / sides;
                    float r = .26f + .03f * Mathf.Cos(b);
                    vertices.Add(new Vector3(Mathf.Cos(a) * r, .03f * Mathf.Sin(b), Mathf.Sin(a) * r));
                    if (i == segments || j == sides) continue;
                    int n = i * (sides + 1) + j;
                    triangles.AddRange(new[] { n, n + 1, n + sides + 1, n + 1, n + sides + 2, n + sides + 1 });
                }
            }
            var mesh = new Mesh { name = "Plastic ring, 0.58m diameter" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        static GameObject MakeRingVisual()
        {
            var root = new GameObject("PlasticRingVisual", typeof(MeshFilter), typeof(MeshRenderer));
            root.GetComponent<MeshFilter>().sharedMesh = RingMesh(); root.GetComponent<MeshRenderer>().sharedMaterial = Mat("Ring");
            return Save(root, "Prefabs/Projectiles/PlasticRingVisual.prefab");
        }

        static Projectile MakeProjectile(GameObject visual)
        {
            var root = new GameObject("PlasticRing"); root.layer = 8;
            var surface = LoadOrCreateSurface();
            var body = root.AddComponent<Rigidbody>(); body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            // One segment lives on the root for the common Projectile surface-material API.
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2 / 32;
                var segment = i == 0 ? root : Group(root.transform, "Ring collider " + i, Vector3.zero).gameObject;
                segment.layer = 8;
                var sphere = segment.AddComponent<SphereCollider>(); sphere.radius = .03f; sphere.sharedMaterial = surface;
                sphere.center = new Vector3(Mathf.Cos(a) * .26f, 0, Mathf.Sin(a) * .26f);
            }
            root.AddComponent<Projectile>(); root.AddComponent<RingFlight>();
            PrefabUtility.InstantiatePrefab(visual, root.transform);
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 8;
            return Save(root, "Prefabs/Projectiles/PlasticRing.prefab").GetComponent<Projectile>();
        }

        static void MakeStage(StageData data)
        {
            var root = new GameObject("NightMarketStage"); var stage = root.AddComponent<StageView>(); stage.data = data;
            stage.authoredRadius = 1;
            var targetRoot = Group(root.transform, "Six staggered prize rows", Vector3.zero); stage.movingTarget = targetRoot;
            MakeStreet(root.transform);
            MakeBooth(root.transform);
            for (int row = 0; row < 6; row++)
            {
                var rowRoot = Group(targetRoot, "Row " + (row + 1) + " - " + RowPoints[row] + " points", new Vector3(0, RowHeights[row], Depths[row]));
                float spacing = .58f * RowSpacing[row];
                float stagger = row % 2 == 1 ? spacing * .5f : 0;
                // Solid stepped shelves rise toward the back, like a night-market prize display.
                float shelfTop = RowHeights[row] - .12f;
                Part(rowRoot, "Tier " + (row + 1) + " support", new Vector3(0, -RowHeights[row] + shelfTop * .5f, 0),
                    new Vector3(9.6f, shelfTop, .88f), row % 2 == 0 ? "Teal" : "Indigo", solid: true);
                Part(rowRoot, "Tier front trim", new Vector3(0, -.145f, -.448f), new Vector3(9.6f, .045f, .02f), "Gold");
                for (int column = 0; column < RowCounts[row]; column++)
                {
                    float x = (column - (RowCounts[row] - 1) * .5f) * spacing + stagger;
                    var target = Group(rowRoot, Names[row] + " " + (column + 1), new Vector3(x, 0, 0));
                    var scoring = target.gameObject.AddComponent<RingTarget>();
                    scoring.stage = stage; scoring.points = RowPoints[row];
                    MakePrize(target, row, column, scoring);
                    Part(target, "Prize plinth", new Vector3(0, -.06f, 0), new Vector3(.32f, .12f, .32f), row % 2 == 0 ? "Teal" : "Red", solid: true);
                }
                float markerX = -(RowCounts[row] - 1) * .5f * spacing + stagger - .48f;
                float markerY = row == 0 ? .48f : .10f;
                Part(rowRoot, "Row marker", new Vector3(markerX, markerY, -.36f), new Vector3(.72f, .22f, .04f), "Ink");
                Label(rowRoot, "Points " + RowPoints[row], RowPoints[row].ToString(), new Vector3(markerX, markerY + .01f, -.39f), .24f, Mat("Glow").color, new Vector2(1, .4f));
            }
            AddAtmosphere(root);
            Save(root, "Prefabs/Stages/NightMarketStage.prefab");
        }

        static void MakePrize(Transform target, int row, int index, RingTarget scoring)
        {
            string accent = index % 3 == 0 ? "Pink" : index % 3 == 1 ? "Teal" : "Gold";
            if (row < 2)
            {
                float scale = row == 0 ? .72f : 1;
                Part(target, "Figure body", new Vector3(0, .14f * scale, 0), new Vector3(.18f, .25f, .16f) * scale, accent, PrimitiveType.Capsule, true);
                Part(target, "Figure head", new Vector3(0, .34f * scale, 0), Vector3.one * .23f * scale, accent, PrimitiveType.Sphere, true);
                foreach (int side in new[] { -1, 1 })
                {
                    Part(target, "Round ear", new Vector3(side * .075f * scale, .44f * scale, 0), Vector3.one * .08f * scale, accent, PrimitiveType.Sphere, true);
                    Part(target, "Eye", new Vector3(side * .045f * scale, .35f * scale, -.103f * scale), Vector3.one * .026f * scale, "Ink", PrimitiveType.Sphere);
                }
                Part(target, "Muzzle", new Vector3(0, .30f * scale, -.105f * scale), new Vector3(.085f, .045f, .025f) * scale, "Cream", PrimitiveType.Sphere);
                scoring.captureHeight = .48f * scale - .03f; scoring.captureRadius = .125f * scale;
                return;
            }
            if (row == 3 || row == 4)
            {
                float height = row == 3 ? .29f : .39f;
                Part(target, row == 3 ? "Tin can" : "Beer can", new Vector3(0, height * .5f, 0), new Vector3(.16f, height * .5f, .16f), row == 3 ? "Red" : "Green", PrimitiveType.Cylinder, true);
                foreach (float y in new[] { .015f, height - .015f })
                    Part(target, "Silver rim", new Vector3(0, y, 0), new Vector3(.168f, .012f, .168f), "Steel", PrimitiveType.Cylinder);
                Part(target, "Can label", new Vector3(0, height * .48f, -.082f), new Vector3(.115f, height * .38f, .008f), "Cream");
                Label(target, "Can lettering", row == 3 ? "POP" : "BEER", new Vector3(0, height * .48f, -.09f), .045f, Mat("Ink").color, new Vector2(.15f, .1f));
                scoring.captureHeight = height - .04f; scoring.captureRadius = .085f;
                return;
            }
            bool premium = row == 5;
            float bodyHeight = premium ? .38f : .28f, neck = premium ? .16f : .10f;
            Part(target, premium ? "Amber premium bottle" : "Blue water bottle", new Vector3(0, bodyHeight * .5f, 0), new Vector3(.16f, bodyHeight * .5f, .16f), premium ? "Amber" : "Water", PrimitiveType.Cylinder, true);
            Part(target, "Bottle shoulder", new Vector3(0, bodyHeight, 0), new Vector3(.16f, .12f, .16f), premium ? "Amber" : "Water", PrimitiveType.Sphere, true);
            Part(target, "Bottle neck", new Vector3(0, bodyHeight + neck * .5f, 0), new Vector3(.075f, neck * .5f, .075f), premium ? "Amber" : "Water", PrimitiveType.Cylinder, true);
            Part(target, "Bottle cap", new Vector3(0, bodyHeight + neck, 0), new Vector3(.085f, .022f, .085f), premium ? "Gold" : "Cream", PrimitiveType.Cylinder, true);
            Part(target, "Bottle label", new Vector3(0, bodyHeight * .5f, -.082f), new Vector3(.12f, .15f, .012f), "Cream");
            Label(target, "Bottle lettering", premium ? "GOLD" : "H2O", new Vector3(0, bodyHeight * .5f, -.091f), .04f, Mat("Ink").color, new Vector2(.15f, .1f));
            scoring.captureHeight = bodyHeight + neck - .008f; scoring.captureRadius = .055f;
        }

        static void MakeStreet(Transform root)
        {
            Part(root, "Night street", new Vector3(0, -.16f, 3), new Vector3(30, .3f, 34), "Asphalt", solid: true);
            for (int x = -12; x <= 12; x++) for (int z = -7; z <= 13; z++)
                Part(root, "Paving tile", new Vector3(x * .7f, .002f, z * .7f), new Vector3(.68f, .015f, .68f), (x + z) % 4 == 0 ? "Asphalt" : "Paving");
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, "Shopfront", new Vector3(side * 10, 3.7f, 6), new Vector3(6, 7.4f, 28), "Indigo");
                for (int floor = 0; floor < 3; floor++) for (int bay = 0; bay < 5; bay++)
                    Part(root, "Lit upstairs window", new Vector3(side * 6.95f, 2.1f + floor * 1.8f, -3 + bay * 4), new Vector3(.04f, .8f, 1.1f), (bay + floor) % 3 == 0 ? "Glow" : "Ink");
                MakeFoodStall(root, side, -1.5f, side < 0 ? "TAIWAN TEA" : "NIGHT SNACKS", side < 0 ? "Teal" : "Red");
                MakeFoodStall(root, side, 5.4f, side < 0 ? "GRILLED CORN" : "STINKY TOFU", side < 0 ? "Red" : "Teal");
            }
            Part(root, "Throw line", new Vector3(0, .021f, -4.5f), new Vector3(3.5f, .02f, .09f), "Glow");
        }

        static void MakeBooth(Transform root)
        {
            Part(root, "Prize court", new Vector3(0, -.015f, 2), new Vector3(9.8f, .03f, 9.5f), "Wood", solid: true);
            Part(root, "Back wall", new Vector3(0, 3.05f, 7), new Vector3(10.4f, 6.1f, .15f), "Indigo", solid: true);
            Part(root, "Back wall inset", new Vector3(0, 3.15f, 6.90f), new Vector3(9.9f, 5.35f, .06f), "Ink");
            Label(root, "Market headline", "TAIWAN NIGHT MARKET", new Vector3(0, 5.55f, 6.78f), .45f, Mat("Neon").color, new Vector2(9, 1));
            Label(root, "Ring toss headline", "RING TOSS", new Vector3(0, 4.85f, 6.78f), .7f, Mat("Glow").color, new Vector2(9, 1));
            Label(root, "Points guide", "20 / 50 / 100 / 250 / 500 / 1000", new Vector3(0, 4.05f, 6.78f), .24f, Mat("Cream").color, new Vector2(9, .5f));
            Label(root, "Stall rules", "ONE RING PER PRIZE / 20 SECONDS", new Vector3(0, 3.63f, 6.78f), .19f, Mat("NeonPink").color, new Vector2(9, .5f));
            foreach (int side in new[] { -1, 1 })
            {
                Part(root, "Stall post", new Vector3(side * 5f, 3.1f, -.8f), new Vector3(.13f, 6.2f, .13f), "Red");
                Part(root, "Court side rail", new Vector3(side * 4.95f, .45f, 2), new Vector3(.10f, .9f, 9.5f), "Wood", solid: true);
                Part(root, "Neon side rail", new Vector3(side * 4.95f, .94f, 2), new Vector3(.08f, .035f, 9.5f), "NeonPink");
            }
            for (int stripe = 0; stripe < 20; stripe++)
            {
                float x = -4.75f + stripe * .5f;
                Part(root, "Striped canopy", new Vector3(x, 6.2f, 2), new Vector3(.5f, .12f, 10), stripe % 2 == 0 ? "Red" : "Cream");
                Part(root, "Canopy valance", new Vector3(x, 5.93f, -3.02f), new Vector3(.5f, .48f, .08f), stripe % 2 == 0 ? "Red" : "Cream");
            }
            for (int i = 0; i < 9; i++) Lantern(root, new Vector3(-4 + i, 5.4f, -.9f), i);
            for (int i = 0; i < 9; i++) Lantern(root, new Vector3(-4 + i, 5.7f, 5.1f), i + 1);
        }

        static void MakeFoodStall(Transform root, int side, float depth, string name, string color)
        {
            var stall = Group(root, name, new Vector3(side * 6.35f, 0, depth));
            Part(stall, "Food cart", new Vector3(0, .6f, 0), new Vector3(2.2f, 1.2f, 2.5f), color);
            Part(stall, "Serving counter", new Vector3(0, 1.25f, 0), new Vector3(2.4f, .12f, 2.7f), "Steel");
            Part(stall, "Illuminated shop sign", new Vector3(0, 2.85f, .2f), new Vector3(2.6f, .75f, .1f), "Ink");
            Label(stall, "Shop name", name, new Vector3(0, 2.85f, .13f), .22f, Mat("Glow").color, new Vector2(2.5f, .6f));
            foreach (int post in new[] { -1, 1 }) Part(stall, "Cart post", new Vector3(post, 1.8f, .7f), new Vector3(.08f, 2.8f, .08f), "Steel");
            Part(stall, "Cart awning", new Vector3(0, 3.3f, .2f), new Vector3(2.6f, .1f, 3), color);
            for (int tray = 0; tray < 3; tray++)
            {
                Part(stall, "Food tray", new Vector3(-.65f + tray * .65f, 1.35f, 0), new Vector3(.5f, .06f, .6f), "Cream");
                for (int food = 0; food < 3; food++) Part(stall, "Street food", new Vector3(-.65f + tray * .65f, 1.43f, -.18f + food * .17f), new Vector3(.33f, .1f, .11f), "Gold", PrimitiveType.Capsule, angles: new Vector3(0, 0, 90));
            }
        }

        static void Lantern(Transform root, Vector3 position, int index)
        {
            var lantern = Group(root, "Red paper lantern " + index, position);
            Part(lantern, "Paper shade", Vector3.zero, new Vector3(.36f, .44f, .36f), index % 2 == 0 ? "Red" : "Glow", PrimitiveType.Sphere);
            Part(lantern, "Gold cap", new Vector3(0, .21f, 0), new Vector3(.18f, .035f, .18f), "Gold", PrimitiveType.Cylinder);
            Part(lantern, "Tassel", new Vector3(0, -.34f, 0), new Vector3(.035f, .12f, .035f), "Gold", PrimitiveType.Cylinder);
            if (index % 3 == 0)
            {
                var light = Group(lantern, "Warm pool of light", new Vector3(0, -.3f, 0)).gameObject.AddComponent<Light>();
                light.type = LightType.Point; light.color = new Color(1, .57f, .29f); light.range = 5; light.intensity = 2.2f;
            }
        }

        static void AddAtmosphere(GameObject root)
        {
            string path = Palette + "NightSky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!sky) { sky = new Material(Shader.Find("TossRush/House Garden Sky")); AssetDatabase.CreateAsset(sky, path); }
            sky.SetColor("_SkyColor", new Color(.012f, .019f, .065f)); sky.SetColor("_HorizonColor", new Color(.12f, .08f, .21f)); EditorUtility.SetDirty(sky);
            var sun = Group(root.transform, "Market moonlight", Vector3.zero).gameObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(.58f, .68f, 1); sun.intensity = .75f;
            sun.transform.localRotation = Quaternion.Euler(48, -25, 0);
            var atmosphere = root.AddComponent<StageAtmosphere>(); atmosphere.sky = sky; atmosphere.sunlight = sun;
            atmosphere.ambient = new Color(.48f, .47f, .63f); atmosphere.viewingDistance = 80;
        }

        static void Label(Transform parent, string name, string text, Vector3 position, float size, Color color, Vector2 bounds)
        {
            var label = Group(parent, name, position).gameObject.AddComponent<TextMeshPro>();
            label.font = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/UI/GameHUD.prefab").GetComponent<GameHUD>().feedbackText.font;
            label.text = text; label.fontSize = size * 10; label.alignment = TextAlignmentOptions.Center;
            label.color = color; label.rectTransform.sizeDelta = bounds;
        }

        static int AddHeld(ThrowController controller, GameObject visual)
        {
            var held = controller.view.transform.Find("Held Ring");
            if (!held)
            {
                held = Group(controller.view.transform, "Held Ring", new Vector3(.34f, -.28f, 1.05f));
                PrefabUtility.InstantiatePrefab(visual, held);
            }
            held.localPosition = new Vector3(.34f, -.28f, 1.05f);
            held.localRotation = Quaternion.Euler(-18, 0, 0);
            held.localScale = Vector3.one * .72f;
            foreach (var child in held.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 9;
            held.gameObject.SetActive(false);
            var heldItems = controller.heldItems.Where(item => item && item != held.gameObject).ToList();
            int index = heldItems.Count; heldItems.Add(held.gameObject); controller.heldItems = heldItems.ToArray();
            var guide = controller.GetComponent<RingAimGuidance>();
            if (!guide) guide = controller.gameObject.AddComponent<RingAimGuidance>();
            guide.controller = controller; guide.guideMaterial = Mat("AimGuide");
            guide.maximumCorrectionDegrees = 1.8f;
            guide.AimAssistEnabled = false;
            controller.ringAim = guide;
            return index;
        }

        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        static UnityEngine.UI.Button Button(GameHUD hud, UnityEngine.UI.Button source, Transform parent, string name, string label)
        {
            var existing = parent.Find(name);
            var button = existing ? existing.GetComponent<UnityEngine.UI.Button>() : UnityEngine.Object.Instantiate(source, parent, false);
            button.name = name;
            // Callback binding is owned by GameHUD.Initialize.
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            var text = button.GetComponentInChildren<TMP_Text>(true); text.text = label; text.fontSize = 21;
            text.enableAutoSizing = true; text.fontSizeMin = 15; text.fontSizeMax = 21;
            return button;
        }

        static void ConfigureMenu(GameHUD hud)
        {
            Place((RectTransform)hud.startButton.transform, new Vector2(-180, -137), new Vector2(330, 66));
            hud.specialButton = Button(hud, hud.startButton, hud.menuPanel.transform, "SpecialModeButton", "SPECIAL MODE / RING TOSS");
            Place((RectTransform)hud.specialButton.transform, new Vector2(180, -137), new Vector2(330, 66));
            hud.specialButton.GetComponent<UnityEngine.UI.Image>().color = new Color(1, .68f, .23f);
            Place(hud.bestText.rectTransform, new Vector2(-180, -206), new Vector2(350, 36));
            hud.bestText.fontSize = 16;
            var best = hud.menuPanel.transform.Find("SpecialBest");
            hud.specialBestText = best ? best.GetComponent<TMP_Text>() : UnityEngine.Object.Instantiate(hud.bestText, hud.menuPanel.transform, false);
            hud.specialBestText.name = "SpecialBest";
            Place(hud.specialBestText.rectTransform, new Vector2(180, -206), new Vector2(350, 36));
            hud.specialBestText.text = "RING TOSS BEST / 0000";
            hud.specialBestText.fontSize = 16;
            Place((RectTransform)hud.restartButton.transform, new Vector2(-180, -170), new Vector2(330, 66));
            hud.menuButton = Button(hud, hud.restartButton, hud.resultPanel.transform, "MainMenuButton", "MAIN MENU");
            Place((RectTransform)hud.menuButton.transform, new Vector2(180, -170), new Vector2(330, 66));
            var aimPanel = hud.playPanel.transform.Find("RingAimStatus");
            if (!aimPanel)
            {
                var panel = new GameObject("RingAimStatus", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                panel.transform.SetParent(hud.playPanel.transform, false); aimPanel = panel.transform;
                var image = panel.GetComponent<UnityEngine.UI.Image>(); image.color = new Color(.015f, .025f, .055f, .85f); image.raycastTarget = false;
                hud.ringAimText = UnityEngine.Object.Instantiate(hud.hintText, panel.transform, false);
                hud.ringAimText.name = "RingAimText";
            }
            if (!hud.ringAimText) hud.ringAimText = aimPanel.GetComponentInChildren<TMP_Text>(true);
            var aimRect = (RectTransform)aimPanel;
            aimRect.anchorMin = aimRect.anchorMax = aimRect.pivot = new Vector2(.5f, 1);
            aimRect.anchoredPosition = new Vector2(0, -18); aimRect.sizeDelta = new Vector2(610, 76);
            Place(hud.ringAimText.rectTransform, Vector2.zero, new Vector2(590, 70));
            hud.ringAimText.text = "FOLLOW THE LANDING RING\nGREEN = READY TO THROW";
            hud.ringAimText.fontSize = 18; hud.ringAimText.enableAutoSizing = false;
            hud.ringAimText.alignment = TextAlignmentOptions.Center; hud.ringAimText.raycastTarget = false;
            aimPanel.gameObject.SetActive(false);
            EditorUtility.SetDirty(hud);
        }

        static void UpdateScene(GameObject visual, StageData data)
        {
            string path = Root + "Scenes/TossRush.unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameDirector>(true)).Single();
                var existing = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<StageView>(true)).FirstOrDefault(stage => stage.data == data);
                if (!existing)
                {
                    var stage = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Stages/NightMarketStage.prefab"), scene);
                    existing = stage.GetComponent<StageView>();
                    existing.transform.SetPositionAndRotation(game.stages[0].transform.position, game.stages[0].transform.rotation);
                }
                existing.gameObject.SetActive(false); game.specialStage = existing;
                data.heldItemIndex = AddHeld(game.thrower, visual); ConfigureMenu(game.hud);
                PrefabUtility.RecordPrefabInstancePropertyModifications(game.thrower);
                PrefabUtility.RecordPrefabInstancePropertyModifications(game.hud);
                EditorUtility.SetDirty(data); EditorUtility.SetDirty(game); AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
