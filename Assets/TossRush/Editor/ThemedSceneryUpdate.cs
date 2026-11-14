using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TossRush.Editor
{
    public static class ThemedSceneryUpdate
    {
        const string Root="Assets/TossRush/";
        const string Materials=Root+"Materials/ThemedScenery/";
        const string Meshes=Root+"Meshes/ThemedScenery/";
        static Mesh foliage;

        [MenuItem("TossRush/Apply All Stage Theme Scenery")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            Folder(Root+"Materials","ThemedScenery");Folder(Root+"Meshes","ThemedScenery");
            Palette();foliage=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Meshes/HouseGarden/RoundedFoliage.asset");
            foreach(string name in new[]{"Corner","Street","Basketball","Carnival"})
            {
                string path=Root+"Prefabs/Stages/"+name+"Stage.prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var old=root.transform.Find("Theme Scenery");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                    var scene=Group(root.transform,"Theme Scenery",Vector3.zero);
                    switch(name)
                    {
                        case "Corner":Room(root,scene);break;
                        case "Street":Street(root,scene);break;
                        case "Basketball":Court(root,scene);break;
                        case "Carnival":Fair(root,scene);break;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("Saved four themed stages: cozy room, city street, park court and twilight fair.");
        }

        static void Folder(string parent,string name){if(!AssetDatabase.IsValidFolder(parent+"/"+name))AssetDatabase.CreateFolder(parent,name);}
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Materials+name+".mat");
        static void Material(string name,Color color,bool emission=false)
        {
            string path=Materials+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);
            if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.4f);}
            EditorUtility.SetDirty(m);
        }
        static void Sky(string name,Color top,Color horizon)
        {
            string path=Materials+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("TossRush/House Garden Sky"));m.name=name;AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_SkyColor",top);m.SetColor("_HorizonColor",horizon);EditorUtility.SetDirty(m);
        }
        static void Palette()
        {
            Material("WarmPlaster",new Color(.83f,.79f,.7f));Material("Ceiling",new Color(.93f,.89f,.79f));
            Material("Oak",new Color(.47f,.32f,.21f));Material("WoodSeam",new Color(.32f,.23f,.17f));
            Material("HoneyWood",new Color(.64f,.45f,.28f));Material("Cream",new Color(.94f,.9f,.78f));
            Material("Sage",new Color(.45f,.6f,.49f));Material("Rug",new Color(.3f,.45f,.43f));
            Material("WindowDaylight",new Color(.56f,.8f,.85f),true);Material("Ink",new Color(.15f,.21f,.26f));
            var daylight=Mat("WindowDaylight");daylight.shader=Shader.Find("Universal Render Pipeline/Unlit");
            daylight.SetColor("_BaseColor",new Color(.62f,.79f,.86f));EditorUtility.SetDirty(daylight);
            Material("UrbanBrick",new Color(.61f,.39f,.31f));Material("UrbanPlaster",new Color(.77f,.74f,.65f));
            Material("UrbanBlue",new Color(.46f,.58f,.66f));Material("DistantCity",new Color(.46f,.57f,.61f));
            Material("ShopGlass",new Color(.25f,.42f,.49f));Material("AwningTeal",new Color(.23f,.49f,.48f));
            Material("AwningWine",new Color(.6f,.29f,.32f));Material("Asphalt",new Color(.31f,.34f,.36f));
            Material("ParkGround",new Color(.44f,.58f,.33f));Material("Leaf",new Color(.25f,.46f,.27f));
            Material("LeafLight",new Color(.46f,.63f,.32f));Material("Trunk",new Color(.39f,.3f,.22f));
            Material("CourtTeal",new Color(.22f,.48f,.5f));Material("CourtKey",new Color(.68f,.35f,.28f));
            Material("WhiteLine",new Color(.92f,.91f,.81f));Material("Metal",new Color(.26f,.31f,.33f));
            Material("FairGround",new Color(.49f,.4f,.31f));Material("FairRed",new Color(.82f,.32f,.35f));
            Material("FairGold",new Color(.94f,.69f,.27f));Material("FairBlue",new Color(.24f,.41f,.6f));
            Material("FairTeal",new Color(.25f,.62f,.57f));Material("FairPurple",new Color(.49f,.36f,.6f));
            Material("Bulb",new Color(1,.78f,.39f),true);Material("LightFace",new Color(.91f,.93f,.83f),true);
            Sky("RoomSky",new Color(.41f,.64f,.78f),new Color(.83f,.88f,.82f));
            Sky("CitySky",new Color(.3f,.57f,.78f),new Color(.79f,.85f,.85f));
            Sky("CourtSky",new Color(.26f,.56f,.8f),new Color(.79f,.89f,.87f));
            Sky("FairSky",new Color(.15f,.2f,.39f),new Color(.87f,.55f,.43f));
            foreach(bool diamond in new[]{true,false})
            {
                string name=diamond?"CourtFence":"FairNet",path=Materials+name+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!m){m=new Material(Shader.Find("TossRush/Scenery Net"));m.name=name;AssetDatabase.CreateAsset(m,path);}
                m.SetColor("_BaseColor",diamond?new Color(.28f,.35f,.36f):new Color(.3f,.31f,.36f));
                m.SetFloat("_Density",diamond?1.55f:3);m.SetFloat("_WireWidth",diamond?.027f:.015f);
                m.SetFloat("_Diagonal",diamond?1:0);EditorUtility.SetDirty(m);
            }
        }
        static Transform Group(Transform parent,string name,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        static GameObject Part(Transform parent,string name,Vector3 position,Vector3 size,string material,
            PrimitiveType primitive=PrimitiveType.Cube,Vector3 angles=default)
        {
            var go=GameObject.CreatePrimitive(primitive);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=size;go.transform.localEulerAngles=angles;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=Mat(material);
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);return go;
        }
        static GameObject MeshPart(Transform parent,string name,Mesh mesh,Vector3 position,Vector3 size,string material,Vector3 angles=default)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=size;go.transform.localEulerAngles=angles;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=Mat(material);
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);return go;
        }
        static void Leaf(Transform p,string name,Vector3 position,Vector3 size,string material="Leaf")=>MeshPart(p,name,foliage,position,size,material);
        static void Paint(Transform root,string name,string material)
        {
            var t=root.Find(name);if(!t)return;var renderer=t.GetComponent<MeshRenderer>();
            if(renderer){renderer.sharedMaterial=Mat(material);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
        }
        static void Hide(Transform root,string name)
        {var t=root.Find(name);if(t&&t.GetComponent<MeshRenderer>())t.GetComponent<MeshRenderer>().enabled=false;}
        static void Atmosphere(GameObject root,Transform scene,string sky,Color ambient,Color light,float intensity,Vector3 angles)
        {
            var sun=new GameObject("Theme sunlight",typeof(Light)).GetComponent<Light>();sun.transform.SetParent(scene,false);sun.transform.localRotation=Quaternion.Euler(angles);
            sun.type=LightType.Directional;sun.color=light;sun.intensity=intensity;sun.shadows=LightShadows.Soft;sun.shadowStrength=.5f;sun.shadowBias=.08f;
            var a=root.GetComponent<StageAtmosphere>();if(!a)a=root.AddComponent<StageAtmosphere>();
            a.sky=Mat(sky);a.sunlight=sun;a.ambient=ambient;a.viewingDistance=150;
        }
        static void Tree(Transform parent,Vector3 position,float height)
        {
            var tree=Group(parent,"Background park tree",position);
            Part(tree,"Trunk",new Vector3(0,height*.28f,0),new Vector3(.32f,height*.28f,.32f),"Trunk",PrimitiveType.Cylinder);
            for(int i=0;i<5;i++)
            {float angle=i*Mathf.PI*.5f;Leaf(tree,"Tree crown",new Vector3(Mathf.Cos(angle)*height*.12f,height*(.65f+i%2*.12f),Mathf.Sin(angle)*height*.1f),new Vector3(height*.5f,height*.48f,height*.45f),i%2==0?"Leaf":"LeafLight");}
        }
        static void Bar(Transform p,string name,Vector3 a,Vector3 b,float width,string mat)
        {var d=b-a;var go=Part(p,name,(a+b)*.5f,new Vector3(width,width,d.magnitude),mat);go.transform.localRotation=Quaternion.LookRotation(d);}
        static Mesh SaveMesh(string name,Mesh mesh)
        {
            string path=Meshes+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;}
            mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh Cone()
        {
            string path=Meshes+"StripedCone.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
            var vertices=new List<Vector3>();var even=new List<int>();var odd=new List<int>();
            for(int i=0;i<16;i++)
            {int k=vertices.Count;float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;vertices.Add(Vector3.up);vertices.Add(new Vector3(Mathf.Cos(a)*.5f,0,Mathf.Sin(a)*.5f));vertices.Add(new Vector3(Mathf.Cos(b)*.5f,0,Mathf.Sin(b)*.5f));(i%2==0?even:odd).AddRange(new[]{k,k+2,k+1});}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.subMeshCount=2;mesh.SetTriangles(even,0);mesh.SetTriangles(odd,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh("StripedCone",mesh);
        }
        static void Room(GameObject root,Transform scene)
        {
            Atmosphere(root,scene,"RoomSky",new Color(.75f,.7f,.6f),new Color(1,.9f,.74f),.95f,new Vector3(40,-30,0));
            Paint(root.transform,"Floor","Oak");Paint(root.transform,"Backdrop","WarmPlaster");
            Paint(root.transform,"Left Wall","WarmPlaster");Paint(root.transform,"Right Wall","WarmPlaster");
            foreach(var t in root.transform.Cast<Transform>().Where(t=>t.name=="Floor Inlay"))t.GetComponent<MeshRenderer>().sharedMaterial=Mat("WoodSeam");
            Part(scene,"Ceiling",new Vector3(0,5.7f,-1.7f),new Vector3(14,.12f,15.2f),"Ceiling");
            foreach(int side in new[]{-1,1})
            {
                Part(scene,"Upper side wall",new Vector3(side*6.9f,4.85f,-1.3f),new Vector3(.2f,1.7f,14),"WarmPlaster");
                Part(scene,"Side skirting board",new Vector3(side*6.76f,.16f,-1.3f),new Vector3(.08f,.3f,14),"HoneyWood");
                Part(scene,"Side crown moulding",new Vector3(side*6.72f,5.54f,-1.3f),new Vector3(.17f,.22f,14),"Cream");
            }
            Part(scene,"Back skirting board",new Vector3(0,.16f,3.46f),new Vector3(13.7f,.3f,.08f),"HoneyWood");
            Part(scene,"Back crown moulding",new Vector3(0,5.54f,3.43f),new Vector3(13.7f,.22f,.17f),"Cream");
            for(int row=0;row<11;row++)for(int board=0;board<6;board++)
                Part(scene,"Staggered oak joint",new Vector3(-6.1f+board*2.35f+(row%2)*.6f,.012f,-6.5f+row*.9f),new Vector3(.018f,.006f,.9f),"WoodSeam");
            var rug=Group(scene,"Woven reading rug",new Vector3(-2,.019f,-1.45f));
            Part(rug,"Rug centre",Vector3.zero,new Vector3(3.5f,.014f,2.15f),"Rug");
            foreach(int s in new[]{-1,1})
            {Part(rug,"Rug border",new Vector3(s*1.65f,.008f,0),new Vector3(.06f,.005f,2),"Cream");Part(rug,"Rug border",new Vector3(0,.008f,s*.98f),new Vector3(3.3f,.005f,.06f),"Cream");}
            var window=Group(scene,"Daylit reading window",new Vector3(-3.25f,3.65f,3.39f));
            Part(window,"Window frame",Vector3.zero,new Vector3(2.4f,1.9f,.1f),"HoneyWood");
            Part(window,"Daylight panes",new Vector3(0,0,-.07f),new Vector3(2.15f,1.66f,.04f),"WindowDaylight");
            Part(window,"Window garden trunk",new Vector3(-.65f,-.5f,-.098f),new Vector3(.045f,.6f,.008f),"Trunk");
            Leaf(window,"Tree outside window",new Vector3(-.65f,-.15f,-.098f),new Vector3(.66f,.8f,.008f),"LeafLight");
            for(int bush=0;bush<3;bush++)Leaf(window,"Window garden hedge",new Vector3(-.65f+bush*.65f,-.65f,-.098f),new Vector3(.85f,.35f,.008f),"Sage");
            Part(window,"Window mullion",new Vector3(0,0,-.1f),new Vector3(.065f,1.7f,.03f),"Cream");
            Part(window,"Window transom",new Vector3(0,.12f,-.1f),new Vector3(2.2f,.055f,.03f),"Cream");
            Part(window,"Window sill",new Vector3(0,-1,-.13f),new Vector3(2.6f,.14f,.35f),"Cream");
            foreach(int side in new[]{-1,1})for(int fold=0;fold<3;fold++)
                Part(window,"Sage linen curtain",new Vector3(side*(1.3f+fold*.12f),-.03f,-.18f),new Vector3(.14f,2.12f,.12f),fold%2==0?"Sage":"Cream");
            var clock=Group(scene,"Wall clock",new Vector3(-.45f,4.35f,3.39f));
            Part(clock,"Clock rim",Vector3.zero,new Vector3(.72f,.035f,.72f),"HoneyWood",PrimitiveType.Cylinder,new Vector3(90,0,0));
            Part(clock,"Clock face",new Vector3(0,0,-.05f),new Vector3(.61f,.015f,.61f),"Cream",PrimitiveType.Cylinder,new Vector3(90,0,0));
            Part(clock,"Clock minute hand",new Vector3(.055f,.06f,-.073f),new Vector3(.025f,.27f,.012f),"Ink",angles:new Vector3(0,0,-40));
            Part(clock,"Clock hour hand",new Vector3(-.04f,.02f,-.08f),new Vector3(.2f,.035f,.012f),"Ink",angles:new Vector3(0,0,-12));
            Part(scene,"Pendant cord",new Vector3(0,5.35f,2),new Vector3(.025f,.5f,.025f),"Ink");
            var shade=MeshPart(scene,"Warm pendant shade",Cone(),new Vector3(0,4.96f,2),new Vector3(.95f,.4f,.95f),"Cream");shade.GetComponent<MeshRenderer>().sharedMaterials=new[]{Mat("Cream"),Mat("Cream")};
            Part(scene,"Pendant diffuser",new Vector3(0,4.95f,2),new Vector3(.8f,.012f,.8f),"LightFace",PrimitiveType.Cylinder);
        }
        static void Street(GameObject root,Transform scene)
        {
            Atmosphere(root,scene,"CitySky",new Color(.7f,.74f,.75f),new Color(1,.95f,.85f),1.05f,new Vector3(43,-24,0));
            Part(scene,"City ground extension",new Vector3(0,-.4f,35),new Vector3(160,.1f,150),"Asphalt");
            for(int i=-3;i<=3;i++)
            {
                float x=i*4.3f,height=3.6f+(i+3)%3*.65f;string facade=i%3==0?"UrbanBrick":i%2==0?"UrbanBlue":"UrbanPlaster";
                Paint(root.transform,"Building "+i,facade);
                Part(scene,"Upper shop-house storeys",new Vector3(x,height+2.1f,16.4f),new Vector3(4.05f,4.2f,3.5f),facade);
                Part(scene,"Stone cornice",new Vector3(x,height,14.57f),new Vector3(4.17f,.16f,.18f),"Cream");
                Part(scene,"Roof parapet",new Vector3(x,height+4.28f,16.4f),new Vector3(4.2f,.24f,3.65f),"Cream");
                for(int row=0;row<2;row++)for(int col=-1;col<=1;col++)
                {
                    Vector3 p=new Vector3(x+col*1.1f,height+.95f+row*1.85f,14.58f);
                    Part(scene,"Upper window surround",p,new Vector3(.86f,1.34f,.12f),"Cream");
                    Part(scene,"Upper window glass",p+Vector3.back*.07f,new Vector3(.66f,1.14f,.035f),"ShopGlass");
                    Part(scene,"Upper window divider",p+Vector3.back*.095f,new Vector3(.035f,1.18f,.02f),"Cream");
                    Part(scene,"Window sill",p+new Vector3(0,-.68f,-.06f),new Vector3(.98f,.09f,.22f),"Cream");
                }
                for(int stripe=0;stripe<8;stripe++)
                    Part(scene,"Striped shop awning",new Vector3(x-1.53f+stripe*.44f,3.015f,14.3f),new Vector3(.44f,.09f,.8f),stripe%2==0?"Cream":i%2==0?"AwningTeal":"AwningWine",angles:new Vector3(8,0,0));
                Part(scene,"Shop door frame",new Vector3(x,1.12f,14.56f),new Vector3(.95f,2.24f,.09f),"Cream");
                Part(scene,"Shop entrance glass",new Vector3(x,1.1f,14.5f),new Vector3(.77f,2.02f,.04f),"ShopGlass");
                Part(scene,"Door pull",new Vector3(x+.23f,1.05f,14.46f),new Vector3(.035f,.35f,.025f),"FairGold");
                if(i%2==0)
                {
                    Part(scene,"Rooftop tank",new Vector3(x+.75f,height+4.95f,16.5f),new Vector3(.8f,.58f,.8f),"Metal",PrimitiveType.Cylinder);
                    Part(scene,"Rooftop service unit",new Vector3(x-1,height+4.55f,16.4f),new Vector3(1,.35f,.8f),"UrbanBlue");
                }
            }
            for(int i=0;i<11;i++)
            {
                float x=-29+i*5.8f,h=10+(i*7%11);
                Part(scene,"Distant city block",new Vector3(x,h*.5f,30+i%3*5),new Vector3(4.8f,h,7),"DistantCity");
                for(int row=0;row<3;row++)for(int col=-1;col<=1;col++)
                    Part(scene,"Distant building window",new Vector3(x+col*1.1f,4+row*3.2f,26.45f+i%3*5),new Vector3(.5f,.9f,.03f),"UrbanBlue");
            }
            for(int i=0;i<10;i++)Part(scene,"Far zebra crossing",new Vector3(13,-.137f,4+i*.82f),new Vector3(2.4f,.009f,.42f),"Cream");
            foreach(int side in new[]{-1,1})
            {
                var crown=root.transform.Find("Tree Crown "+side);
                if(crown){crown.GetComponent<MeshFilter>().sharedMesh=foliage;crown.GetComponent<MeshRenderer>().sharedMaterial=Mat("Leaf");}
                Part(scene,"Streetlight diffuser",new Vector3(side*6.8f,3.72f,2.1f),new Vector3(.54f,.025f,.35f),"LightFace");
            }
        }
        static void Fence(Transform scene,string material)
        {
            Mesh Panel(float width,float height,string name)
            {
                var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-width*.5f,0,0),new Vector3(width*.5f,0,0),new Vector3(width*.5f,height,0),new Vector3(-width*.5f,height,0)};
                mesh.uv=new[]{Vector2.zero,new Vector2(width,0),new Vector2(width,height),new Vector2(0,height)};
                mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveMesh(name,mesh);
            }
            var back=MeshPart(scene,"Back safety mesh",Panel(13.7f,6,"BackFencePanel"),new Vector3(0,0,5.88f),Vector3.one,material);
            back.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            foreach(int side in new[]{-1,1})
            {
                var panel=MeshPart(scene,"Side safety mesh",Panel(13,4,"SideFencePanel"),new Vector3(side*6.78f,0,0),Vector3.one,material,new Vector3(0,90,0));
                panel.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                Part(scene,"Low perimeter wall",new Vector3(side*6.9f,.22f,0),new Vector3(.2f,.44f,13),"Metal");
                for(int i=0;i<6;i++)Part(scene,"Side fence post",new Vector3(side*6.78f,2,-6.3f+i*2.5f),new Vector3(.075f,4.1f,.075f),"Metal");
                Bar(scene,"Fence top rail",new Vector3(side*6.78f,4.02f,-6.4f),new Vector3(side*6.78f,4.02f,6.4f),.065f,"Metal");
            }
            for(int i=0;i<5;i++)Part(scene,"Back fence post",new Vector3(-6.7f+i*3.35f,3,5.87f),new Vector3(.075f,6.1f,.075f),"Metal");
            Bar(scene,"Back fence top rail",new Vector3(-6.8f,6.02f,5.87f),new Vector3(6.8f,6.02f,5.87f),.065f,"Metal");
        }
        static void FloorLine(Transform p,Vector3 a,Vector3 b,float width=.045f)
        {
            var d=b-a;var go=Part(p,"Painted court line",(a+b)*.5f,new Vector3(width,.006f,d.magnitude),"WhiteLine");
            go.transform.localRotation=Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        static void FloorArc(Transform p,Vector3 centre,float radius,float start,float end,int segments)
        {
            for(int i=0;i<segments;i++)
            {float a=Mathf.Lerp(start,end,i/(float)segments)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1)/(float)segments)*Mathf.Deg2Rad;FloorLine(p,centre+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),centre+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));}
        }
        static void Court(GameObject root,Transform scene)
        {
            Atmosphere(root,scene,"CourtSky",new Color(.72f,.76f,.72f),new Color(1,.94f,.82f),1.08f,new Vector3(42,-30,0));
            foreach(string name in new[]{"Backdrop","Left Wall","Right Wall","CourtCircle"})Hide(root.transform,name);
            foreach(var t in root.transform.Cast<Transform>().Where(t=>t.name=="Floor Inlay"))t.GetComponent<MeshRenderer>().enabled=false;
            Paint(root.transform,"Floor","CourtTeal");Fence(scene,"CourtFence");
            Part(scene,"Park meadow",new Vector3(0,-.3f,35),new Vector3(160,.1f,160),"ParkGround");
            Part(scene,"Painted key",new Vector3(0,.012f,1.15f),new Vector3(3.4f,.015f,4.7f),"CourtKey");
            foreach(int side in new[]{-1,1})
            {
                FloorLine(scene,new Vector3(side*5.9f,.027f,-6.7f),new Vector3(side*5.9f,.027f,4.2f));
                FloorLine(scene,new Vector3(side*1.7f,.03f,-1.2f),new Vector3(side*1.7f,.03f,3.5f));
                Tree(scene,new Vector3(side*10.5f,0,12),8);Tree(scene,new Vector3(side*17,0,23),10);
                Part(scene,"Floodlight mast",new Vector3(side*9.2f,4.6f,6),new Vector3(.16f,9.2f,.16f),"Metal");
                for(int lamp=0;lamp<4;lamp++)
                {Part(scene,"Stadium floodlight housing",new Vector3(side*9.2f-.9f+lamp*.6f,9.25f,5.85f),new Vector3(.5f,.6f,.23f),"Metal");Part(scene,"Stadium light lens",new Vector3(side*9.2f-.9f+lamp*.6f,9.25f,5.72f),new Vector3(.41f,.48f,.025f),"LightFace");}
            }
            FloorLine(scene,new Vector3(-5.9f,.027f,4.2f),new Vector3(5.9f,.027f,4.2f));
            FloorLine(scene,new Vector3(-5.9f,.027f,-6.7f),new Vector3(5.9f,.027f,-6.7f));
            FloorLine(scene,new Vector3(-1.7f,.03f,-1.2f),new Vector3(1.7f,.03f,-1.2f));
            FloorArc(scene,new Vector3(0,.03f,-1.2f),1.7f,0,360,48);
            FloorArc(scene,new Vector3(0,.03f,1.65f),4.7f,180,360,48);
            for(int row=0;row<3;row++)
            {
                Part(scene,"Bleacher terrace",new Vector3(0,.2f+row*.33f,8+row*.9f),new Vector3(11,.22f,.85f),"Metal");
                Part(scene,"Bleacher seat plank",new Vector3(0,.4f+row*.33f,8+row*.9f),new Vector3(10.8f,.09f,.44f),row%2==0?"Cream":"UrbanBlue");
            }
            Part(scene,"Community sports hall",new Vector3(-20,3,29),new Vector3(13,6,11),"UrbanPlaster");
            Part(scene,"Sports hall roof",new Vector3(-20,6.15f,29),new Vector3(13.5f,.3f,11.5f),"Metal");
            for(int i=0;i<7;i++)Part(scene,"Sports hall window",new Vector3(-25+i*1.6f,3.7f,23.45f),new Vector3(1.1f,2.5f,.05f),"ShopGlass");
        }
        static void Fair(GameObject root,Transform scene)
        {
            Atmosphere(root,scene,"FairSky",new Color(.62f,.59f,.67f),new Color(1,.81f,.69f),.88f,new Vector3(26,-30,0));
            foreach(string name in new[]{"Backdrop","Left Wall","Right Wall"})Hide(root.transform,name);
            foreach(var t in root.transform.Cast<Transform>().Where(t=>t.name=="Floor Inlay"))t.GetComponent<MeshRenderer>().enabled=false;
            Paint(root.transform,"Floor","FairGround");Fence(scene,"FairNet");
            Part(scene,"Fairground meadow",new Vector3(0,-.3f,30),new Vector3(160,.1f,150),"ParkGround");
            for(int row=0;row<8;row++)Part(scene,"Promenade plank joint",new Vector3(0,.01f,-6+row*1.4f),new Vector3(13.4f,.005f,.018f),"WoodSeam");
            foreach(int side in new[]{-1,1})
            {
                Tent(scene,new Vector3(side*10.5f,0,16),side<0?7:11,side<0?4.8f:8.2f);
                Tree(scene,new Vector3(side*20,0,25),9);
            }
            FerrisWheel(scene,new Vector3(-16.5f,0,27));
            for(int row=0;row<3;row++)
            {
                float z=-.5f+row*7,baseHeight=5.1f+row*.5f;
                for(int i=0;i<20;i++)
                {
                    float x=-11+i*1.16f,next=x+1.16f,y=baseHeight+.006f*x*x,ny=baseHeight+.006f*next*next;
                    Bar(scene,"Festival festoon cable",new Vector3(x,y,z),new Vector3(next,ny,z),.018f,"Ink");
                    Leaf(scene,"Warm festoon bulb",new Vector3(x,y-.12f,z),Vector3.one*.14f,"Bulb");
                    if(i%2==0)
                    {
                        var flag=new Mesh();var a=Vector3.zero;var b=new Vector3(.7f,.04f,0);var c=new Vector3(.32f,-.6f,0);
                        flag.vertices=new[]{a,b,c,a,b,c};flag.triangles=new[]{0,1,2,3,5,4};flag.RecalculateNormals();flag.RecalculateBounds();
                        MeshPart(scene,"Colourful bunting",SaveMesh("BuntingFlag",flag),new Vector3(x+.2f,y-.02f,z),Vector3.one,new[]{"FairRed","FairGold","FairTeal","FairBlue"}[(i/2+row)%4]);
                    }
                }
            }
        }
        static void Tent(Transform p,Vector3 position,float width,float height)
        {
            var tent=Group(p,"Distant striped big top",position);float wall=height*.43f;
            Part(tent,"Canvas walls",new Vector3(0,wall*.5f,0),new Vector3(width*.92f,wall*.5f,width*.92f),"FairBlue",PrimitiveType.Cylinder);
            var roof=MeshPart(tent,"Striped tent roof",Cone(),new Vector3(0,wall,0),new Vector3(width,height-wall,width),"FairRed");roof.GetComponent<MeshRenderer>().sharedMaterials=new[]{Mat("FairRed"),Mat("Cream")};
            Part(tent,"Tent peak pole",new Vector3(0,height+.35f,0),new Vector3(.07f,.7f,.07f),"Metal");
            Part(tent,"Tent peak pennant",new Vector3(.4f,height+.5f,0),new Vector3(.8f,.35f,.03f),"FairGold");
            for(int i=0;i<10;i++)Part(tent,"Canvas panel stripe",new Vector3(-width*.4f+i*width*.088f,wall*.5f,-width*.45f),new Vector3(width*.035f,wall,.05f),i%2==0?"Cream":"FairGold");
        }
        static void FerrisWheel(Transform p,Vector3 position)
        {
            var wheel=Group(p,"Distant ferris wheel",position);Vector3 hub=new Vector3(0,7.6f,0);float radius=6.2f;
            foreach(int side in new[]{-1,1})Bar(wheel,"Wheel support",new Vector3(side*3.3f,0,-.8f),hub,.23f,"Cream");
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;
                Vector3 point=hub+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius,next=hub+new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*radius;
                Bar(wheel,"Wheel rim",point,next,.16f,"FairGold");
                if(i%5==0)
                {
                    Bar(wheel,"Wheel spoke",hub,point,.09f,"Cream");
                    var cabin=Group(wheel,"Ferris gondola",point+new Vector3(0,-.5f,-.1f));
                    Part(cabin,"Cabin body",Vector3.zero,new Vector3(1.1f,.65f,.75f),new[]{"FairRed","FairBlue","FairTeal","FairPurple"}[i/5%4]);
                    Part(cabin,"Cabin canopy",new Vector3(0,.65f,0),new Vector3(1.2f,.1f,.85f),"Cream");
                    foreach(int side in new[]{-1,1})Part(cabin,"Cabin hanger",new Vector3(side*.45f,.35f,0),new Vector3(.04f,.65f,.06f),"Cream");
                }
                if(i%2==0)Leaf(wheel,"Wheel bulb",point+Vector3.back*.11f,Vector3.one*.23f,"Bulb");
            }
            Part(wheel,"Wheel hub",hub+Vector3.back*.1f,new Vector3(.75f,.13f,.75f),"FairGold",PrimitiveType.Cylinder,new Vector3(90,0,0));
        }
    }
}
