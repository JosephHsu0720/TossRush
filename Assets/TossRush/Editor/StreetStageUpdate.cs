using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TossRush.Editor
{
    // Authoring only. All geometry/UI is saved into assets; nothing is constructed by gameplay code.
    public static class StreetStageUpdate
    {
        const string Root = "Assets/TossRush/";
        const string ScenePath = Root + "Scenes/TossRushPrototype.unity";
        const string Menu = "TossRush/Apply Street Stage Update";
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        [Serializable]
        sealed class Report
        {
            public string appliedUtc;
            public string[] stageOrder, deletedUnusedAssets;
            public int trafficVehicles, streetPoints, projectileTypes;
            public string validation = "Assets applied. Play Mode physics and visual tests must still be run.";
        }

        [MenuItem(Menu)]
        static void ApplyFromMenu() => Debug.Log(Apply());
        [MenuItem(Menu, true)]
        static bool CanApply() => !EditorApplication.isPlayingOrWillChangePlaymode;

        public static string Apply()
        {
            if (!CanApply()) throw new InvalidOperationException("Exit Play Mode before updating the prototype.");
            foreach (string path in new[] { ScenePath, Root + "Prefabs/UI/GameHUD.prefab", Root + "Prefabs/PlayerView.prefab",
                Root + "Prefabs/Stages/CornerStage.prefab", Root + "Prefabs/Stages/BasketballStage.prefab", Root + "Prefabs/Stages/CarnivalStage.prefab" })
                if (!File.Exists(path)) throw new FileNotFoundException("Required prototype asset is missing.", path);
            Directory.CreateDirectory("Temp/StreetStage");
            File.Copy(ScenePath, "Temp/StreetStage/BeforeStreetUpdate.unity", true);
            EnsureFolder(Root + "Materials/Street");
            EnsureFolder(Root + "Meshes/Street");
            MakeMaterials();
            var bagSurface = Surface("TrashBagSurface", .42f, .5f, .03f);
            var lidSurface = Surface("OpenLidSurface", .12f, .2f, 0);
            var visual = MakeBagVisual();
            var projectile = MakeBagProjectile(visual, bagSurface);
            var data = MakeData(projectile, bagSurface);
            var target = MakeTarget(lidSurface);
            var car = MakeCar();
            MakeStage(data, target, car);
            UpdatePlayer(visual);
            UpdateHUD();
            HideCleanShotPrefab();
            foreach (string name in new[] { "CornerStage", "BasketballStage", "CarnivalStage" }) RemoveWorldTitles(name);
            AssetDatabase.SaveAssets();
            ApplyScene();
            var report = new Report {
                appliedUtc = DateTime.UtcNow.ToString("o"),
                stageOrder = new[] { "Corner", "Street", "Basketball", "Carnival" },
                streetPoints = 200, trafficVehicles = 4, projectileTypes = 4,
                deletedUnusedAssets = RemoveUnusedOriginalRoom()
            };
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/StreetStageApply.json", JsonUtility.ToJson(report, true));
            return "Street update applied: ROOM > STREET > COURT > MIDWAY. Street = 200 points including lid bank shots. Run Play Mode tests before release.";
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        static void MakeMaterials()
        {
            materials.Clear();
            string[] names = { "Asphalt", "Stone", "Facade", "Trim", "Secondary", "Glass", "CarPaint", "Black", "BagFold", "BinBlue", "BinRim", "Foliage" };
            string[] colors = { "#494D4C", "#B8A994", "#BEA892", "#D7CBB8", "#81857E", "#3C5662", "#967B68", "#181F27", "#34414E", "#155D95", "#95D7E5", "#6F795C" };
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/StageToon.shader");
            if (!shader) throw new InvalidOperationException("Stage Toon shader is missing.");
            for (int i = 0; i < names.Length; i++) {
                string path = Root + "Materials/Street/" + names[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = new Material(shader) { name = names[i] }; AssetDatabase.CreateAsset(material, path); }
                ColorUtility.TryParseHtmlString(colors[i], out Color color);
                material.SetColor("_BaseColor", color);
                material.SetColor("_ShadowTint", new Color(.55f, .61f, .66f, 1));
                material.SetFloat("_Steps", 3);
                material.SetFloat("_Rim", names[i] == "Black" ? .12f : names[i] == "BinBlue" ? .07f : .02f);
                EditorUtility.SetDirty(material);
                materials.Add(names[i], material);
            }
        }

        static PhysicsMaterial Surface(string name, float dynamicFriction, float staticFriction, float bounce)
        {
            string path = Root + "Materials/Street/" + name + ".physicMaterial";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (!material) { material = new PhysicsMaterial(name); AssetDatabase.CreateAsset(material, path); }
            material.dynamicFriction = dynamicFriction; material.staticFriction = staticFriction; material.bounciness = bounce;
            material.frictionCombine = PhysicsMaterialCombine.Minimum; material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material); return material;
        }

        static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, string material, bool collider = false, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size; go.transform.localRotation = Quaternion.Euler(euler);
            go.GetComponent<MeshRenderer>().sharedMaterial = materials[material];
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject MeshPart(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material; return go;
        }

        static Mesh SaveMesh(Mesh source, string name)
        {
            source.name = name;
            string path = Root + "Meshes/Street/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!asset) { AssetDatabase.CreateAsset(source, path); return source; }
            var triangles = Enumerable.Range(0, source.subMeshCount).Select(source.GetTriangles).ToArray();
            asset.Clear(false); asset.indexFormat = source.indexFormat; asset.vertices = source.vertices;
            asset.normals = source.normals; asset.uv = source.uv; asset.subMeshCount = source.subMeshCount;
            for (int i = 0; i < triangles.Length; i++) asset.SetTriangles(triangles[i], i);
            asset.RecalculateBounds(); EditorUtility.SetDirty(asset); UnityEngine.Object.DestroyImmediate(source); return asset;
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            try { return PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static Mesh BagMesh()
        {
            const int segments = 12;
            var profile = new[] { new Vector2(-.27f, .04f), new Vector2(-.22f, .18f), new Vector2(-.06f, .265f), new Vector2(.14f, .24f), new Vector2(.27f, .1f), new Vector2(.33f, .045f) };
            var vertices = new List<Vector3>(); var indices = new List<int>();
            for (int row = 0; row < profile.Length; row++) for (int i = 0; i < segments; i++) {
                float angle = i * Mathf.PI * 2 / segments;
                float radius = profile[row].y * (1 + .09f * Mathf.Sin(i * 2.4f + row * .4f));
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, profile[row].x, Mathf.Sin(angle) * radius));
            }
            for (int row = 0; row < profile.Length - 1; row++) for (int i = 0; i < segments; i++) {
                int a = row * segments + i, b = row * segments + (i + 1) % segments;
                indices.AddRange(new[] { a, a + segments, b, b, a + segments, b + segments });
            }
            int bottom = vertices.Count; vertices.Add(new Vector3(0, profile[0].x, 0));
            int top = vertices.Count; vertices.Add(new Vector3(0, profile[profile.Length - 1].x, 0));
            for (int i = 0; i < segments; i++) { int j = (i + 1) % segments, last = (profile.Length - 1) * segments; indices.AddRange(new[] { bottom, i, j, top, last + j, last + i }); }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static GameObject MakeBagVisual()
        {
            var root = new GameObject("TrashBagVisual");
            MeshPart(root.transform, "Bag", SaveMesh(BagMesh(), "TrashBag"), materials["Black"]);
            Part(root.transform, "Knot", PrimitiveType.Sphere, new Vector3(0, .34f, 0), new Vector3(.13f, .11f, .12f), "BagFold");
            Part(root.transform, "Tie Left", PrimitiveType.Cube, new Vector3(-.05f, .4f, 0), new Vector3(.15f, .035f, .065f), "Black", false, new Vector3(0, 0, -28));
            Part(root.transform, "Tie Right", PrimitiveType.Cube, new Vector3(.05f, .4f, 0), new Vector3(.15f, .035f, .065f), "Black", false, new Vector3(0, 0, 28));
            return SavePrefab(root, Root + "Prefabs/Projectiles/TrashBagVisual.prefab");
        }

        static Projectile MakeBagProjectile(GameObject visual, PhysicsMaterial surface)
        {
            var root = new GameObject("TrashBag"); root.layer = 8;
            PrefabUtility.InstantiatePrefab(visual, root.transform);
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 8;
            root.AddComponent<SphereCollider>().radius = .28f; root.GetComponent<Collider>().sharedMaterial = surface;
            var body = root.AddComponent<Rigidbody>(); body.mass = .8f; body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.interpolation = RigidbodyInterpolation.Interpolate;
            root.AddComponent<Projectile>();
            return SavePrefab(root, Root + "Prefabs/Projectiles/TrashBag.prefab").GetComponent<Projectile>();
        }

        static StageData MakeData(Projectile projectile, PhysicsMaterial surface)
        {
            string profilePath = Root + "Data/Physics/TrashBag.asset";
            var profile = AssetDatabase.LoadAssetAtPath<ProjectilePhysicsProfile>(profilePath);
            if (!profile) { profile = ScriptableObject.CreateInstance<ProjectilePhysicsProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
            profile.name = "TrashBag";
            profile.displayName = "BLACK TRASH BAG"; profile.mass = .8f; profile.referenceArea = .035f; profile.dragCoefficient = .8f;
            profile.angularDamping = .3f; profile.initialSpin = new Vector3(0, 12, 0); profile.spinLift = .05f;
            profile.maxSpinAcceleration = 6; profile.flightLifetime = 7; profile.surfaceMaterial = surface; EditorUtility.SetDirty(profile);
            string dataPath = Root + "Data/Street.asset";
            var data = AssetDatabase.LoadAssetAtPath<StageData>(dataPath);
            if (!data) { data = ScriptableObject.CreateInstance<StageData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.name = "Street";
            data.title = "CURBSIDE SPIN"; data.instruction = "BLACK BAG / SPIN CURVE / BANK OFF THE OPEN LID";
            data.accent = new Color(.35f, .9f, .91f, 1); data.points = 200; data.cleanShotBonus = 0; data.pointsPerPropCollision = 0;
            data.heldItemIndex = 3; data.projectilePrefab = projectile; data.physicsProfile = profile;
            data.launchSpeed = 11.8f; data.throwLoft = 38; data.gravityScale = 1; data.throwInterval = .55f;
            data.projectileRadius = .28f; data.targetRadius = .67f; data.motionAmplitude = 0;
            data.randomize = true; data.windInfluence = 1; data.windSpeedRange = new Vector2(.4f, 1.8f); data.windDirectionSpread = 15;
            data.horizontalOffsetRange = new Vector2(-.55f, .55f); data.depthOffsetRange = new Vector2(-.15f, .15f);
            EditorUtility.SetDirty(data); return data;
        }

        static Mesh BinMesh(bool solid)
        {
            var vertices = new List<Vector3>(); var indices = new List<int>();
            Action<Vector3, Vector3, Vector3, Vector3> quad = (a, b, c, d) => {
                int first = vertices.Count; vertices.AddRange(new[] { a, b, c, d }); indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            };
            Func<float, float, float, Vector3[]> ring = (x, y, z) => new[] { new Vector3(-x,y,-z), new Vector3(-x,y,z), new Vector3(x,y,z), new Vector3(x,y,-z) };
            var bottom = ring(.51f,.16f,.41f); var top = ring(.7f,1.58f,.6f);
            var insideBottom = ring(.43f,.23f,.33f); var insideTop = ring(.62f,1.58f,.52f);
            for (int i = 0; i < 4; i++) {
                int j = (i + 1) % 4; quad(bottom[i],bottom[j],top[j],top[i]);
                if (!solid) { quad(insideBottom[j],insideBottom[i],insideTop[i],insideTop[j]); quad(top[i],top[j],insideTop[j],insideTop[i]); }
            }
            if (solid) { quad(top[0],top[1],top[2],top[3]); quad(bottom[3],bottom[2],bottom[1],bottom[0]); }
            else quad(insideBottom[0],insideBottom[1],insideBottom[2],insideBottom[3]);
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static void WallCollider(Transform root, string name, Vector3 position, Vector3 size, Vector3 euler)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler); go.AddComponent<BoxCollider>().size = size;
        }

        static GameObject MakeTarget(PhysicsMaterial lidSurface)
        {
            var root = new GameObject("StreetTarget"); root.AddComponent<TargetMarker>();
            MeshPart(root.transform, "Wheeled Bin Body", SaveMesh(BinMesh(false), "WheeledBinBody"), materials["BinBlue"]);
            WallCollider(root.transform,"Left Wall Collider",new Vector3(-.60f,.87f,0),new Vector3(.09f,1.42f,1.12f),new Vector3(0,0,7.5f));
            WallCollider(root.transform,"Right Wall Collider",new Vector3(.60f,.87f,0),new Vector3(.09f,1.42f,1.12f),new Vector3(0,0,-7.5f));
            WallCollider(root.transform,"Front Wall Collider",new Vector3(0,.87f,-.50f),new Vector3(1.24f,1.42f,.09f),new Vector3(-7.5f,0,0));
            WallCollider(root.transform,"Rear Wall Collider",new Vector3(0,.87f,.50f),new Vector3(1.24f,1.42f,.09f),new Vector3(7.5f,0,0));
            WallCollider(root.transform,"Bottom Collider",new Vector3(0,.19f,0),new Vector3(1.02f,.1f,.82f),Vector3.zero);
            foreach (int sign in new[] {-1,1}) {
                Part(root.transform,"Side Rim "+sign,PrimitiveType.Cube,new Vector3(sign*.66f,1.6f,0),new Vector3(.09f,.07f,1.24f),"BinRim",true);
                Part(root.transform,"Front Rear Rim "+sign,PrimitiveType.Cube,new Vector3(0,1.6f,sign*.56f),new Vector3(1.34f,.07f,.09f),"BinRim",true);
                Part(root.transform,"Wheel "+sign,PrimitiveType.Cylinder,new Vector3(sign*.57f,.2f,.46f),new Vector3(.4f,.09f,.4f),"Black",false,new Vector3(0,0,90));
            }
            Part(root.transform,"Rear Handle",PrimitiveType.Cube,new Vector3(0,1.45f,.7f),new Vector3(.8f,.12f,.14f),"Black",true);
            var lid = new GameObject("Open Lid"); lid.transform.SetParent(root.transform,false); lid.transform.localPosition = new Vector3(0,1.64f,.58f); lid.transform.localRotation = Quaternion.Euler(125,0,0);
            var surface = Part(lid.transform,"Roll Surface",PrimitiveType.Cube,new Vector3(0,0,-.62f),new Vector3(1.52f,.09f,1.30f),"BinBlue",true);
            surface.GetComponent<Collider>().sharedMaterial = lidSurface;
            foreach (int sign in new[] {-1,1}) Part(lid.transform,"Lid Ridge "+sign,PrimitiveType.Cube,new Vector3(sign*.48f,-.055f,-.62f),new Vector3(.055f,.035f,1.08f),"Black");
            var gate = new GameObject("Opening Gate"); gate.transform.SetParent(root.transform,false); gate.transform.localPosition = new Vector3(0,1.5f,0);
            var scoring = gate.AddComponent<ScoreGate>(); scoring.openingHalfExtents = new Vector2(.62f,.52f); scoring.rollInDepth = .65f;
            AddOutline(root);
            return SavePrefab(root,Root+"Prefabs/Targets/StreetTarget.prefab");
        }

        static void AddOutline(GameObject root)
        {
            Mesh proxy = BinMesh(true);
            var positions = new List<Vector3>(); var indices = new List<int>(); var lookup = new Dictionary<Vector3Int,int>();
            Action<Mesh,Matrix4x4> add = (mesh,matrix) => {
                Vector3[] source = mesh.vertices; var remap = new int[source.Length];
                for(int i=0;i<source.Length;i++) {
                    Vector3 p = matrix.MultiplyPoint3x4(source[i]); var key = new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
                    if(!lookup.TryGetValue(key,out int index)) { index=positions.Count;positions.Add(p);lookup.Add(key,index); } remap[i]=index;
                }
                foreach(int i in mesh.triangles)indices.Add(remap[i]);
            };
            add(proxy,Matrix4x4.identity); UnityEngine.Object.DestroyImmediate(proxy);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) add(filter.sharedMesh,root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix);
            var fill = new Mesh { indexFormat=IndexFormat.UInt32 }; fill.SetVertices(positions);fill.SetTriangles(indices,0);fill.RecalculateBounds();
            var expandedPositions=new Vector3[positions.Count*16];var uv=new Vector2[expandedPositions.Length];var triangles=new int[indices.Count*16];
            for(int k=0;k<16;k++) {
                float angle=k*Mathf.PI*2/16;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                for(int i=0;i<positions.Count;i++){expandedPositions[k*positions.Count+i]=positions[i];uv[k*positions.Count+i]=direction;}
                for(int i=0;i<indices.Count;i++)triangles[k*indices.Count+i]=indices[i]+k*positions.Count;
            }
            var expanded=new Mesh{indexFormat=expandedPositions.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};expanded.vertices=expandedPositions;expanded.uv=uv;expanded.triangles=triangles;expanded.RecalculateBounds();
            Mesh mask=SaveMesh(fill,"StreetTargetMask"), outline=SaveMesh(expanded,"StreetTargetDilation");
            foreach(var pair in new[]{new {name="OutlineMask",mesh=mask,mat="TargetSilhouetteMask"},new {name="Outline",mesh=outline,mat="TargetOutline"}}) {
                var go=MeshPart(root.transform,pair.name,pair.mesh,AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+pair.mat+".mat"));
                var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            }
        }

        static GameObject MakeCar()
        {
            var root=new GameObject("StreetCar");var model=new GameObject("Model");model.transform.SetParent(root.transform,false);
            Part(model.transform,"Chassis",PrimitiveType.Cube,new Vector3(0,.52f,0),new Vector3(1.5f,.52f,3.2f),"CarPaint");
            Part(model.transform,"Cabin",PrimitiveType.Cube,new Vector3(0,1.0f,-.12f),new Vector3(1.24f,.65f,1.78f),"CarPaint");
            Part(model.transform,"Front Glass",PrimitiveType.Cube,new Vector3(0,1.04f,.79f),new Vector3(1.13f,.46f,.035f),"Glass");
            Part(model.transform,"Rear Glass",PrimitiveType.Cube,new Vector3(0,1.04f,-1.03f),new Vector3(1.13f,.46f,.035f),"Glass");
            foreach(int side in new[]{-1,1}) {
                Part(model.transform,"Side Glass "+side,PrimitiveType.Cube,new Vector3(side*.635f,1.04f,-.12f),new Vector3(.035f,.46f,1.57f),"Glass");
                Part(model.transform,"Headlight "+side,PrimitiveType.Cube,new Vector3(side*.48f,.55f,1.62f),new Vector3(.28f,.16f,.04f),"Trim");
                foreach(int axle in new[]{-1,1})Part(model.transform,"Wheel "+side+" "+axle,PrimitiveType.Cylinder,new Vector3(side*.72f,.27f,axle*1.03f),new Vector3(.44f,.12f,.44f),"Black",false,new Vector3(0,0,90));
            }
            BakeCar(model,root.transform);
            var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var chassis=root.AddComponent<BoxCollider>();chassis.center=new Vector3(0,.52f,0);chassis.size=new Vector3(1.5f,.52f,3.2f);
            var roof=root.AddComponent<BoxCollider>();roof.center=new Vector3(0,1,-.12f);roof.size=new Vector3(1.24f,.65f,1.78f);
            root.AddComponent<TrafficVehicle>();return SavePrefab(root,Root+"Prefabs/Props/StreetCar.prefab");
        }

        static void BakeCar(GameObject model,Transform parent)
        {
            var renderers=model.GetComponentsInChildren<MeshRenderer>();var mats=renderers.Select(r=>r.sharedMaterial).Distinct().ToArray();var intermediate=new List<Mesh>();
            foreach(var mat in mats){var mesh=new Mesh();mesh.CombineMeshes(renderers.Where(r=>r.sharedMaterial==mat).Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);intermediate.Add(mesh);}
            var combined=new Mesh();combined.CombineMeshes(intermediate.Select(mesh=>new CombineInstance{mesh=mesh,transform=Matrix4x4.identity}).ToArray(),false,true);
            var asset=SaveMesh(combined,"StreetCar");foreach(var mesh in intermediate)UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(model);
            var go=MeshPart(parent,"Car Visual",asset,mats[0]);go.GetComponent<MeshRenderer>().sharedMaterials=mats;
        }

        static void MakeStage(StageData data,GameObject target,GameObject car)
        {
            var root=new GameObject("StreetStage");var stage=root.AddComponent<StageView>();stage.data=data;stage.authoredRadius=.67f;
            Part(root.transform,"Sidewalk",PrimitiveType.Cube,new Vector3(0,-.16f,-1.4f),new Vector3(32,.32f,9.6f),"Stone",true);
            Part(root.transform,"Curb",PrimitiveType.Cube,new Vector3(0,-.02f,3.38f),new Vector3(32,.18f,.2f),"Trim",true);
            Part(root.transform,"Two Way Road",PrimitiveType.Cube,new Vector3(0,-.30f,8),new Vector3(32,.3f,9.2f),"Asphalt",true);
            Part(root.transform,"Far Sidewalk",PrimitiveType.Cube,new Vector3(0,-.10f,13.0f),new Vector3(32,.24f,1.5f),"Stone",true);
            for(int i=-5;i<=5;i++) {
                Part(root.transform,"Center Stripe "+i,PrimitiveType.Cube,new Vector3(i*3,-.143f,7.7f),new Vector3(1.8f,.012f,.1f),"Trim");
                Part(root.transform,"Paving Joint "+i,PrimitiveType.Cube,new Vector3(i*3,.004f,-1.4f),new Vector3(.022f,.008f,9.4f),"Secondary");
            }
            for(int i=-3;i<=3;i++) {
                float x=i*4.3f,height=3.6f+(i+3)%3*.65f;
                Part(root.transform,"Building "+i,PrimitiveType.Cube,new Vector3(x,height*.5f,16.4f),new Vector3(4.05f,height,3.5f),"Facade",true);
                for(int w=-1;w<=1;w++)Part(root.transform,"Window "+i+" "+w,PrimitiveType.Cube,new Vector3(x+w*1.05f,2.1f,14.63f),new Vector3(.65f,.95f,.035f),"Glass");
                Part(root.transform,"Awning "+i,PrimitiveType.Cube,new Vector3(x,3,14.4f),new Vector3(3.5f,.12f,.65f),"Secondary");
            }
            foreach(int side in new[]{-1,1}) {
                Part(root.transform,"Lamp Pole "+side,PrimitiveType.Cylinder,new Vector3(side*6.8f,1.9f,2.1f),new Vector3(.13f,1.9f,.13f),"Secondary",true);
                Part(root.transform,"Lamp Head "+side,PrimitiveType.Cube,new Vector3(side*6.8f,3.8f,2.1f),new Vector3(.65f,.16f,.45f),"Trim");
                Part(root.transform,"Tree Trunk "+side,PrimitiveType.Cylinder,new Vector3(side*10.5f,1.2f,2),new Vector3(.3f,1.2f,.3f),"CarPaint",true);
                Part(root.transform,"Tree Crown "+side,PrimitiveType.Sphere,new Vector3(side*10.5f,3,2),new Vector3(2.5f,2.6f,2.4f),"Foliage");
            }
            var bin=(GameObject)PrefabUtility.InstantiatePrefab(target,root.transform);bin.transform.localPosition=new Vector3(0,0,1.25f);
            stage.movingTarget=bin.transform;stage.gate=bin.GetComponentInChildren<ScoreGate>();stage.gate.stage=stage;
            for(int lane=0;lane<2;lane++)for(int i=0;i<2;i++) {
                var vehicle=(GameObject)PrefabUtility.InstantiatePrefab(car,root.transform);vehicle.name="Traffic Lane "+lane+" Car "+i;
                vehicle.transform.localPosition=new Vector3(-10+i*17,-.20f,lane==0?5.3f:10.0f);vehicle.transform.localRotation=Quaternion.Euler(0,lane==0?90:-90,0);
                var traffic=vehicle.GetComponent<TrafficVehicle>();traffic.stage=stage;traffic.direction=lane==0?1:-1;traffic.speed=lane==0?4.2f:3.6f;traffic.halfTravel=17;
            }
            SavePrefab(root,Root+"Prefabs/Stages/StreetStage.prefab");
        }

        static void UpdatePlayer(GameObject visual)
        {
            string path=Root+"Prefabs/PlayerView.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try {
                var controller=root.GetComponent<ThrowController>();var existing=controller.view.transform.Find("Held TrashBag");
                GameObject held;
                if(existing)held=existing.gameObject;
                else {
                    held=new GameObject("Held TrashBag");held.transform.SetParent(controller.view.transform,false);held.transform.localPosition=new Vector3(.43f,-.34f,1.35f);
                    PrefabUtility.InstantiatePrefab(visual,held.transform);held.AddComponent<HeldItemSpin>();
                }
                if(!held.GetComponent<HeldItemSpin>())held.AddComponent<HeldItemSpin>();
                foreach(var part in held.GetComponentsInChildren<Transform>(true))part.gameObject.layer=9;
                held.SetActive(false);controller.heldItems=controller.heldItems.Take(3).Concat(new[]{held}).ToArray();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static RectTransform UIObject(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }

        static UnityEngine.UI.Image Stroke(Transform parent,string name,Vector2 position,Vector2 size,float rotation)
        {
            var rect=UIObject(parent,name,size,position);rect.localRotation=Quaternion.Euler(0,0,rotation);
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.raycastTarget=false;
            image.color=new Color(.45f,1,1,name.StartsWith("Neon Glow")?.28f:1);return image;
        }

        static GameObject MakeWindPrefab(TMP_Text template)
        {
            var root=new GameObject("WindIndicator",typeof(RectTransform),typeof(WindIndicator));var indicator=root.GetComponent<WindIndicator>();
            root.GetComponent<RectTransform>().sizeDelta=new Vector2(350,30);
            indicator.arrow=UIObject(root.transform,"Arrow",new Vector2(56,26),new Vector2(-145,0));
            var core=new List<UnityEngine.UI.Image>();var glow=new List<UnityEngine.UI.Image>();
            for(int i=0;i<3;i++) {
                Vector2 position=i==0?new Vector2(-3,0):new Vector2(12,i==1?4:-4);float rotation=i==0?0:i==1?-30:30;float width=i==0?38:18;
                glow.Add(Stroke(indicator.arrow,"Neon Glow "+i,position,new Vector2(width+4,8),rotation));
            }
            for(int i=0;i<3;i++)core.Add(Stroke(indicator.arrow,"Arrow Core "+i,i==0?new Vector2(-3,0):new Vector2(12,i==1?4:-4),new Vector2(i==0?38:18,3),i==0?0:i==1?-30:30));
            indicator.coreImages=core.ToArray();indicator.glowImages=glow.ToArray();
            var label=UnityEngine.Object.Instantiate(template.gameObject,root.transform);label.name="Speed";indicator.speedText=label.GetComponent<TMP_Text>();
            indicator.speedText.text="0.0 m/s";indicator.speedText.fontSize=14;indicator.speedText.alignment=TextAlignmentOptions.MidlineLeft;indicator.speedText.raycastTarget=false;
            var rect=indicator.speedText.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(66,0);rect.sizeDelta=new Vector2(210,28);
            return SavePrefab(root,Root+"Prefabs/UI/WindIndicator.prefab");
        }

        static void UpdateHUD()
        {
            string path=Root+"Prefabs/UI/GameHUD.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try {
                var hud=root.GetComponent<GameHUD>();var windPrefab=MakeWindPrefab(hud.stageText);var top=hud.stageText.transform.parent;
                var existing=top.Find("WindIndicator");if(existing)UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(windPrefab,top);instance.name="WindIndicator";
                var rect=instance.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(20,-22);rect.sizeDelta=new Vector2(350,30);
                hud.windIndicator=instance.GetComponent<WindIndicator>();hud.stageText.rectTransform.anchoredPosition=new Vector2(20,17);hud.stageText.rectTransform.sizeDelta=new Vector2(400,36);
                hud.stageText.text="CORNER SHOTS / +100";
                foreach(var label in hud.menuPanel.GetComponentsInChildren<TMP_Text>(true))if(label.name=="StageLegend")label.text="ROOM +100 / STREET +200 / COURT +150 / MIDWAY +200\nSPIN THE BAG. BANK OFF THE LID.";
                hud.cleanShotEffect.rays.gameObject.SetActive(false);hud.cleanShotEffect.label.gameObject.SetActive(false);hud.cleanShotEffect.GetComponent<CanvasGroup>().alpha=0;hud.cleanShotEffect.gameObject.SetActive(false);
                HudLayoutUpdate.Configure(hud);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static void HideCleanShotPrefab()
        {
            string path=Root+"Prefabs/UI/CleanShotFeedback.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{var effect=root.GetComponent<CleanShotEffect>();effect.rays.gameObject.SetActive(false);effect.label.gameObject.SetActive(false);root.GetComponent<CanvasGroup>().alpha=0;PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static void RemoveWorldTitles(string name)
        {
            string path=Root+"Prefabs/Stages/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))if(text&&!text.GetComponentInParent<Canvas>())UnityEngine.Object.DestroyImmediate(text.gameObject);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static void ApplyScene()
        {
            Scene scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try {
                var game=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<GameDirector>(true)).Single();
                var original=game.stages.ToArray();
                StageView corner=original.Single(s=>s&&s.data.name=="Corner"),court=original.Single(s=>s&&s.data.name=="Basketball"),carnival=original.Single(s=>s&&s.data.name=="Carnival");
                StageView street=original.FirstOrDefault(s=>s&&s.data.name=="Street");
                if(!street){var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Stages/StreetStage.prefab"),scene);street=instance.GetComponent<StageView>();street.transform.SetPositionAndRotation(corner.transform.position,corner.transform.rotation);}
                game.stages=new[]{corner,street,court,carnival};game.startingStageIndex=0;
                foreach(var old in original)if(old&&old.data.name=="Room")UnityEngine.Object.DestroyImmediate(old.gameObject);
                for(int i=0;i<game.stages.Length;i++)game.stages[i].gameObject.SetActive(i==0);
                var held=game.thrower.view.transform.Find("Held TrashBag");
                if(!held)throw new InvalidOperationException("Player prefab update has not propagated. Refresh assets and re-run the update.");
                game.thrower.heldItems=game.thrower.heldItems.Take(3).Concat(new[]{held.gameObject}).ToArray();
                var wind=game.hud.transform.Find("PlayPanel/TopBar/WindIndicator");
                if(!wind)throw new InvalidOperationException("HUD prefab update has not propagated. Refresh assets and re-run the update.");
                game.hud.windIndicator=wind.GetComponent<WindIndicator>();game.hud.cleanShotEffect.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save the prototype scene.");
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }

        static string[] RemoveUnusedOriginalRoom()
        {
            string[] candidates={Root+"Prefabs/Stages/RoomStage.prefab",Root+"Prefabs/Targets/RoomStageTarget.prefab",Root+"Data/Room.asset"};
            string[] otherRoots=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!candidates.Contains(p)&&(p.EndsWith(".prefab")||p.EndsWith(".unity")||p.EndsWith(".asset"))).ToArray();
            var references=new HashSet<string>(AssetDatabase.GetDependencies(otherRoots,true));var deleted=new List<string>();
            foreach(string path in candidates)if(File.Exists(path)&&!references.Contains(path)&&AssetDatabase.DeleteAsset(path))deleted.Add(path);
            return deleted.ToArray();
        }
    }
}
