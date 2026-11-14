using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TossRush.Editor
{
    public static class EuropeanGardenUpdate
    {
        const string Root = "Assets/TossRush/";
        const string Materials = Root + "Materials/HouseGarden/";
        static Mesh foliage;

        [MenuItem("TossRush/Apply European Garden Atmosphere")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            Folder(Root + "Materials", "HouseGarden");
            Folder(Root + "Meshes", "HouseGarden");
            Palette();
            foliage = MakeFoliageMesh();
            string path = Root + "Prefabs/Stages/DoorstepStage.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var old = root.transform.Find("European Garden");
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var garden = Group(root.transform, "European Garden", Vector3.zero);
                Terrain(garden);
                Planting(garden);
                Neighborhood(garden);
                Clouds(garden);
                var sun = new GameObject("Garden afternoon sun", typeof(Light)).GetComponent<Light>();
                sun.transform.SetParent(garden, false); sun.transform.localRotation = Quaternion.Euler(48, -32, 0);
                sun.type = LightType.Directional; sun.color = new Color(1, .96f, .86f); sun.intensity = 1.12f;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = .58f; sun.shadowBias = .08f;
                var atmosphere = root.GetComponent<StageAtmosphere>();
                if (!atmosphere) atmosphere = root.AddComponent<StageAtmosphere>();
                atmosphere.sky = AssetDatabase.LoadAssetAtPath<Material>(Materials + "SummerSky.mat");
                atmosphere.sunlight = sun; atmosphere.ambient = new Color(.7f, .77f, .7f); atmosphere.viewingDistance = 180;
                root.transform.Find("Front yard").GetComponent<MeshRenderer>().sharedMaterial = Mat("Lawn");
                root.transform.Find("Delivery path").GetComponent<MeshRenderer>().sharedMaterial = Mat("LimestonePath");
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("Saved sunny European garden: summer sky, layered planting, mature trees and neighboring cottages.");
        }

        static void Folder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Materials + name + ".mat");
        static void ColorMaterial(string name, Color color, Shader shader = null)
        {
            string path = Materials + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader ? shader : Shader.Find("Universal Render Pipeline/Lit")); material.name = name; AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .08f);
            EditorUtility.SetDirty(material);
        }
        static void Palette()
        {
            ColorMaterial("Lawn", new Color(.39f, .61f, .22f));
            ColorMaterial("Meadow", new Color(.45f, .63f, .3f));
            ColorMaterial("LimestonePath", new Color(.73f, .7f, .61f));
            ColorMaterial("Gravel", new Color(.74f, .74f, .68f));
            ColorMaterial("PaleStone", new Color(.65f, .66f, .59f));
            ColorMaterial("StoneCap", new Color(.84f, .82f, .72f));
            ColorMaterial("Hedge", new Color(.24f, .43f, .14f));
            ColorMaterial("LeafLight", new Color(.43f, .61f, .19f));
            ColorMaterial("LeafDark", new Color(.19f, .36f, .16f));
            ColorMaterial("DistantLeaf", new Color(.4f, .54f, .35f));
            ColorMaterial("Trunk", new Color(.36f, .29f, .21f));
            ColorMaterial("Soil", new Color(.25f, .23f, .16f));
            ColorMaterial("Rose", new Color(.83f, .22f, .4f));
            ColorMaterial("Lavender", new Color(.58f, .45f, .71f));
            ColorMaterial("Ivory", new Color(.98f, .93f, .78f));
            ColorMaterial("Plaster", new Color(.84f, .82f, .7f));
            ColorMaterial("Slate", new Color(.3f, .35f, .37f));
            ColorMaterial("Glass", new Color(.33f, .52f, .57f));
            ColorMaterial("Shutter", new Color(.28f, .4f, .36f));
            ColorMaterial("BurgundyDoor", new Color(.43f, .22f, .25f));
            ColorMaterial("Cloud", new Color(.97f, .97f, .9f));
            string path = Materials + "SummerSky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!sky) { sky = new Material(Shader.Find("TossRush/House Garden Sky")); sky.name = "SummerSky"; AssetDatabase.CreateAsset(sky, path); }
            sky.SetColor("_SkyColor", new Color(.25f, .6f, .9f));
            sky.SetColor("_HorizonColor", new Color(.8f, .91f, .93f)); EditorUtility.SetDirty(sky);
        }
        static Transform Group(Transform parent, string name, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }
        static GameObject Part(Transform parent, string name, Vector3 position, Vector3 size, string material,
            PrimitiveType primitive = PrimitiveType.Cube, Vector3 angles = default)
        {
            var go = GameObject.CreatePrimitive(primitive); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size; go.transform.localEulerAngles = angles;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }
        static GameObject Leaf(Transform parent, string name, Vector3 position, Vector3 size, string material, Vector3 angles = default)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size; go.transform.localEulerAngles = angles;
            go.GetComponent<MeshFilter>().sharedMesh = foliage; go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }
        static Mesh MakeFoliageMesh()
        {
            string path = Root + "Meshes/HouseGarden/RoundedFoliage.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing) return existing;
            float t = (1 + Mathf.Sqrt(5)) / 2;
            Vector3[] v = { new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1) };
            int[] faces = {0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            void Face(Vector3 a, Vector3 b, Vector3 c)
            { int i = vertices.Count; vertices.Add(a.normalized * .5f); vertices.Add(b.normalized * .5f); vertices.Add(c.normalized * .5f); triangles.Add(i); triangles.Add(i+1); triangles.Add(i+2); }
            for (int i = 0; i < faces.Length; i += 3)
            {
                Vector3 a=v[faces[i]].normalized,b=v[faces[i+1]].normalized,c=v[faces[i+2]].normalized;
                Vector3 ab=(a+b).normalized,bc=(b+c).normalized,ca=(c+a).normalized;
                Face(a,ab,ca);Face(b,bc,ab);Face(c,ca,bc);Face(ab,bc,ca);
            }
            var mesh = new Mesh { name = "Rounded garden foliage" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path); return mesh;
        }

        static void Terrain(Transform garden)
        {
            Part(garden,"Surrounding meadow",new Vector3(0,-.23f,35),new Vector3(180,.1f,180),"Meadow");
            Part(garden,"Right gravel drive",new Vector3(14,-.002f,10),new Vector3(3.8f,.05f,27),"Gravel",angles:new Vector3(0,10,0));
            foreach(int side in new[]{-1,1})
            {
                var wall=Group(garden,"Boundary stone wall "+side,new Vector3(side*9,0,8));
                for(int row=0;row<3;row++) for(int brick=0;brick<8;brick++)
                    Part(wall,"Limestone block",new Vector3(0,.13f+row*.22f,-5.4f+brick*1.45f+(row%2)*.25f),new Vector3(.38f,.2f,1.4f),"PaleStone");
                Part(wall,"Coping stones",new Vector3(0,.72f,0),new Vector3(.48f,.12f,12),"StoneCap");
                Part(garden,"Gate pillar "+side,new Vector3(side*8.75f,.59f,2.6f),new Vector3(.7f,1.18f,.7f),"PaleStone");
                Part(garden,"Pillar cap "+side,new Vector3(side*8.75f,1.23f,2.6f),new Vector3(.83f,.12f,.83f),"StoneCap");
                Leaf(garden,"Distant wooded hill "+side,new Vector3(side*37,1.8f,66),new Vector3(70,14,37),"DistantLeaf");
            }
            for(int i=0;i<6;i++)
                Part(garden,"Garden path stepping stone "+i,new Vector3(5.4f+Mathf.Sin(i*.8f)*.35f,.015f,3.8f+i*.95f),new Vector3(.7f,.06f,.58f),"StoneCap",angles:new Vector3(0,i*9,0));
        }

        static void Planting(Transform garden)
        {
            foreach(int side in new[]{-1,1})
            {
                var hedge=Group(garden,"Clipped side hedge "+side,new Vector3(side*6.5f,0,7));
                for(int i=0;i<7;i++) Leaf(hedge,"Boxwood hedge",new Vector3(0,.8f,-3.3f+i*1.3f),new Vector3(1.5f,1.7f,1.85f),i%3==0?"LeafLight":"Hedge",new Vector3(0,i*19,0));
                FlowerBed(garden,side,new Vector3(side*5.7f,0,-.65f));
                for(int i=0;i<5;i++)
                    Leaf(garden,"Foundation shrub",new Vector3(side*(4.7f+i*.44f),.48f,5.3f+i*.42f),new Vector3(1.15f,.95f,1),i%2==0?"LeafLight":"Hedge",new Vector3(0,i*31,0));
                Tree(garden,"Garden oak "+side,new Vector3(side*10.5f,0,10.5f),7.7f,side*15);
                Tree(garden,"Distant oak "+side,new Vector3(side*19,0,30),10,side*22);
                Cypress(garden,new Vector3(side*7.8f,0,18.5f),9.4f);
                Cypress(garden,new Vector3(side*24,0,25),11.8f);
            }
            Tree(garden,"Oak behind house",new Vector3(-4,0,16),10.6f,-8);
            foreach(int side in new[]{-1,1})
                for(int i=0;i<3;i++)
                    Leaf(garden,"Low garden rock",new Vector3(side*(5.2f+i*.53f),.15f,-1.6f-i*.13f),new Vector3(.7f+i*.08f,.45f,.6f),"PaleStone",new Vector3(i*13,i*42,12));
        }
        static void FlowerBed(Transform parent,int side,Vector3 position)
        {
            var bed=Group(parent,"Mixed rose and lavender border "+side,position);
            Leaf(bed,"Mulched bed",new Vector3(0,.04f,0),new Vector3(3.7f,.18f,1.85f),"Soil");
            for(int i=0;i<11;i++)
            {
                float x=-1.4f+i*.28f,z=Mathf.Sin(i*2.3f)*.42f;
                Leaf(bed,"Flower foliage",new Vector3(x,.22f,z),new Vector3(.6f,.46f,.6f),i%2==0?"Hedge":"LeafLight",new Vector3(0,i*37,0));
                string color=i%3==0?"Lavender":i%3==1?"Rose":"Ivory";
                for(int bloom=0;bloom<4;bloom++)
                {
                    float angle=(bloom*90+i*19)*Mathf.Deg2Rad;
                    Leaf(bed,"Flower bloom",new Vector3(x+Mathf.Cos(angle)*.15f,.4f+(i%3)*.06f,z+Mathf.Sin(angle)*.15f),new Vector3(.2f,color=="Lavender"?.3f:.15f,.2f),color,new Vector3(0,i*17,0));
                }
            }
            for(int i=0;i<10;i++)
                Part(bed,"Stone edging",new Vector3(-1.62f+i*.36f,.065f,-.78f),new Vector3(.33f,.13f,.19f),"PaleStone",angles:new Vector3(0,i%2*5,0));
        }
        static void Tree(Transform parent,string name,Vector3 position,float height,float angle)
        {
            var tree=Group(parent,name,position); tree.localRotation=Quaternion.Euler(0,angle,0);
            Part(tree,"Oak trunk",new Vector3(0,height*.3f,0),new Vector3(.42f,height*.3f,.42f),"Trunk",PrimitiveType.Cylinder);
            foreach(int side in new[]{-1,1})
                Part(tree,"Spreading branch",new Vector3(side*.55f,height*.5f,.1f),new Vector3(.22f,height*.18f,.22f),"Trunk",PrimitiveType.Cylinder,new Vector3(0,0,side*-32));
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/6;
                Vector3 offset=i==6?new Vector3(0,height*.83f,0):new Vector3(Mathf.Cos(a)*height*.15f,height*(.64f+i%2*.08f),Mathf.Sin(a)*height*.13f);
                Leaf(tree,"Leaf crown",offset,new Vector3(height*.46f,height*.37f,height*.43f),i%3==0?"LeafLight":i%3==1?"Hedge":"LeafDark",new Vector3(i*11,i*37,0));
            }
        }
        static void Cypress(Transform parent,Vector3 position,float height)
        {
            var tree=Group(parent,"Slender cypress",position);
            Part(tree,"Trunk",new Vector3(0,height*.25f,0),new Vector3(.23f,height*.25f,.23f),"Trunk",PrimitiveType.Cylinder);
            for(int i=0;i<5;i++)
                Leaf(tree,"Tapered crown",new Vector3(0,height*(.36f+i*.12f),0),new Vector3(height*(.22f-i*.035f),height*.36f,height*(.2f-i*.031f)),i%2==0?"LeafDark":"Hedge",new Vector3(0,i*27,0));
        }
        static void Neighborhood(Transform garden)
        {
            Cottage(garden,new Vector3(-17,0,21),18,.95f);
            Cottage(garden,new Vector3(15.5f,0,22),-17,1.1f);
            Cottage(garden,new Vector3(-31,0,38),10,1.15f);
            Cottage(garden,new Vector3(32,0,39),-12,1.05f);
        }
        static void Cottage(Transform parent,Vector3 position,float angle,float scale)
        {
            var house=Group(parent,"Neighbor stone cottage",position);house.localRotation=Quaternion.Euler(0,angle,0);house.localScale=Vector3.one*scale;
            Part(house,"Cream stone walls",new Vector3(0,1.9f,0),new Vector3(6,3.8f,5.5f),"Plaster");
            foreach(int side in new[]{-1,1})
            {
                Part(house,"Slate pitched roof",new Vector3(side*1.65f,4.65f,0),new Vector3(3.8f,.16f,6.2f),"Slate",angles:new Vector3(0,0,-side*30));
                // Fill the triangular gable instead of leaving an open roof silhouette.
                var mesh=new Mesh { name="Cottage gable" };
                mesh.vertices=new[]{new Vector3(-3,3.8f,side*2.76f),new Vector3(3,3.8f,side*2.76f),new Vector3(0,5.65f,side*2.76f)};
                mesh.triangles=side<0?new[]{0,2,1}:new[]{0,1,2};mesh.RecalculateNormals();
                string meshPath=Root+"Meshes/HouseGarden/CottageGable"+side+".asset";
                var asset=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(!asset){AssetDatabase.CreateAsset(mesh,meshPath);asset=mesh;}else UnityEngine.Object.DestroyImmediate(mesh);
                var gable=new GameObject("Stone gable",typeof(MeshFilter),typeof(MeshRenderer));gable.transform.SetParent(house,false);gable.GetComponent<MeshFilter>().sharedMesh=asset;gable.GetComponent<MeshRenderer>().sharedMaterial=Mat("Plaster");
                Part(house,"Window surround",new Vector3(side*1.8f,2.05f,-2.8f),new Vector3(1.25f,1.4f,.12f),"StoneCap");
                Part(house,"Window glass",new Vector3(side*1.8f,2.05f,-2.87f),new Vector3(1,1.15f,.03f),"Glass");
                Part(house,"Window mullion",new Vector3(side*1.8f,2.05f,-2.9f),new Vector3(.055f,1.2f,.025f),"Ivory");
                foreach(int shutter in new[]{-1,1}) Part(house,"Sage shutter",new Vector3(side*1.8f+shutter*.76f,2.05f,-2.84f),new Vector3(.3f,1.2f,.08f),"Shutter");
            }
            Part(house,"Burgundy door",new Vector3(0,1.15f,-2.81f),new Vector3(.95f,2.3f,.12f),"BurgundyDoor");
            Part(house,"Chimney",new Vector3(1.8f,5.15f,1.25f),new Vector3(.62f,1.6f,.65f),"PaleStone");
            Part(house,"Chimney cap",new Vector3(1.8f,5.98f,1.25f),new Vector3(.8f,.12f,.8f),"StoneCap");
            Leaf(house,"Front shrub",new Vector3(-2.7f,.43f,-3.5f),new Vector3(1.6f,.85f,1.3f),"Hedge");
            Leaf(house,"Front shrub",new Vector3(2.7f,.43f,-3.5f),new Vector3(1.6f,.85f,1.3f),"LeafLight");
        }
        static void Clouds(Transform garden)
        {
            foreach(var cloud in new[]{new Vector3(-20,18,44),new Vector3(22,21,52),new Vector3(-3,25,65)})
                for(int i=0;i<5;i++) Leaf(garden,"Soft summer cloud",cloud+new Vector3((i-2)*1.7f,Mathf.Sin(i*1.7f)*.65f,0),new Vector3(5,1.9f+i%2,2.8f),"Cloud",new Vector3(0,i*21,0));
        }
    }
}
