using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TossRush.Editor
{
    // Authoring only: save reusable assets and scene references, never construct scenery at runtime.
    public static class NeighborhoodUpdate
    {
        const string Root = "Assets/TossRush/";
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/" + name + ".mat");
        static PhysicsMaterial BankSurface => AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "Materials/CornerBankSurface.physicMaterial");

        [MenuItem("TossRush/Apply Neighborhood And Rebound Update")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var newspaper = MakeNewspaper();
            var data = MakeDeliveryData(newspaper);
            var mailbox = MakeMailbox();
            MakeDoorstep(data, mailbox);
            MakeBottle();
            UpdateHeldNewspaper();
            ConfigureStreet();
            foreach (string name in new[] { "Corner", "Street", "Basketball", "Carnival", "Doorstep" })
            {
                var stageData = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/" + name + ".asset");
                stageData.cleanShotBonus = 150;
                stageData.pointsPerPropCollision = 30;
                stageData.minimumReboundSpeed = name == "Street" ? .1f : 1.5f;
                stageData.pedestrianHitPenalty = name == "Street" ? 10 : 0;
                stageData.instruction = name == "Doorstep" ? "NEWSPAPERS / AIM UP+DOWN / FIND THE MAILBOX" :
                    name == "Street" ? "TRASH BAG / LID REBOUND +30 / HIT PEDESTRIAN -10" :
                    name == "Carnival" ? "BOTTLE FLIP / CLEAN +150 / REBOUND +30 ON GOAL" :
                    "CLEAN +150 / STRONG REBOUND +30 / BANK ON GOAL";
                EditorUtility.SetDirty(stageData);
                Edit("Prefabs/Stages/" + name + "Stage.prefab", root =>
                {
                    DressStage(root);
                });
            }
            ConfigureTargetVariationData();
            Edit("Prefabs/UI/GameHUD.prefab", root =>
            {
                var hud = root.GetComponent<GameHUD>();
                foreach (var label in hud.menuPanel.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if (label.name == "StageLegend") label.text = "ROOM / STREET / COURT / MIDWAY / DOORSTEP\nCLEAN +150 / REBOUND +30 ON GOAL";
            });
            AssetDatabase.SaveAssets();
            UpdateScene();
            KnockdownSceneUpdate.Apply();
            EuropeanGardenUpdate.Apply();
            ThemedSceneryUpdate.Apply();
            WindAndProgressionUpdate.Apply();
            Debug.Log("Neighborhood update saved: five stages, threshold rebounds, varied props and walking crowd.");
        }

        static void Edit(string path, Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(Root + path);
            try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, Root + path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static GameObject Save(GameObject root, string path)
        {
            try { return PrefabUtility.SaveAsPrefabAsset(root, Root + path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static GameObject Part(Transform parent, string name, Vector3 pos, Vector3 size, string material,
            bool solid = false, PrimitiveType shape = PrimitiveType.Cube, Vector3 angles = default)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = size; go.transform.localEulerAngles = angles;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
            if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Projectile MakeNewspaper()
        {
            var visual = new GameObject("NewspaperStackVisual");
            for (int i = 0; i < 6; i++)
                Part(visual.transform, "Folded newspaper " + i, new Vector3((i % 2) * .012f, (i - 2.5f) * .024f, 0),
                    new Vector3(.46f, .021f, .32f), i % 2 == 0 ? "Cream" : "Street/Trim", false, angles: new Vector3(0, (i % 3 - 1) * 3, 0));
            Part(visual.transform, "Paper strap", new Vector3(0, .076f, 0), new Vector3(.045f, .008f, .33f), "Coral");
            Part(visual.transform, "Masthead", new Vector3(-.095f, .078f, -.085f), new Vector3(.15f, .005f, .045f), "Ink");
            for (int i = 0; i < 4; i++)
                Part(visual.transform, "Printed column " + i, new Vector3(-.16f + i * .095f, .078f, .025f), new Vector3(.065f, .005f, .12f), "Street/Secondary");
            var visualPrefab = Save(visual, "Prefabs/Projectiles/NewspaperStackVisual.prefab");
            var root = new GameObject("NewspaperStack"); root.layer = 8;
            PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
            root.AddComponent<BoxCollider>().size = new Vector3(.48f, .15f, .34f);
            var body = root.AddComponent<Rigidbody>(); body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            root.AddComponent<Projectile>();
            return Save(root, "Prefabs/Projectiles/NewspaperStack.prefab").GetComponent<Projectile>();
        }

        static StageData MakeDeliveryData(Projectile projectile)
        {
            string physicsPath = Root + "Data/Physics/NewspaperStack.asset";
            var physics = AssetDatabase.LoadAssetAtPath<ProjectilePhysicsProfile>(physicsPath);
            if (!physics) { physics = ScriptableObject.CreateInstance<ProjectilePhysicsProfile>(); AssetDatabase.CreateAsset(physics, physicsPath); }
            physics.displayName = "NEWSPAPER STACK"; physics.mass = .45f; physics.referenceArea = .018f;
            physics.dragCoefficient = .4f; physics.initialSpin = Vector3.zero; physics.angularDamping = .8f;
            physics.spinLift = 0; physics.flightLifetime = 4;
            physics.surfaceMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "Materials/PaperSurface.physicMaterial");
            EditorUtility.SetDirty(physics);
            string dataPath = Root + "Data/Doorstep.asset";
            var data = AssetDatabase.LoadAssetAtPath<StageData>(dataPath);
            if (!data) { data = ScriptableObject.CreateInstance<StageData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.title = "MORNING DELIVERY"; data.accent = new Color(1, .68f, .36f); data.points = 200;
            data.cleanShotBonus = 150; data.heldItemIndex = 4; data.physicsProfile = physics; data.projectilePrefab = projectile;
            data.launchSpeed = 34; data.throwLoft = 0; data.horizontalThrow = true; data.projectileRadius = .13f;
            data.targetRadius = .67f; data.gravityScale = 1; data.throwInterval = .45f;
            data.randomize = true; data.wind = Vector3.zero; data.windInfluence = 1;
            data.horizontalOffsetRange = new Vector2(-2.75f, 2.75f); data.minimumHorizontalDistanceFromCenter = 2.25f;
            data.minimumHorizontalChange = .12f; data.depthOffsetRange = Vector2.zero;
            data.windSpeedRange = Vector2.zero;
            EditorUtility.SetDirty(data); return data;
        }

        static GameObject MakeMailbox()
        {
            MailboxMaterial("MailboxIron", new Color(.035f, .12f, .095f));
            MailboxMaterial("MailboxRelief", new Color(.08f, .2f, .15f));
            MailboxMaterial("MailboxBrass", new Color(.57f, .48f, .29f));
            var root = new GameObject("MailboxTarget"); root.AddComponent<TargetMarker>();
            Part(root.transform, "Base", new Vector3(0, .045f, .04f), new Vector3(.5f, .09f, .45f), "Street/MailboxIron", true);
            Part(root.transform, "Stepped base", new Vector3(0, .115f, .04f), new Vector3(.36f, .06f, .32f), "Street/MailboxRelief", true);
            Part(root.transform, "Post", new Vector3(0, .35f, .04f), new Vector3(.18f, .23f, .18f), "Street/MailboxIron", true, PrimitiveType.Cylinder);
            foreach (float y in new[] { .17f, .51f, .58f })
                Part(root.transform, "Post collar", new Vector3(0, y, .04f), new Vector3(.25f, .025f, .25f), "Street/MailboxRelief", false, PrimitiveType.Cylinder);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Part(root.transform, "Post fluting " + i, new Vector3(Mathf.Cos(angle) * .09f, .35f, .04f + Mathf.Sin(angle) * .09f), new Vector3(.025f, .28f, .025f), "Street/MailboxRelief");
            }
            foreach (int side in new[] { -1, 1 })
            {
                Part(root.transform, "Support bracket " + side, new Vector3(side * .16f, .55f, .04f), new Vector3(.065f, .3f, .09f), "Street/MailboxRelief", false, angles: new Vector3(0, 0, -side * 48));
                Part(root.transform, side < 0 ? "Left wall" : "Right wall", new Vector3(side * .46f, 1.05f, 0), new Vector3(.1f, .9f, .66f), "Street/MailboxIron", true);
                Part(root.transform, "Side inset " + side, new Vector3(side * .515f, 1.04f, 0), new Vector3(.018f, .64f, .46f), "Street/MailboxRelief");
            }
            Part(root.transform, "Shelf", new Vector3(0, .62f, 0), new Vector3(1.04f, .09f, .7f), "Street/MailboxRelief", true);
            Part(root.transform, "Interior", new Vector3(0, 1.05f, .30f), new Vector3(.84f, .85f, .06f), "Street/MailboxIron", true);
            Part(root.transform, "Decorative front door", new Vector3(0, .87f, -.32f), new Vector3(.84f, .5f, .08f), "Street/MailboxIron", true);
            foreach (int side in new[] { -1, 1 })
            {
                Part(root.transform, "Door vertical molding " + side, new Vector3(side * .37f, .87f, -.37f), new Vector3(.035f, .43f, .025f), "Street/MailboxRelief");
                Part(root.transform, "Door horizontal molding " + side, new Vector3(0, .87f + side * .21f, -.37f), new Vector3(.76f, .035f, .025f), "Street/MailboxRelief");
            }
            Part(root.transform, "Brass letter plaque", new Vector3(0, 1.055f, -.38f), new Vector3(.55f, .09f, .025f), "Street/MailboxBrass");
            Part(root.transform, "Relief shield", new Vector3(0, .88f, -.38f), new Vector3(.19f, .22f, .025f), "Street/MailboxRelief", false, angles: new Vector3(0, 0, 45));
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                Part(root.transform, "Embossed wreath " + i, new Vector3(Mathf.Cos(a) * .19f, .87f + Mathf.Sin(a) * .15f, -.39f), new Vector3(.075f, .035f, .022f), "Street/MailboxRelief", false, PrimitiveType.Sphere, new Vector3(0, 0, i * 30));
            }
            Part(root.transform, "Slot lintel", new Vector3(0, 1.455f, -.32f), new Vector3(.87f, .09f, .08f), "Street/MailboxIron", true);
            foreach (int side in new[] { -1, 1 })
            {
                Part(root.transform, "Mail slot horizontal rim " + side, new Vector3(0, 1.27f + side * .1375f, -.375f), new Vector3(.805f, .015f, .018f), "Street/MailboxBrass");
                Part(root.transform, "Mail slot vertical rim " + side, new Vector3(side * .395f, 1.27f, -.375f), new Vector3(.015f, .29f, .018f), "Street/MailboxBrass");
            }
            Part(root.transform, "Roof eave", new Vector3(0, 1.51f, 0), new Vector3(1.16f, .07f, .82f), "Street/MailboxIron", true);
            MakeMailboxRoof(root.transform);
            Part(root.transform, "Finial collar", new Vector3(0, 1.735f, 0), new Vector3(.16f, .025f, .16f), "Street/MailboxBrass", false, PrimitiveType.Cylinder);
            Part(root.transform, "Finial", new Vector3(0, 1.81f, 0), new Vector3(.13f, .15f, .13f), "Street/MailboxIron", false, PrimitiveType.Sphere);
            Part(root.transform, "Finial tip", new Vector3(0, 1.9f, 0), new Vector3(.045f, .08f, .045f), "Street/MailboxRelief", false, PrimitiveType.Sphere);
            var gate = new GameObject("Mail slot").AddComponent<ScoreGate>(); gate.transform.SetParent(root.transform, false);
            gate.transform.localPosition = new Vector3(0, 1.265f, -.37f);
            gate.transform.localRotation = Quaternion.Euler(90, 0, 0);
            gate.entryNormal = Vector3.down; gate.openingHalfExtents = new Vector2(.41f, .145f); gate.rollInDepth = .5f;
            AddOutline(root);
            return Save(root, "Prefabs/Targets/MailboxTarget.prefab");
        }

        static void MailboxMaterial(string name, Color color)
        {
            string path = Root + "Materials/Street/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/StageToon.shader")); material.name = name; AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetColor("_ShadowTint", new Color(.6f, .67f, .65f));
            material.SetFloat("_Steps", 3); material.SetFloat("_Rim", .08f); EditorUtility.SetDirty(material);
        }

        static void MakeMailboxRoof(Transform parent)
        {
            Vector3[] lower = { new Vector3(-.57f,1.54f,-.4f), new Vector3(.57f,1.54f,-.4f), new Vector3(.57f,1.54f,.4f), new Vector3(-.57f,1.54f,.4f) };
            Vector3[] upper = { new Vector3(-.09f,1.72f,-.07f), new Vector3(.09f,1.72f,-.07f), new Vector3(.09f,1.72f,.07f), new Vector3(-.09f,1.72f,.07f) };
            var vertices = new Vector3[16]; var triangles = new int[24];
            for (int i = 0; i < 4; i++)
            {
                int v = i * 4, t = i * 6;
                vertices[v] = lower[i]; vertices[v+1] = upper[i]; vertices[v+2] = upper[(i+1)%4]; vertices[v+3] = lower[(i+1)%4];
                triangles[t] = v; triangles[t+1] = v+1; triangles[t+2] = v+2; triangles[t+3] = v; triangles[t+4] = v+2; triangles[t+5] = v+3;
            }
            var mesh = new Mesh { name = "EuropeanMailboxRoof", vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Root + "Meshes/Street/EuropeanMailboxRoof.asset";
            var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (asset) { EditorUtility.CopySerialized(mesh, asset); UnityEngine.Object.DestroyImmediate(mesh); }
            else { AssetDatabase.CreateAsset(mesh, path); asset = mesh; }
            var roof = new GameObject("Hipped cast iron roof", typeof(MeshFilter), typeof(MeshRenderer)); roof.transform.SetParent(parent, false);
            roof.GetComponent<MeshFilter>().sharedMesh = asset; roof.GetComponent<MeshRenderer>().sharedMaterial = Mat("Street/MailboxIron");
            for (int i = -3; i <= 3; i++)
            {
                Vector3 start = new Vector3(i * .13f, 1.545f, -.403f), end = new Vector3(i * .0195f, 1.725f, -.073f);
                Vector3 direction = end - start;
                Part(parent, "Roof raised rib " + i, (start + end) * .5f, new Vector3(.015f, .015f, direction.magnitude), "Street/MailboxRelief", false, angles: Quaternion.LookRotation(direction).eulerAngles);
            }
        }

        [MenuItem("TossRush/Apply Mailbox Bottle And Street Scoring")]
        public static void ApplyThrowRefinements()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            MakeBottle();
            MakeDeliveryData(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Projectiles/NewspaperStack.prefab").GetComponent<Projectile>());
            MakeMailbox();
            Edit("Prefabs/Stages/DoorstepStage.prefab", ConfigureDoorstepPlacement);
            ConfigureStreet();
            ConfigureTargetVariationData();
            AssetDatabase.SaveAssets();
            UpdateScene();
            KnockdownSceneUpdate.Apply();
        }

        static void ConfigureStreet()
        {
            var data = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/Street.asset");
            data.minimumReboundSpeed = .1f; data.pedestrianHitPenalty = 10;
            data.instruction = "TRASH BAG / LID REBOUND +30 / HIT PEDESTRIAN -10";
            EditorUtility.SetDirty(data);
            var lid = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "Materials/Street/OpenLidSurface.physicMaterial");
            lid.bounciness = .06f; lid.bounceCombine = PhysicsMaterialCombine.Maximum;
            EditorUtility.SetDirty(lid);
            Edit("Prefabs/Projectiles/TrashBag.prefab", root =>
            {
                var soft = root.GetComponent<SoftBagDeformation>();
                if (!soft) soft = root.AddComponent<SoftBagDeformation>();
                soft.maximumCompression = .5f; soft.reboundRetention = .55f; soft.maximumReboundSpeed = .22f;
            });
        }

        static void MakeBottle()
        {
            string materialPath = Root + "Materials/Street/BottlePlastic.mat";
            var plastic = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!plastic)
            {
                plastic = new Material(AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/StageToon.shader"));
                plastic.name = "BottlePlastic"; plastic.SetColor("_BaseColor", new Color(.4f, .8f, .9f));
                plastic.SetColor("_ShadowTint", new Color(.55f, .65f, .72f)); plastic.SetFloat("_Steps", 3);
                AssetDatabase.CreateAsset(plastic, materialPath);
            }
            var visual = new GameObject("PlasticBottleVisual");
            Part(visual.transform, "Bottle body", new Vector3(0, -.07f, 0), new Vector3(.28f, .2f, .28f), "Street/BottlePlastic", false, PrimitiveType.Cylinder);
            Part(visual.transform, "Shoulder", new Vector3(0, .13f, 0), new Vector3(.28f, .16f, .28f), "Street/BottlePlastic", false, PrimitiveType.Sphere);
            Part(visual.transform, "Neck", new Vector3(0, .235f, 0), new Vector3(.11f, .045f, .11f), "Street/BottlePlastic", false, PrimitiveType.Cylinder);
            Part(visual.transform, "Screw cap", new Vector3(0, .295f, 0), new Vector3(.13f, .025f, .13f), "Street/BinBlue", false, PrimitiveType.Cylinder);
            foreach (float y in new[] { -.21f, -.14f, .07f }) Part(visual.transform, "Bottle rib", new Vector3(0, y, 0), new Vector3(.287f, .006f, .287f), "Street/BottlePlastic", false, PrimitiveType.Cylinder);
            Part(visual.transform, "Label", new Vector3(0, -.035f, 0), new Vector3(.29f, .055f, .29f), "Cream", false, PrimitiveType.Cylinder);
            Part(visual.transform, "Label mark", new Vector3(0, -.035f, -.148f), new Vector3(.105f, .065f, .008f), "Coral");
            var visualPrefab = Save(visual, "Prefabs/Projectiles/PlasticBottleVisual.prefab");
            var root = new GameObject("PlasticBottle");
            PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
            var collider = root.AddComponent<CapsuleCollider>(); collider.direction = 1; collider.height = .62f; collider.radius = .145f; collider.center = new Vector3(0, .025f, 0);
            var body = root.AddComponent<Rigidbody>(); body.useGravity = false; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; root.AddComponent<Projectile>();
            var prefab = Save(root, "Prefabs/Projectiles/PlasticBottle.prefab").GetComponent<Projectile>();
            string surfacePath = Root + "Materials/Street/BottleSurface.physicMaterial";
            var surface = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(surfacePath);
            if (!surface) { surface = new PhysicsMaterial("BottleSurface"); AssetDatabase.CreateAsset(surface, surfacePath); }
            surface.dynamicFriction = .45f; surface.staticFriction = .5f; surface.bounciness = .08f;
            surface.bounceCombine = PhysicsMaterialCombine.Average; EditorUtility.SetDirty(surface);
            string physicsPath = Root + "Data/Physics/PlasticBottle.asset";
            var profile = AssetDatabase.LoadAssetAtPath<ProjectilePhysicsProfile>(physicsPath);
            if (!profile) { profile = ScriptableObject.CreateInstance<ProjectilePhysicsProfile>(); AssetDatabase.CreateAsset(profile, physicsPath); }
            profile.displayName = "PLASTIC BOTTLE"; profile.mass = .3f; profile.referenceArea = .014f; profile.dragCoefficient = .9f;
            profile.initialSpin = new Vector3(14, 0, 0); profile.angularDamping = .25f; profile.spinLift = 0;
            profile.flightLifetime = 4; profile.surfaceMaterial = surface; EditorUtility.SetDirty(profile);
            var data = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/Carnival.asset");
            data.projectilePrefab = prefab; data.physicsProfile = profile; data.projectileEulerAngles = Vector3.zero;
            data.instruction = "BOTTLE FLIP / CLEAN +150 / REBOUND +30 ON GOAL"; data.heldItemIndex = 2;
            EditorUtility.SetDirty(data);
            Edit("Prefabs/PlayerView.prefab", player =>
            {
                var controller = player.GetComponent<ThrowController>(); var held = controller.heldItems[2];
                foreach (Transform child in held.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                held.name = "Held Bottle"; held.transform.localPosition = new Vector3(.38f, -.27f, 1.05f);
                held.transform.localRotation = Quaternion.Euler(0, -15, -10);
                PrefabUtility.InstantiatePrefab(visualPrefab, held.transform);
                foreach (var t in held.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 9;
                held.SetActive(false);
            });
        }

        static void AddOutline(GameObject root)
        {
            // Match the existing stencil silhouette and 16-direction screen-space outline.
            var meshes = root.GetComponentsInChildren<MeshFilter>().Select(f => new CombineInstance
                { mesh = f.sharedMesh, transform = root.transform.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray();
            var fill = new Mesh(); fill.CombineMeshes(meshes);
            var vertices = fill.vertices; var indices = fill.triangles;
            var expanded = new Mesh { indexFormat = IndexFormat.UInt32 };
            var positions = new Vector3[vertices.Length * 16]; var uv = new Vector2[positions.Length]; var triangles = new int[indices.Length * 16];
            for (int k = 0; k < 16; k++)
            {
                float a = k * Mathf.PI * 2 / 16;
                for (int i = 0; i < vertices.Length; i++) { positions[k * vertices.Length + i] = vertices[i]; uv[k * vertices.Length + i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }
                for (int i = 0; i < indices.Length; i++) triangles[k * indices.Length + i] = indices[i] + k * vertices.Length;
            }
            expanded.vertices = positions; expanded.uv = uv; expanded.triangles = triangles; expanded.RecalculateBounds();
            for (int i = 0; i < 2; i++)
            {
                Mesh source = i == 0 ? fill : expanded;
                string name = i == 0 ? "MailboxMask" : "MailboxOutline";
                string path = Root + "Meshes/Street/" + name + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (asset) { EditorUtility.CopySerialized(source, asset); UnityEngine.Object.DestroyImmediate(source); }
                else { source.name = name; AssetDatabase.CreateAsset(source, path); asset = source; }
                var part = new GameObject(i == 0 ? "OutlineMask" : "Outline", typeof(MeshFilter), typeof(MeshRenderer));
                part.transform.SetParent(root.transform, false); part.GetComponent<MeshFilter>().sharedMesh = asset;
                var renderer = part.GetComponent<MeshRenderer>(); renderer.sharedMaterial = Mat(i == 0 ? "TargetSilhouetteMask" : "TargetOutline");
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static void MakeDoorstep(StageData data, GameObject target)
        {
            var root = new GameObject("DoorstepStage"); var stage = root.AddComponent<StageView>(); stage.data = data;
            Part(root.transform, "Front yard", new Vector3(0, -.16f, 1), new Vector3(28, .3f, 22), "Street/Foliage", true);
            Part(root.transform, "Delivery path", new Vector3(0, -.025f, -2), new Vector3(3.2f, .06f, 10), "Street/Stone", true);
            Part(root.transform, "House", new Vector3(0, 2.1f, 6.5f), new Vector3(8.5f, 4.2f, 4), "Street/Facade", true);
            foreach (int side in new[] { -1, 1 })
            {
                Part(root.transform, "Roof slope " + side, new Vector3(side * 2.1f, 4.7f, 6.5f), new Vector3(4.9f, .18f, 4.8f), "Coral", false, angles: new Vector3(0, 0, -side * 19));
                Part(root.transform, "Window " + side, new Vector3(side * 2.6f, 2.35f, 4.46f), new Vector3(1.5f, 1.5f, .08f), "Street/Trim");
                Part(root.transform, "Window glass " + side, new Vector3(side * 2.6f, 2.35f, 4.4f), new Vector3(1.25f, 1.25f, .08f), "Street/Glass");
                Part(root.transform, "Window cross " + side, new Vector3(side * 2.6f, 2.35f, 4.34f), new Vector3(.06f, 1.25f, .04f), "Street/Trim");
                Part(root.transform, "Porch column " + side, new Vector3(side * 1.5f, 1.5f, 3.4f), new Vector3(.17f, 3, .17f), "Street/Trim", true);
                for (int i = 0; i < 8; i++) Part(root.transform, "Picket " + side + " " + i, new Vector3(side * (2.8f + i * .72f), .57f, 2.6f), new Vector3(.32f, 1.14f, .14f), "Street/Trim", true);
                Part(root.transform, "Fence rail " + side, new Vector3(side * 5.3f, .65f, 2.65f), new Vector3(6, .14f, .14f), "Street/Trim", true);
            }
            Part(root.transform, "Front door", new Vector3(0, 1.35f, 4.44f), new Vector3(1.3f, 2.7f, .12f), "Street/BinBlue", true);
            Part(root.transform, "Doorknob", new Vector3(.43f, 1.3f, 4.32f), Vector3.one * .09f, "Gold", false, PrimitiveType.Sphere);
            Part(root.transform, "Porch canopy", new Vector3(0, 3.12f, 3.8f), new Vector3(3.5f, .18f, 1.8f), "Street/Secondary");
            Part(root.transform, "Welcome mat", new Vector3(0, .02f, 3.4f), new Vector3(1.1f, .04f, .6f), "Coral");
            Part(root.transform, "Chimney", new Vector3(2.6f, 4.7f, 7), new Vector3(.75f, 1.4f, .8f), "Street/Secondary");
            var mailbox = (GameObject)PrefabUtility.InstantiatePrefab(target, root.transform); mailbox.transform.localPosition = new Vector3(-2.5f, 0, 3.4f);
            stage.movingTarget = mailbox.transform; stage.gate = mailbox.GetComponentInChildren<ScoreGate>(); stage.gate.stage = stage;
            ConfigureDoorstepPlacement(root);
            Save(root, "Prefabs/Stages/DoorstepStage.prefab");
        }

        [MenuItem("TossRush/Update Scene Props And Sidewalk Crowd")]
        public static void ApplySceneDressing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            foreach (string name in new[] { "Corner", "Street", "Basketball", "Carnival", "Doorstep" })
                Edit("Prefabs/Stages/" + name + "Stage.prefab", DressStage);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TossRush/Apply Target Variation And Progression")]
        public static void ApplyTargetProgression()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            ConfigureTargetVariationData();
            Edit("Prefabs/Stages/DoorstepStage.prefab", ConfigureDoorstepPlacement);
            AssetDatabase.SaveAssets();
        }

        static void ConfigureTargetVariationData()
        {
            foreach (string name in new[] { "Street", "Basketball", "Doorstep" })
            {
                var data = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/" + name + ".asset");
                data.randomize = true; data.depthOffsetRange = Vector2.zero;
                float extent = name == "Street" ? .55f : name == "Basketball" ? .7f : 2.75f;
                data.horizontalOffsetRange = new Vector2(-extent, extent);
                data.minimumHorizontalDistanceFromCenter = name == "Doorstep" ? 2.25f : 0;
                data.minimumHorizontalChange = name == "Street" ? .25f : name == "Basketball" ? .3f : .12f;
                if (name == "Doorstep") { data.wind = Vector3.zero; data.windSpeedRange = Vector2.zero; }
                EditorUtility.SetDirty(data);
            }
            var carnival = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/Carnival.asset");
            carnival.verticalMotionAmplitude = .32f; carnival.verticalMotionSpeed = 1.15f; carnival.verticalMotionUnlockHits = 1;
            carnival.motionSpeedAfterFirstClear = 1.25f;
            EditorUtility.SetDirty(carnival);
        }

        static void ConfigureDoorstepPlacement(GameObject root)
        {
            root.GetComponent<StageView>().movingTarget.localPosition = new Vector3(0, 0, 3.4f);
            var oldPad = root.transform.Find("Mailbox paving pad");
            if (oldPad) UnityEngine.Object.DestroyImmediate(oldPad.gameObject);
            foreach (int side in new[] { -1, 1 })
                if (!root.transform.Find("Mailbox paving pad " + side))
                    Part(root.transform, "Mailbox paving pad " + side, new Vector3(side * 2.5f, .005f, 3.4f), new Vector3(1.55f, .025f, 1), "Street/Stone", true);
            var plant = root.transform.Find("Scene Props/Tall porch plant");
            if (plant) plant.localPosition = new Vector3(-3.8f, 0, 3.7f);
            var rack = root.transform.Find("Scene Props/Shoe rack");
            if (rack) rack.localPosition = new Vector3(3.8f, 0, 3.6f);
            var flowers = root.transform.Find("Scene Props/Flowerpot");
            if (flowers) flowers.localPosition = new Vector3(3.6f, 0, 1.8f);
        }

        [MenuItem("TossRush/Repair Bin And Hoop Entry Detection")]
        public static void ApplyEntryDetection()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            foreach (string name in new[] { "Street", "Basketball" })
                Edit("Prefabs/Stages/" + name + "Stage.prefab", ConfigureEntryTracking);
            AssetDatabase.SaveAssets();
        }

        static void ConfigureEntryTracking(GameObject root)
        {
            string name = root.GetComponent<StageView>().data.name;
            if (name != "Street" && name != "Basketball") return;
            foreach (var gate in root.GetComponentsInChildren<ScoreGate>(true)) gate.rollInDepth = name == "Street" ? .65f : .6f;
        }

        static void DressStage(GameObject root)
        {
            ConfigureEntryTracking(root);
            foreach (string name in new[] { "Rebound Obstacles", "Extra Physics Props", "Scene Props" })
            {
                var old = root.transform.Find(name);
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            if (root.GetComponent<StageView>().data.name == "Street") { AddCrowd(root); return; }
            var parent = Group(root.transform, "Scene Props", Vector3.zero);
            switch (root.GetComponent<StageView>().data.name)
            {
                case "Corner": RoomProps(parent); break;
                case "Basketball": CourtProps(parent); break;
                case "Carnival": CarnivalProps(parent); break;
                case "Doorstep": DoorstepProps(parent); ConfigureDoorstepPlacement(root); break;
            }
            KnockdownSceneUpdate.ConfigureProps(root, root.GetComponent<StageView>().data.name);
        }

        static Transform Group(Transform parent, string name, Vector3 position)
        {
            var group = new GameObject(name).transform; group.SetParent(parent, false); group.localPosition = position;
            return group;
        }

        static void Bench(Transform parent, Vector3 position, string material)
        {
            var group = Group(parent, "Bench", position);
            Part(group, "Seat", new Vector3(0, .48f, 0), new Vector3(1.8f, .12f, .55f), material, true);
            Part(group, "Backrest", new Vector3(0, .87f, .24f), new Vector3(1.8f, .65f, .1f), material, true);
            foreach (int side in new[] { -1, 1 })
                Part(group, "Leg " + side, new Vector3(side * .67f, .22f, 0), new Vector3(.13f, .44f, .46f), "Street/Secondary", true);
        }

        static void Plant(Transform parent, string name, Vector3 position, float height, bool flowers = false)
        {
            var group = Group(parent, name, position);
            Part(group, "Flowerpot", new Vector3(0, .25f, 0), new Vector3(.5f, .25f, .5f), "Coral", true, PrimitiveType.Cylinder);
            for (int i = -1; i <= 1; i++)
            {
                float stemHeight = height * (i == 0 ? 1 : .78f);
                Part(group, "Stem " + i, new Vector3(i * .13f, .4f + stemHeight * .4f, 0), new Vector3(.045f, stemHeight * .8f, .045f), "Street/Foliage");
                Part(group, "Leaves " + i, new Vector3(i * .2f, .5f + stemHeight * .5f, 0), new Vector3(.35f, .45f, .16f), "Street/Foliage", false, PrimitiveType.Sphere, new Vector3(0, 0, i * -25));
                if (flowers)
                    for (int petal = 0; petal < 5; petal++)
                    {
                        float angle = petal * Mathf.PI * 2 / 5;
                        Part(group, "Petal " + i + " " + petal, new Vector3(i * .13f + Mathf.Cos(angle) * .09f, .4f + stemHeight * .8f + Mathf.Sin(angle) * .09f, -.025f), Vector3.one * .12f, i == 0 ? "Gold" : "Cream", false, PrimitiveType.Sphere);
                    }
            }
        }

        static Transform Loose(Transform parent, string name, Vector3 position, Vector3 size, string material, bool ball = false)
        {
            var group = Group(parent, name, position);
            Part(group, "Visual", Vector3.zero, size, material, false, ball ? PrimitiveType.Sphere : PrimitiveType.Cube);
            if (ball) group.gameObject.AddComponent<SphereCollider>().radius = size.x * .5f;
            else group.gameObject.AddComponent<BoxCollider>().size = size;
            var body = group.gameObject.AddComponent<Rigidbody>(); body.mass = ball ? .18f : .35f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            group.gameObject.AddComponent<PhysicsProp>(); return group;
        }

        static void RoomProps(Transform parent)
        {
            var shelf = Group(parent, "Bookcase", new Vector3(-5.3f, 0, 1.4f));
            Part(shelf, "Back panel", new Vector3(0, 1.15f, .23f), new Vector3(1.4f, 2.3f, .08f), "Wood", true).GetComponent<Collider>().sharedMaterial = BankSurface;
            foreach (int side in new[] { -1, 1 }) Part(shelf, "Side " + side, new Vector3(side * .7f, 1.15f, 0), new Vector3(.1f, 2.3f, .56f), "Wood", true);
            for (int row = 0; row < 4; row++)
            {
                float y = .12f + row * .63f;
                Part(shelf, "Shelf " + row, new Vector3(0, y, 0), new Vector3(1.4f, .08f, .56f), "Wood", true);
                for (int i = 0; i < 6; i++)
                {
                    float height = .39f + i % 2 * .08f;
                    Part(shelf, "Book " + row + " " + i, new Vector3(-.5f + i * .2f, y + .04f + height * .5f + .01f, -.04f), new Vector3(.14f, height, .32f), new[] { "Mint", "Coral", "Cream", "Ink" }[(row + i) % 4]);
                }
            }
            var desk = Group(parent, "Side table and lamp", new Vector3(3.7f, 0, -.8f));
            Part(desk, "Tabletop", new Vector3(0, .7f, 0), new Vector3(1.15f, .1f, .8f), "Wood", true);
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                Part(desk, "Table leg", new Vector3(x * .45f, .33f, z * .27f), new Vector3(.08f, .66f, .08f), "Wood", true);
            Part(desk, "Lamp stand", new Vector3(0, 1.02f, 0), new Vector3(.055f, .32f, .055f), "Ink");
            Part(desk, "Lamp shade", new Vector3(0, 1.35f, 0), new Vector3(.45f, .2f, .45f), "Cream", true, PrimitiveType.Cylinder);
            Bench(parent, new Vector3(3.9f, 0, 1.8f), "Wood");
            Plant(parent, "Houseplant", new Vector3(-4.7f, 0, -.9f), 1.1f);
            var books = Loose(parent, "Loose book stack", new Vector3(-2.8f, .14f, -1.7f), new Vector3(.48f, .28f, .36f), "Mint");
            Part(books, "Page edges", new Vector3(0, .02f, -.185f), new Vector3(.42f, .18f, .012f), "Cream");
            Loose(parent, "Toy storage box", new Vector3(2.8f, .2f, -2), new Vector3(.5f, .4f, .45f), "Coral");
            Loose(parent, "Toy ball", new Vector3(-3.8f, .18f, -2.6f), Vector3.one * .36f, "Gold", true);
        }

        static void CourtProps(Transform parent)
        {
            Bench(parent, new Vector3(-3.7f, 0, 1.1f), "Street/BinBlue");
            var rack = Group(parent, "Basketball rack", new Vector3(3.7f, 0, .8f));
            foreach (int side in new[] { -1, 1 })
                Part(rack, "Rack upright " + side, new Vector3(side * .8f, .83f, 0), new Vector3(.08f, 1.66f, .6f), "Street/Secondary", true);
            for (int row = 0; row < 2; row++)
            {
                float y = .45f + row * .8f;
                Part(rack, "Ball shelf " + row, new Vector3(0, y, 0), new Vector3(1.65f, .08f, .6f), "Street/Secondary", true);
                for (int i = -1; i <= 1; i++)
                {
                    var ball = Part(rack, "Rack basketball " + row + " " + i, new Vector3(i * .52f, y + .26f, 0), Vector3.one * .43f, "Coral", true, PrimitiveType.Sphere);
                    Part(ball.transform, "Ball seam", Vector3.zero, new Vector3(1.015f, .045f, 1.015f), "Ink");
                }
            }
            var scoreboard = Group(parent, "Courtside scoreboard", new Vector3(-4.7f, 0, 3));
            Part(scoreboard, "Stand", new Vector3(0, 1, 0), new Vector3(.12f, 2, .12f), "Street/Secondary", true);
            Part(scoreboard, "Score panel", new Vector3(0, 2.1f, 0), new Vector3(1.45f, .8f, .12f), "Ink", true).GetComponent<Collider>().sharedMaterial = BankSurface;
            foreach (int side in new[] { -1, 1 }) Part(scoreboard, "Score digit " + side, new Vector3(side * .3f, 2.1f, -.07f), new Vector3(.2f, .4f, .025f), "Gold");
            for (int i = 0; i < 3; i++)
            {
                var cone = Group(parent, "Training cone " + i, new Vector3(-3.2f + i * .65f, 0, -2.5f));
                Part(cone, "Cone base", new Vector3(0, .035f, 0), new Vector3(.35f, .07f, .35f), "Coral", true);
                for (int ring = 0; ring < 3; ring++)
                    Part(cone, "Cone tier " + ring, new Vector3(0, .14f + ring * .13f, 0), new Vector3(.25f - ring * .07f, .065f, .25f - ring * .07f), ring == 1 ? "Cream" : "Coral", true, PrimitiveType.Cylinder);
            }
            var bottle = Loose(parent, "Water bottle", new Vector3(2.9f, .25f, -2), new Vector3(.2f, .5f, .2f), "Mint");
            Part(bottle, "Bottle cap", new Vector3(0, .28f, 0), new Vector3(.15f, .04f, .15f), "Cream", false, PrimitiveType.Cylinder);
            Loose(parent, "Sports kit case", new Vector3(4.5f, .2f, -1.5f), new Vector3(.7f, .4f, .4f), "Street/BinBlue");
            Loose(parent, "Practice basketball", new Vector3(3.5f, .24f, -2.7f), Vector3.one * .48f, "Coral", true);
        }

        static void CarnivalProps(Transform parent)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var stall = Group(parent, side < 0 ? "Prize booth" : "Can toss stall", new Vector3(side * 3.7f, 0, 1.2f));
                Part(stall, "Counter", new Vector3(0, .55f, 0), new Vector3(1.75f, 1.1f, .7f), side < 0 ? "Mint" : "Coral", true);
                Part(stall, "Countertop", new Vector3(0, 1.13f, 0), new Vector3(1.9f, .1f, .85f), "Cream", true);
                foreach (int post in new[] { -1, 1 }) Part(stall, "Canopy pole " + post, new Vector3(post * .9f, 1.3f, .3f), new Vector3(.07f, 2.6f, .07f), "Wood", true);
                for (int stripe = 0; stripe < 6; stripe++) Part(stall, "Striped canopy " + stripe, new Vector3(-.83f + stripe * .33f, 2.65f, 0), new Vector3(.33f, .15f, 1.1f), stripe % 2 == 0 ? "Coral" : "Cream", true);
                if (side < 0)
                    for (int toy = -1; toy <= 1; toy++)
                    {
                        var bear = Group(stall, "Plush bear " + toy, new Vector3(toy * .54f, 1.2f, 0));
                        string color = toy == 0 ? "Gold" : "Cream";
                        Part(bear, "Body", new Vector3(0, .17f, 0), new Vector3(.3f, .33f, .22f), color, false, PrimitiveType.Sphere);
                        Part(bear, "Head", new Vector3(0, .42f, 0), Vector3.one * .27f, color, false, PrimitiveType.Sphere);
                        foreach (int ear in new[] { -1, 1 }) Part(bear, "Ear " + ear, new Vector3(ear * .12f, .55f, 0), Vector3.one * .12f, color, false, PrimitiveType.Sphere);
                    }
                else
                    for (int row = 0; row < 3; row++) for (int can = 0; can < 3 - row; can++)
                        Part(stall, "Stacked tin " + row + " " + can, new Vector3((can - 1 + row * .5f) * .35f, 1.35f + row * .32f, 0), new Vector3(.25f, .15f, .25f), row % 2 == 0 ? "Gold" : "Cream", true, PrimitiveType.Cylinder).GetComponent<Collider>().sharedMaterial = BankSurface;
            }
            Loose(parent, "Loose prize box", new Vector3(-2.9f, .2f, -1.8f), new Vector3(.45f, .4f, .4f), "Mint");
            Loose(parent, "Loose tin can", new Vector3(2.9f, .2f, -1.8f), new Vector3(.25f, .4f, .25f), "Gold");
            Loose(parent, "Prize ball", new Vector3(4.4f, .18f, -2.1f), Vector3.one * .36f, "Coral", true);
        }

        static void DoorstepProps(Transform parent)
        {
            Plant(parent, "Tall porch plant", new Vector3(-3.5f, 0, 3.7f), 1.65f, true);
            Plant(parent, "Flowerpot", new Vector3(2.7f, 0, 1.8f), .65f, true);
            Bench(parent, new Vector3(-4, 0, 1.1f), "Wood");
            var rack = Group(parent, "Shoe rack", new Vector3(2.5f, 0, 3.6f));
            foreach (int side in new[] { -1, 1 }) Part(rack, "Rack side " + side, new Vector3(side * .55f, .4f, 0), new Vector3(.07f, .8f, .45f), "Wood", true);
            for (int row = 0; row < 2; row++)
            {
                Part(rack, "Shoe shelf " + row, new Vector3(0, .12f + row * .43f, 0), new Vector3(1.15f, .06f, .5f), "Wood", true);
                foreach (int shoe in new[] { -1, 1 }) Part(rack, "Boot " + row + " " + shoe, new Vector3(shoe * .23f, .29f + row * .43f, -.06f), new Vector3(.19f, .26f, .34f), row == 0 ? "Ink" : "Coral", true);
            }
            var trellis = Group(parent, "Garden trellis", new Vector3(4.5f, 0, 2.2f));
            for (int i = 0; i < 5; i++) Part(trellis, "Upright " + i, new Vector3(-.6f + i * .3f, 1.1f, 0), new Vector3(.055f, 2.2f, .055f), "Wood", true);
            for (int i = 0; i < 4; i++) Part(trellis, "Crossbar " + i, new Vector3(0, .3f + i * .55f, 0), new Vector3(1.4f, .055f, .055f), "Wood", true);
            for (int i = 0; i < 5; i++) Part(trellis, "Climbing leaf " + i, new Vector3(Mathf.Sin(i * 2) * .4f, .4f + i * .36f, -.06f), new Vector3(.3f, .22f, .08f), "Street/Foliage", false, PrimitiveType.Sphere);
            var parcel = Loose(parent, "Delivery parcel", new Vector3(-2.7f, .2f, -.8f), new Vector3(.65f, .4f, .5f), "Street/Facade");
            Part(parcel, "Packing tape", new Vector3(0, .205f, 0), new Vector3(.09f, .012f, .51f), "Cream");
            var can = Loose(parent, "Watering can", new Vector3(3.4f, .2f, -.8f), new Vector3(.38f, .4f, .3f), "Mint");
            Part(can, "Spout", new Vector3(-.3f, .07f, 0), new Vector3(.34f, .1f, .1f), "Mint", false, angles: new Vector3(0, 0, -25));
            Part(can, "Handle top", new Vector3(.23f, .24f, 0), new Vector3(.22f, .06f, .08f), "Mint");
            Part(can, "Handle side", new Vector3(.33f, .05f, 0), new Vector3(.06f, .4f, .08f), "Mint");
        }

        static Transform Limb(Transform parent, string name, Vector3 joint, Vector3 size, string material)
        {
            var pivot = new GameObject(name).transform; pivot.SetParent(parent, false); pivot.localPosition = joint;
            Part(pivot, "Clothing", new Vector3(0, -size.y * .5f, 0), size, material);
            return pivot;
        }

        static void AddCrowd(GameObject root)
        {
            var existing = root.transform.Find("Walking Crowd");
            if (existing) { PlaceCrowdOnSidewalk(existing); return; }
            var parent = new GameObject("Walking Crowd").transform; parent.SetParent(root.transform, false);
            for (int i = 0; i < 10; i++)
            {
                var person = new GameObject("Pedestrian " + (i + 1)); person.transform.SetParent(parent, false);
                float direction = i % 2 == 0 ? 1 : -1;
                person.transform.localPosition = new Vector3(-10.5f + i % 6 * 3.7f, i < 6 ? .02f : 0, i < 6 ? 12.8f : 2.85f);
                person.transform.localRotation = Quaternion.Euler(0, direction * 90, 0);
                person.transform.localScale = Vector3.one * (.86f + i % 4 * .08f);
                string shirt = new[] { "Coral", "Mint", "Gold", "Street/BinBlue", "Cream" }[i % 5];
                Part(person.transform, "Torso", new Vector3(0, 1.12f, 0), new Vector3(.43f, .58f, .25f), shirt);
                Part(person.transform, "Head", new Vector3(0, 1.6f, 0), new Vector3(.32f, .37f, .32f), "Street/Facade", false, PrimitiveType.Sphere);
                Part(person.transform, "Hair", new Vector3(0, 1.75f, .015f), new Vector3(.33f, .13f, .32f), "Ink", false, PrimitiveType.Sphere);
                Part(person.transform, "Nose", new Vector3(0, 1.6f, .18f), new Vector3(.08f, .09f, .09f), "Street/Facade");
                var walker = person.AddComponent<Pedestrian>(); walker.stage = root.GetComponent<StageView>();
                walker.leftArm = Limb(person.transform, "Left arm", new Vector3(-.3f, 1.37f, 0), new Vector3(.13f, .57f, .15f), shirt);
                walker.rightArm = Limb(person.transform, "Right arm", new Vector3(.3f, 1.37f, 0), new Vector3(.13f, .57f, .15f), shirt);
                walker.leftLeg = Limb(person.transform, "Left leg", new Vector3(-.12f, .84f, 0), new Vector3(.16f, .79f, .21f), "Ink");
                walker.rightLeg = Limb(person.transform, "Right leg", new Vector3(.12f, .84f, 0), new Vector3(.16f, .79f, .21f), "Ink");
                var collider = person.AddComponent<CapsuleCollider>(); collider.center = new Vector3(0, .88f, 0); collider.height = 1.76f; collider.radius = .25f;
                var body = person.GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                walker.direction = direction; walker.speed = .85f + i % 4 * .17f; walker.halfTravel = 14;
            }
            PlaceCrowdOnSidewalk(parent);
        }

        static void PlaceCrowdOnSidewalk(Transform parent)
        {
            var walkers = parent.GetComponentsInChildren<Pedestrian>(true).OrderBy(p => p.transform.GetSiblingIndex()).ToArray();
            for (int i = 0; i < walkers.Length; i++)
            {
                int row = i / 5;
                var walker = walkers[i];
                walker.transform.localPosition = new Vector3(-7.5f + i % 5 * 3 + row * 1.5f, 0, row == 0 ? -3.7f : -1.35f);
                walker.direction = (i % 2 == 0 ? 1 : -1);
                walker.speed = .95f + i % 4 * .18f; walker.halfTravel = 10;
                walker.transform.localRotation = Quaternion.Euler(0, walker.direction * 90, 0);
            }
        }

        static void UpdateHeldNewspaper() => Edit("Prefabs/PlayerView.prefab", root =>
        {
            var controller = root.GetComponent<ThrowController>();
            var held = controller.view.transform.Find("Held Newspapers");
            if (!held)
            {
                held = new GameObject("Held Newspapers").transform; held.SetParent(controller.view.transform, false);
                held.localPosition = new Vector3(.38f, -.33f, .95f);
                held.localRotation = Quaternion.Euler(-12, -18, 0);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Projectiles/NewspaperStackVisual.prefab"), held);
            }
            foreach (var child in held.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 9;
            held.gameObject.SetActive(false);
            controller.heldItems = controller.heldItems.Take(4).Concat(new[] { held.gameObject }).ToArray();
        });

        static void UpdateScene()
        {
            const string path = Root + "Scenes/TossRush.unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<GameDirector>(true)).Single();
                var doorstep = game.stages.FirstOrDefault(s => s.data.name == "Doorstep");
                if (!doorstep)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Stages/DoorstepStage.prefab"), scene);
                    doorstep = instance.GetComponent<StageView>(); doorstep.transform.SetPositionAndRotation(game.stages[0].transform.position, game.stages[0].transform.rotation);
                }
                game.stages = game.stages.Where(s => s.data.name != "Doorstep").Concat(new[] { doorstep }).ToArray();
                var held = game.thrower.view.transform.Find("Held Newspapers");
                game.thrower.heldItems = game.thrower.heldItems.Take(4).Concat(new[] { held.gameObject }).ToArray();
                var bottle = game.thrower.view.transform.Find("Held Bottle");
                if (bottle) game.thrower.heldItems[2] = bottle.gameObject;
                for (int i = 0; i < game.stages.Length; i++) game.stages[i].gameObject.SetActive(i == 0);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
