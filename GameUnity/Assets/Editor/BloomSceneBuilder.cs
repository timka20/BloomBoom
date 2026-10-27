using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;

public static partial class BloomBuilder
{
    const string Output="Assets/BloomGenerated";
    static Font uiFont;
    static Material textMat;
    static Dictionary<string,Material> mats;
    static Transform room;
    static Material M(string name,Color color,bool unlit=false,bool glass=false)
    {
        if(mats.TryGetValue(name,out var cached))return cached;
        var m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.35f);
        if(glass){m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);}
        string path=$"{Output}/Materials/{name}.mat";AssetDatabase.CreateAsset(m,path);mats[name]=m;return m;
    }
    static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
    static GameObject Shape(string name,Vector3 pos,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube,bool solid=true,Transform parent=null)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.position=pos;g.transform.localScale=size;g.transform.SetParent(parent?parent:room,true);g.GetComponent<Renderer>().sharedMaterial=mat;
        if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static TextMesh Text(string text,Vector3 p,float height,Color color,Quaternion rotation,Transform parent=null)
    {
        var g=new GameObject("Label "+text);g.transform.SetParent(parent?parent:room);g.transform.position=p;g.transform.rotation=rotation;var t=g.AddComponent<TextMesh>();t.font=uiFont;t.fontSize=80;t.characterSize=height/8f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;t.text=text;t.GetComponent<Renderer>().sharedMaterial=textMat;return t;
    }
    static BloomInteractable Target(string name,Bounds bounds,BloomAction action,int index=0)
    {
        var g=new GameObject(name);g.transform.position=bounds.center;g.transform.SetParent(room);var box=g.AddComponent<BoxCollider>();box.size=bounds.size;var i=g.AddComponent<BloomInteractable>();i.action=action;i.index=index;return i;
    }
    static Transform Hinge(Renderer r)
    {
        var b=r.bounds;var p=new GameObject(r.name+" hinge").transform;p.position=new Vector3(b.min.x,b.center.y,b.min.z);p.SetParent(room);r.transform.SetParent(p,true);
        var col=r.gameObject.AddComponent<MeshCollider>();col.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;return p;
    }

    [MenuItem("Bloom Boom/Build scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory(Output+"/Materials");Directory.CreateDirectory(Output+"/Meshes");Directory.CreateDirectory(Output+"/Previews");Directory.CreateDirectory(Output+"/Audio");
        AssetDatabase.Refresh();mats=new Dictionary<string,Material>();uiFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMat=new Material(Shader.Find("Bloom/WorldText"));textMat.mainTexture=uiFont.material.mainTexture;AssetDatabase.CreateAsset(textMat,Output+"/Materials/World font.mat");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        room=new GameObject("Bloom Avenue — walkable 3D mall").transform;
        var game=new GameObject("Bloom Boom — Game").AddComponent<BloomBoomGame>();game.font=uiFont;game.worldTextMaterial=textMat;
        game.memory=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/PhotoboothMemory.png");
        SetupMall();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Macanic.fbx");if(!model)throw new Exception("Missing export from macanic.blend");
        var root=(GameObject)PrefabUtility.InstantiatePrefab(model);PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.name="macanic.blend — original model (FBX export)";
        root.transform.localScale=Vector3.one*.34f;root.transform.position=new Vector3(-3.2684f,.01f,7.25126f);root.transform.SetParent(room);game.modelRoot=root.transform;
        Material pink=M("Macanic_Pink",C("C4809E")),dark=M("Macanic_Frame",C("35243C")),green=M("Macanic_Header",C("134D3E")),gold=M("Macanic_Gold",C("DBB454")),glass=M("Macanic_Glass",new Color(.64f,.85f,.88f,.105f),false,true),creamGlow=M("Macanic_LED",C("FFF0CD"),true);
        var renderers=root.GetComponentsInChildren<Renderer>();
        foreach(var r in renderers){string n=r.name;Material mat=pink;if(n.StartsWith("ramka_")||n.StartsWith("button_"))mat=dark;if(n=="Куб")mat=green;if(n=="Текст.001")mat=gold;if(n.StartsWith("text_"))mat=creamGlow;if(n.StartsWith("Door_"))mat=glass;if(n.StartsWith("Куб.")&&n!="Куб.015"&&n!="Куб.016")mat=creamGlow;if(n=="ramka_kupuripriemnika_green")mat=M("ReaderGreen",C("46DD59"),true);if(n=="Куб.016")mat=M("BoothSeat",C("274D43"));r.sharedMaterial=mat;}
        Renderer R(string n)=>renderers.First(r=>r.name==n);
        foreach(string n in new[]{"VendingMachine_Body","Цилиндр","Куб.015","Куб.016"}){var r=R(n);var col=r.gameObject.AddComponent<MeshCollider>();col.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;}
        // Keep the supplied screen shape. Put an emissive display just in front of its face.
        Bounds s=R("screen").bounds;
        game.screenMaterial=M("Display",C("FFFFFF"),true);
        game.catalogueScreen=Shape("Catalogue — bouquet image",new Vector3(s.center.x,s.center.y+.04f,s.min.z-.009f),new Vector3(s.size.x*.91f,s.size.y*.73f,.008f),game.screenMaterial,PrimitiveType.Quad,false).GetComponent<Renderer>();
        Shape("Catalogue caption background",new Vector3(s.center.x,s.min.y+.076f,s.min.z-.022f),new Vector3(s.size.x*.94f,.153f,.008f),M("Catalogue cream",C("F6E8DA"),true),PrimitiveType.Quad,false);
        game.catalogueTitle=Text("",new Vector3(s.center.x,s.min.y+.07f,s.min.z-.032f),.033f,C("163E32"),Quaternion.identity);
        Target("Browse screen",new Bounds(new Vector3(s.center.x,s.center.y,s.min.z-.03f),new Vector3(s.size.x,s.size.y,.04f)),BloomAction.Screen);
        foreach(var pair in new[]{("button_vlevo",BloomAction.Previous),("button_vpraro",BloomAction.Next),("button_ok",BloomAction.Confirm)}){Bounds b=R(pair.Item1).bounds;Target(pair.Item1+" clickable",new Bounds(new Vector3(b.center.x,b.center.y,b.min.z-.018f),new Vector3(Mathf.Max(.1f,b.size.x),.12f,.04f)),pair.Item2);Text(pair.Item2==BloomAction.Previous?"<":pair.Item2==BloomAction.Next?">":"OK",new Vector3(b.center.x,b.center.y,b.min.z-.015f),.047f,Color.white,Quaternion.identity);}
        Bounds t=R("screen_terminal").bounds;
        R("screen_terminal").sharedMaterial=M("TerminalDisplay",C("194C3C"),true);
        game.paymentText=Text("ОЖИДАНИЕ",new Vector3(t.center.x,t.center.y,t.min.z-.018f),.019f,C("F0FFCE"),Quaternion.identity);
        Target("Payment terminal",new Bounds(new Vector3(t.center.x,t.center.y,t.min.z-.025f),new Vector3(.19f,.23f,.06f)),BloomAction.Pay);
        var prototypes=new GameObject[9];var previews=new Texture[9];
        for(int k=0;k<9;k++){prototypes[k]=CreateBouquet(k);previews[k]=LivePreview(prototypes[k],k);}
        game.bouquets=new BouquetItem[18];string[] titles={"Розовый рассвет · розы","Солнце · подсолнухи","Облако · пионы","Весна · тюльпаны","Лето · ромашки","Сумерки · ирисы","Прованс · лаванда","Небо · гортензия","Признание · алые розы"};
        int[] prices={1800,2100,2700,1600,1400,2200,1700,2600,2400};
        for(int i=0;i<18;i++){
            var r=R($"Door_{i+1:00}");Bounds b=r.bounds;var hinge=Hinge(r);
            int kind=i%9;var f=UnityEngine.Object.Instantiate(prototypes[kind]);f.name=$"Bouquet {i+1:00} — {titles[kind]}";f.transform.position=new Vector3(b.center.x,b.min.y+.045f,b.center.z+.27f);f.transform.SetParent(room,true);
            var pickup=Target($"Collect bouquet {i+1:00}",new Bounds(new Vector3(b.center.x,b.center.y,b.min.z-.14f),new Vector3(b.size.x*.86f,b.size.y*.9f,.12f)),BloomAction.Pickup,i);pickup.gameObject.SetActive(false);
            game.bouquets[i]=new BouquetItem{title=titles[kind],price=prices[kind],preview=previews[kind],door=hinge,flowers=f,pickup=pickup};
            Text($"{i+1:00}",new Vector3(b.min.x+.055f,b.min.y+.045f,b.min.z-.008f),.036f,C("FFEDC2"),Quaternion.identity,hinge);
            Shape("Compartment light",new Vector3(b.center.x,b.max.y-.018f,b.center.z+.09f),new Vector3(b.size.x*.86f,.014f,.025f),creamGlow,PrimitiveType.Cube,false);
        }
        foreach(var proto in prototypes)UnityEngine.Object.DestroyImmediate(proto);
        Bounds door=R("Door_bootka").bounds;game.boothDoor=Hinge(R("Door_bootka"));var doorAction=R("Door_bootka").gameObject.AddComponent<BloomInteractable>();doorAction.action=BloomAction.BoothDoor;
        game.boothCenter=new GameObject("Booth interior").transform;game.boothCenter.position=new Vector3(door.center.x,0,7.55f);game.boothCenter.SetParent(room);
        // The original booth screen is on its left interior wall, facing into the booth (+X).
        Bounds bs=R("button_camera").bounds;Quaternion side=Quaternion.Euler(0,-90,0);
        game.boothText=Text("СНЯТЬ",new Vector3(bs.max.x+.025f,1.73f,bs.center.z),.095f,Color.white,side);
        R("button_camera").sharedMaterial=M("BoothScreen",C("1B3F35"),true);
        Target("Photo screen — Снять",new Bounds(new Vector3(bs.max.x+.025f,1.78f,bs.center.z),new Vector3(.06f,.72f,.58f)),BloomAction.Photo);
        var photoMat=M("PrintedPhoto",Color.white,true);photoMat.mainTexture=game.memory;
        game.paper=Shape("Printed photograph",new Vector3(bs.max.x+.14f,1.12f,bs.center.z),new Vector3(.27f,.34f,1),photoMat,PrimitiveType.Quad,false).transform;game.paper.rotation=side;
        var paperHit=game.paper.gameObject.AddComponent<BoxCollider>();paperHit.size=new Vector3(1,1,.04f);game.paper.gameObject.AddComponent<BloomInteractable>().action=BloomAction.Print;
        Shape("Printer slot",new Vector3(bs.max.x+.07f,.92f,bs.center.z),new Vector3(.1f,.06f,.38f),dark);
        Text("PHOTO CLUB",new Vector3(door.center.x,2.7f,5.89f),.16f,C("FFF0CD"),Quaternion.identity);
        Text("ФОТО С БУКЕТОМ\n50% ОБРАТНО",new Vector3(door.center.x,2.46f,5.89f),.064f,C("FFF0CD"),Quaternion.identity);
        Light boothLight=new GameObject("Booth softbox").AddComponent<Light>();boothLight.type=LightType.Point;boothLight.range=4;boothLight.intensity=2;boothLight.color=C("FFE0C6");boothLight.transform.position=new Vector3(door.center.x,2.8f,7.8f);boothLight.transform.SetParent(room);
        var p=new GameObject("Player — WASD + Input System");p.layer=2;p.transform.SetParent(room);game.player=p.AddComponent<CharacterController>();game.player.height=1.75f;game.player.center=new Vector3(0,.875f,0);game.player.radius=.23f;game.player.stepOffset=.22f;
        var cg=new GameObject("First person camera");cg.tag="MainCamera";cg.transform.SetParent(p.transform);cg.transform.localPosition=new Vector3(0,1.63f,0);game.playerCamera=cg.AddComponent<Camera>();game.playerCamera.fieldOfView=66;game.playerCamera.nearClipPlane=.04f;game.playerCamera.farClipPlane=150;game.playerCamera.clearFlags=CameraClearFlags.Skybox;game.playerCamera.cullingMask=~(1<<31);cg.AddComponent<AudioListener>();game.playerCamera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        game.spawn=new GameObject("Spawn at mall entrance").transform;game.spawn.position=new Vector3(-1,.02f,-3.5f);game.spawn.rotation=Quaternion.Euler(0,-3,0);game.spawn.SetParent(room);p.transform.position=game.spawn.position;
        game.musicClip=CreateAudio("Evening piano",24,0);game.paymentClip=CreateAudio("Payment approved",.65f,1);game.shutterClip=CreateAudio("Camera shutter",.28f,2);game.printClip=CreateAudio("Photo printer",2,3);
        game.footstepClips=new AudioClip[4];for(int i=0;i<4;i++)game.footstepClips[i]=CreateAudio("Shoe on marble "+i,.28f,4+i);
        game.walletClip=CreateAudio("Leather rustle",.55f,8);SetupWallet(game);
        game.appIcon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/BloomBoomIcon.png");
        // Explicit material references in the saved scene keep shaders included in the player.
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/BloomMall.unity");EditorSceneManager.SaveScene(scene,"Assets/Scenes/SampleScene.unity",true);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/BloomMall.unity",true)};
        PlayerSettings.companyName="BloomBoom";PlayerSettings.productName="Bloom Boom 3D";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        PlayerSettings.bundleVersion="2.0";
        PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown,new[]{game.appIcon},IconKind.Any);
        var iconSizes=PlayerSettings.GetIconSizes(UnityEditor.Build.NamedBuildTarget.Standalone,IconKind.Any);if(iconSizes.Length>0)PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Standalone,Enumerable.Repeat(game.appIcon,iconSizes.Length).ToArray(),IconKind.Any);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
        AssetDatabase.SaveAssets();Debug.Log("BLOOM_SCENE_BUILT: original model + fully serialized mall");
    }
    public static void BuildPlayer(){CreateScene();var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/BloomMall.unity"},locationPathName="Builds/BloomBoom3D-v2/BloomBoom.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);Debug.Log("BLOOM_BUILD_SUCCESS");}
}
