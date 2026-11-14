using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TossRush.Editor
{
    public static class KnockdownSceneUpdate
    {
        const string Root = "Assets/TossRush/";

        [MenuItem("TossRush/Repair Books And Mailbox Entry")]
        public static void RepairBooksAndMailbox()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            string path = Root + "Prefabs/Stages/CornerStage.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try { ConfigureProps(root, "Corner"); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            RepairMailboxOpening();
            AssetDatabase.SaveAssets();
        }

        static void RepairMailboxOpening()
        {
            string path = Root + "Prefabs/Targets/MailboxTarget.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var gate = root.GetComponentInChildren<ScoreGate>();
                // Match the physical jambs (+/- .41), lower door top (1.12),
                // and lintel bottom (1.41). Track lip entries up to the back wall.
                gate.transform.localPosition = new Vector3(0, 1.265f, -.37f);
                gate.openingHalfExtents = new Vector2(.41f, .145f);
                gate.rollInDepth = .5f;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("TossRush/Apply Knockdown And Delivery Update")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            NeighborhoodUpdate.ApplyTargetProgression();
            foreach (string name in new[] { "Corner", "Basketball", "Carnival", "Doorstep" })
            {
                string path = Root + "Prefabs/Stages/" + name + "Stage.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ConfigureProps(root, name);
                    if (name == "Doorstep") ConfigureWindows(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            ConfigureBins();
            RepairMailboxOpening();
            var delivery = AssetDatabase.LoadAssetAtPath<StageData>(Root + "Data/Doorstep.asset");
            delivery.instruction = "NEWSPAPERS / AIM UP+DOWN / FIND THE MAILBOX";
            EditorUtility.SetDirty(delivery);
            AssetDatabase.SaveAssets();
            Debug.Log("Saved knockable scenery, instant carnival knockdown points, detached windows and real bin colours.");
        }

        public static void ConfigureProps(GameObject root, string stageName)
        {
            var props = root.transform.Find("Scene Props");
            if (!props) return;
            if (stageName == "Corner")
            {
                var bookcase = props.Find("Bookcase");
                if (bookcase) bookcase.localPosition = new Vector3(-5.3f, 0, 1.4f);
                foreach (var t in props.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Book ")).ToArray())
                {
                    Dynamic(t, .08f);
                    var box = t.GetComponent<BoxCollider>();
                    var parts = t.name.Split(' ');
                    var shelf = t.parent.Find("Shelf " + parts[1]);
                    if (shelf && box)
                    {
                        // Rest each differently sized book above the shelf, instead of
                        // embedding tall books in it and letting depenetration launch them.
                        Vector3 position = t.localPosition;
                        position.y = shelf.localPosition.y + shelf.localScale.y * .5f + .01f
                            + t.localScale.y * (box.size.y * .5f - box.center.y);
                        t.localPosition = position;
                        box.contactOffset = .002f;
                        shelf.GetComponent<Collider>().contactOffset = .002f;
                    }
                }
                var lamp = props.Find("Side table and lamp");
                if (lamp && !lamp.Find("Knockable lamp"))
                {
                    var group = new GameObject("Knockable lamp").transform;
                    group.SetParent(lamp, false); group.localPosition = new Vector3(0, .77f, 0);
                    foreach (string name in new[] { "Lamp stand", "Lamp shade" })
                    { var part = lamp.Find(name); if (part) part.SetParent(group, true); }
                    Dynamic(group, .18f);
                }
            }
            if (stageName == "Basketball")
                foreach (var t in props.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Rack basketball ")).ToArray()) Dynamic(t, .22f);
            if (stageName == "Carnival")
                foreach (var t in props.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Plush bear ") || t.name.StartsWith("Stacked tin ")).ToArray())
                {
                    var prop = Dynamic(t, .12f);
                    prop.knockdownPoints = 10;
                    if (t.name.StartsWith("Plush bear "))
                    {
                        var box = t.GetComponent<BoxCollider>();
                        box.center = new Vector3(0, .31f, 0); box.size = new Vector3(.48f, .62f, .3f);
                        prop.GetComponent<Rigidbody>().centerOfMass = new Vector3(0, .22f, 0);
                    }
                }
            if (stageName == "Doorstep")
                foreach (var t in props.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Boot ")).ToArray()) Dynamic(t, .12f);

            // Movable furniture uses compound colliders; loose contents keep their own bodies.
            foreach (Transform t in props.Cast<Transform>().ToArray())
            {
                if (t.GetComponent<Rigidbody>()) continue;
                float mass = t.name == "Bookcase" ? 1.2f : t.name.Contains("Bench") ? .8f : .5f;
                Dynamic(t, mass);
            }
        }

        static PhysicsProp Dynamic(Transform t, float mass)
        {
            if (!t.GetComponentsInChildren<Collider>().Any(c => !c.isTrigger))
            {
                var box = t.gameObject.AddComponent<BoxCollider>();
                var meshes = t.GetComponentsInChildren<MeshFilter>();
                Bounds bounds = new Bounds(Vector3.zero, Vector3.zero); bool first = true;
                foreach (var mesh in meshes)
                {
                    Bounds b = mesh.sharedMesh.bounds;
                    for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 p = t.InverseTransformPoint(mesh.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z))));
                        if (first) { bounds = new Bounds(p, Vector3.zero); first = false; } else bounds.Encapsulate(p);
                    }
                }
                box.center = bounds.center; box.size = Vector3.Max(bounds.size, Vector3.one * .04f);
            }
            var body = t.GetComponent<Rigidbody>();
            if (!body) body = t.gameObject.AddComponent<Rigidbody>();
            body.mass = mass; body.useGravity = true; body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = .08f; body.angularDamping = .15f;
            var prop = t.GetComponent<PhysicsProp>();
            if (!prop) prop = t.gameObject.AddComponent<PhysicsProp>();
            return prop;
        }

        static void ConfigureWindows(GameObject root)
        {
            foreach (int side in new[] { -1, 1 })
            {
                string name = "Detachable window " + side;
                var window = root.transform.Find(name);
                if (!window)
                {
                    window = new GameObject(name).transform; window.SetParent(root.transform, false);
                    window.localPosition = new Vector3(side * 2.6f, 2.35f, 4.4f);
                    foreach (string partName in new[] { "Window ", "Window glass ", "Window cross " })
                    { var part = root.transform.Find(partName + side); if (part) part.SetParent(window, true); }
                }
                var prop = Dynamic(window, .4f);
                prop.releaseOnImpact = true; prop.releaseVelocity = new Vector3(0, 0, -1.2f);
                var body = prop.GetComponent<Rigidbody>(); body.isKinematic = true;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        static void ConfigureBins()
        {
            var colors = new[] { new Color(.09f, .115f, .14f), new Color(.08f, .32f, .63f), new Color(.13f, .38f, .21f) };
            var names = new[] { "BinCharcoal", "BinBlue", "BinGreen" };
            for (int i = 0; i < names.Length; i++)
            {
                string path = Root + "Materials/Street/" + names[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    material = new Material(AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Street/BinBlue.mat"));
                    material.name = names[i]; AssetDatabase.CreateAsset(material, path);
                }
                material.SetColor("_BaseColor", colors[i]); EditorUtility.SetDirty(material);
            }
            string stagePath = Root + "Prefabs/Stages/StreetStage.prefab";
            var root = PrefabUtility.LoadPrefabContents(stagePath);
            try
            {
                var stage = root.GetComponent<StageView>();
                var bins = stage.movingTarget.GetComponentsInChildren<ScoreGate>(true).OrderBy(g => g.transform.position.x).ToArray();
                for (int i = 0; i < bins.Length; i++)
                    foreach (var renderer in bins[i].transform.parent.GetComponentsInChildren<MeshRenderer>(true))
                        if ((renderer.name == "Wheeled Bin Body" || renderer.name == "Roll Surface")
                            && renderer.GetComponentInParent<TargetMarker>() == bins[i].GetComponentInParent<TargetMarker>())
                        {
                            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Street/" + names[i % names.Length] + ".mat");
                            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        }
                PrefabUtility.SaveAsPrefabAsset(root, stagePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
