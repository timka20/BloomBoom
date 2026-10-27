using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static partial class BloomBuilder
{
    static void SetupMall()
    {
        var stone=M("Ivory stone",C("E6DDD0"));var tile=M("Porcelain",C("F4EEE5"));var trim=M("Bronze",C("84745D"));var dark=M("Dark metal",C("253A35"));var wood=M("Oak",C("A17851"));var wall=M("Warm plaster",C("D3C7B3"));var glow=M("Warm light",C("FFF0CD"),true);var glass=M("Store glass",new Color(.6f,.8f,.85f,.13f),false,true);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=C("C9D9E1");RenderSettings.ambientEquatorColor=C("A6A094");RenderSettings.ambientGroundColor=C("6F695E");RenderSettings.ambientIntensity=1;
        var hdr=AssetDatabase.LoadAssetAtPath<Texture>("Assets/Art/Environment/RoyalEsplanade.hdr");var sky=new Material(Shader.Find("Skybox/Panoramic"));sky.SetTexture("_MainTex",hdr);sky.SetFloat("_Exposure",.7f);AssetDatabase.CreateAsset(sky,Output+"/Materials/Mall panorama.mat");RenderSettings.skybox=sky;
        var sun=new GameObject("Atrium daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.color=C("FFF3E1");sun.intensity=1.25f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-28,0);sun.transform.SetParent(room);
        Shape("Solid walkable floor",new Vector3(0,-.12f,0),new Vector3(30,.2f,38),stone);
        for(int x=-7;x<8;x++)for(int z=-9;z<10;z++)Shape("Porcelain tile",new Vector3(x*2,-.01f,z*2),new Vector3(1.975f,.018f,1.975f),((x+z)%5==0)?stone:tile,PrimitiveType.Cube,false);
        Shape("Centre inlay L",new Vector3(-6,.006f,0),new Vector3(.045f,.006f,36),trim,PrimitiveType.Cube,false);Shape("Centre inlay R",new Vector3(6,.006f,0),new Vector3(.045f,.006f,36),trim,PrimitiveType.Cube,false);
        Shape("Rear feature wall",new Vector3(0,3.9f,12),new Vector3(30,7.8f,.3f),wall);
        Shape("Rear green inset",new Vector3(-1,2.2f,11.8f),new Vector3(14,4.4f,.12f),M("Feature green",C("264D42")));
        Text("B L O O M   A V E N U E",new Vector3(0,6.25f,11.7f),.48f,C("395145"),Quaternion.identity);
        Text("ЦВЕТЫ  /  КОФЕ  /  МАЛЕНЬКИЕ РАДОСТИ",new Vector3(0,5.5f,11.68f),.11f,C("395145"),Quaternion.identity);
        Shape("Entrance limit",new Vector3(0,2,-18),new Vector3(30,4,.1f),glass);
        Shape("Left boundary",new Vector3(-14,3,0),new Vector3(.2f,6,38),wall);Shape("Right boundary",new Vector3(14,3,0),new Vector3(.2f,6,38),wall);
        string[] shops={"ATELIER","NORD / HOME","MAISON","CAFÉ CRÈME","BOOK & CO","SUNDAY"};
        for(int side=-1;side<=1;side+=2){
            for(int i=0;i<3;i++){
                float z=-10+i*8;float x=side*11.8f;
                var shopWall=M("Shop warm "+i,new[]{C("B9AC91"),C("B5B5A0"),C("C7AAA0")}[i]);
                Shape("Shop back wall",new Vector3(side*13.6f,1.8f,z),new Vector3(.1f,3.6f,6.8f),shopWall,PrimitiveType.Cube,false);
                Shape("Shop dividing wall",new Vector3(x,1.8f,z-3.45f),new Vector3(4,3.6f,.14f),shopWall,PrimitiveType.Cube,false);
                Shape("Shop ceiling",new Vector3(x,3.55f,z),new Vector3(4,.1f,6.8f),glow,PrimitiveType.Cube,false);
                Shape("Glass shopfront",new Vector3(side*9.75f,1.7f,z),new Vector3(.04f,3.3f,6.45f),glass);
                Shape("Shop sign fascia",new Vector3(side*9.7f,3.9f,z),new Vector3(.25f,.8f,7.3f),dark);
                Quaternion q=Quaternion.Euler(0,side<0?-90:90,0);
                Text(shops[i+(side>0?3:0)],new Vector3(side*9.54f,3.88f,z),.23f,C("F6EDDB"),q);
                for(int j=-1;j<=1;j++){
                    Shape("Window mullion",new Vector3(side*9.7f,1.7f,z+j*2.15f),new Vector3(.10f,3.5f,.075f),trim);
                    Shape("Window display plinth",new Vector3(side*10.05f,.6f,z+j*1.75f),new Vector3(.6f,1.2f,.62f),stone);
                    Shape("Display vessel",new Vector3(side*10.05f,1.42f,z+j*1.75f),new Vector3(.29f,.37f,.29f),M("Ceramic",C("DBC3A4")),PrimitiveType.Cylinder,false);
                }
            }
            Shape("Mezzanine ledge",new Vector3(side*11,4.65f,0),new Vector3(5,.24f,35),stone);
            Shape("Upper balustrade glass",new Vector3(side*8.6f,5.3f,0),new Vector3(.04f,1.12f,35),glass,PrimitiveType.Cube,false);
            Shape("Upper handrail",new Vector3(side*8.6f,5.9f,0),new Vector3(.08f,.08f,35),trim,PrimitiveType.Cube,false);
            for(int z=-14;z<=10;z+=8){
                Shape("Atrium column",new Vector3(side*8.25f,3.7f,z),new Vector3(.65f,7.4f,.65f),stone);
                Shape("Column base",new Vector3(side*8.25f,.15f,z),new Vector3(.82f,.3f,.82f),dark);
            }
        }
        Shape("Atrium ceiling",new Vector3(0,8.1f,0),new Vector3(28,.2f,38),stone,PrimitiveType.Cube,false);
        for(int z=-12;z<=12;z+=4){Shape("Roof crossbeam",new Vector3(0,7.65f,z),new Vector3(28,.25f,.2f),stone,PrimitiveType.Cube,false);Shape("Ceiling skylight",new Vector3(0,7.88f,z),new Vector3(15,.03f,3.7f),M("Skylight",C("E0EBEA"),true),PrimitiveType.Cube,false);}
        for(int z=-10;z<=7;z+=6){
            for(int side=-1;side<=1;side+=2){
                float x=side*6.5f;Shape("Oak bench",new Vector3(x,.48f,z),new Vector3(.7f,.16f,2.2f),wood);Shape("Bench base",new Vector3(x,.21f,z),new Vector3(.45f,.43f,1.9f),dark);
                Plant(new Vector3(x,0,z+1.9f));
                var l=new GameObject("Warm ceiling pool").AddComponent<Light>();l.type=LightType.Point;l.range=8;l.intensity=2;l.color=C("FFE5C5");l.transform.position=new Vector3(x,5.9f,z);l.transform.SetParent(room);
                Shape("Pendant",new Vector3(x,6.3f,z),new Vector3(.85f,.06f,.85f),glow,PrimitiveType.Cylinder,false);
            }
        }
        Text("BLOOM BOOM  →",new Vector3(-5.6f,2.0f,3.8f),.13f,C("1C4335"),Quaternion.identity);
        Shape("Welcome sign",new Vector3(-5.6f,1.35f,3.85f),new Vector3(1.3f,1.5f,.12f),stone);
        Text("ЦВЕТЫ\n24 / 7",new Vector3(-5.6f,1.4f,3.77f),.16f,C("214E3F"),Quaternion.identity);
    }
    static void Plant(Vector3 p)
    {
        var pot=M("Planter",C("E6CEBC"));var leaf=M("Plant green",C("3E6244"));var bark=M("Bark",C("66523C"));
        Shape("Planter",p+Vector3.up*.36f,new Vector3(.85f,.36f,.85f),pot,PrimitiveType.Cylinder);
        Shape("Trunk",p+Vector3.up*1.28f,new Vector3(.09f,.9f,.09f),bark,PrimitiveType.Cylinder,false);
        for(int i=0;i<7;i++){float a=i*2.4f;Shape("Leaf canopy",p+new Vector3(Mathf.Cos(a)*.35f,1.8f+(i%3)*.22f,Mathf.Sin(a)*.35f),new Vector3(.65f,.55f,.65f),leaf,PrimitiveType.Sphere,false);}
    }
    static GameObject CreateBouquet(int kind)
    {
        var root=new GameObject("Bouquet prototype "+kind);var old=room;room=root.transform;
        string[] wraps={"E8BABC","D3B891","EDDCE4","CFDBC7","E8D7B0","C0BADA","CEBCD9","B9D6D8","E8CBB6"};
        string[] petals={"E74D78","F2B928","EFABC2","F18A55","FFF7E5","7751BD","8D74C5","739CD8","B92344"};
        string[] lights={"FFA7BD","FFE278","FFE2ED","FFC197","FFFFFF","B9A1EE","C5AFEF","C1D9EF","EE5973"};
        Material stem=M("Bouquet stems",C("3C6543")),leaf=M("Bouquet leaves",C("618351")),wrap=M("Wrapping "+kind,C(wraps[kind])),petal=M("Petals "+kind,C(petals[kind])),petalLight=M("Petal highlights "+kind,C(lights[kind]));
        var mesh=new Mesh();var vertices=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<=24;i++){float a=i*Mathf.PI*2/24;vertices.Add(new Vector3(Mathf.Cos(a)*.055f,.035f,Mathf.Sin(a)*.055f));vertices.Add(new Vector3(Mathf.Cos(a)*.22f,.32f+(i%2)*.025f,Mathf.Sin(a)*.22f));}
        for(int i=0;i<24;i++){int n=i*2;tris.AddRange(new[]{n,n+1,n+2,n+1,n+3,n+2,n+2,n+1,n,n+2,n+3,n+1});}
        mesh.SetVertices(vertices);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,$"{Output}/Meshes/Wrap{kind}.asset");var paper=new GameObject("Florist paper");paper.transform.SetParent(root.transform);paper.AddComponent<MeshFilter>().sharedMesh=mesh;paper.AddComponent<MeshRenderer>().sharedMaterial=wrap;
        var rnd=new System.Random(100+kind);
        int flowers=kind==6?15:kind==7?5:kind==1?5:9;
        for(int i=0;i<flowers;i++){
            float a=i*2.39996f,rad=i==0?0:kind==6?.15f:.13f;Vector3 head=new Vector3(Mathf.Cos(a)*rad,.40f+(float)rnd.NextDouble()*.10f,Mathf.Sin(a)*rad);
            Vector3 bottom=new Vector3(head.x*.1f,.06f,head.z*.1f);var stalk=Shape("Stem",(head+bottom)/2,new Vector3(.008f,(head-bottom).magnitude/2,.008f),stem,PrimitiveType.Cylinder,false);stalk.transform.rotation=Quaternion.FromToRotation(Vector3.up,head-bottom);
            for(int l=0;l<2;l++){var g=Shape("Leaf",new Vector3(head.x*.6f,head.y*.5f+l*.065f,head.z*.6f),new Vector3(.09f,.02f,.035f),leaf,PrimitiveType.Sphere,false);g.transform.rotation=Quaternion.Euler(0,i*80+l*110,25);}
            if(kind==6){ // Lavender has tall tapered spikes, not rose-shaped heads.
                for(int tier=0;tier<6;tier++)for(int n=0;n<4;n++){float angle=n*Mathf.PI/2+tier*.7f;float r=.017f*(1-tier*.1f);Shape("Lavender floret",head+new Vector3(Mathf.Cos(angle)*r,tier*.019f-.04f,Mathf.Sin(angle)*r),new Vector3(.022f,.027f,.020f),tier%2==0?petal:petalLight,PrimitiveType.Sphere,false);}
            }else if(kind==7){
                for(int bud=0;bud<17;bud++){float angle=bud*2.4f,yy=(bud/17f-.5f)*.11f,rr=Mathf.Sqrt(Mathf.Max(.001f,.0036f-yy*yy));Vector3 center=head+new Vector3(Mathf.Cos(angle)*rr,yy,Mathf.Sin(angle)*rr);for(int n=0;n<4;n++){float ph=n*Mathf.PI/2;Shape("Hydrangea cluster petals",center+new Vector3(Mathf.Cos(ph)*.010f,0,Mathf.Sin(ph)*.010f),new Vector3(.024f,.014f,.024f),bud%2==0?petal:petalLight,PrimitiveType.Sphere,false);}}
            }else if(kind==3){
                for(int n=0;n<6;n++){float angle=n*Mathf.PI/3;var g=Shape("Tulip cup petal",head+new Vector3(Mathf.Cos(angle)*.024f,.02f,Mathf.Sin(angle)*.024f),new Vector3(.039f,.09f,.029f),n%2==0?petal:petalLight,PrimitiveType.Sphere,false);g.transform.rotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,12);}
            }else{
                int layers=kind==0||kind==8?3:kind==2?4:1;
                for(int layer=0;layer<layers;layer++){
                    int count=kind==1?15:kind==4?12:kind==5?6:9-layer;
                    float radius=(kind==1?.060f:kind==4?.038f:kind==5?.038f:.041f)*(1-layer*.18f);
                    for(int n=0;n<count;n++){float angle=n*Mathf.PI*2/count+layer*.6f;Vector3 off=new Vector3(Mathf.Cos(angle)*radius,layer*.018f,Mathf.Sin(angle)*radius);Vector3 scale=kind==1?new Vector3(.073f,.015f,.032f):kind==4?new Vector3(.055f,.01f,.019f):kind==5?new Vector3(.077f,.015f,.040f):new Vector3(.048f,.030f,.038f);var g=Shape("Shaped flower petal",head+off,scale,(n+layer)%3==0?petalLight:petal,PrimitiveType.Sphere,false);g.transform.rotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,kind==5?(n%2==0?45:-30):layer*18);}
                }
                Shape("Flower heart",head+Vector3.up*(layers*.012f),kind==1?new Vector3(.062f,.025f,.062f):Vector3.one*.027f,kind==1?M("Sunflower heart",C("513423")):kind==4?M("Daisy pollen",C("E8B932")):petalLight,PrimitiveType.Sphere,false);
            }
        }
        Shape("Satin ribbon",new Vector3(0,.12f,0),new Vector3(.12f,.035f,.12f),M("Ribbon",C("873F59")),PrimitiveType.Cylinder,false);
        // Merge by material: inexpensive draw calls with exactly the same geometry in previews and shelves.
        var filters=root.GetComponentsInChildren<MeshFilter>();int idx=0;
        var petalMesh=FlowerSphere();foreach(var f in filters)if(f.sharedMesh.name=="Sphere")f.sharedMesh=petalMesh;
        foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray()){
            var combined=new Mesh{indexFormat=IndexFormat.UInt32};combined.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray());combined.RecalculateBounds();AssetDatabase.CreateAsset(combined,$"{Output}/Meshes/Bouquet{kind}_{idx++}.asset");var g=new GameObject(group.Key.name);g.transform.SetParent(root.transform);g.AddComponent<MeshFilter>().sharedMesh=combined;g.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var f in filters)UnityEngine.Object.DestroyImmediate(f.gameObject);room=old;return root;
    }
    static Mesh FlowerSphere()
    {
        string path=Output+"/Meshes/FlowerSphere.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
        var v=new List<Vector3>();var n=new List<Vector3>();var triangles=new List<int>();
        const int rings=8,slices=12;
        for(int y=0;y<=rings;y++)for(int x=0;x<=slices;x++){float p=y*Mathf.PI/rings,a=x*Mathf.PI*2/slices;var normal=new Vector3(Mathf.Sin(p)*Mathf.Cos(a),Mathf.Cos(p),Mathf.Sin(p)*Mathf.Sin(a));v.Add(normal*.5f);n.Add(normal);}
        for(int y=0;y<rings;y++)for(int x=0;x<slices;x++){int a=y*(slices+1)+x,b=a+slices+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
        var mesh=new Mesh{name="Efficient smooth petals"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static RenderTexture LivePreview(GameObject prototype,int kind)
    {
        var root=UnityEngine.Object.Instantiate(prototype);root.name="Catalogue studio "+kind;root.transform.position=new Vector3(400+kind*5,0,0);root.transform.SetParent(room,true);
        foreach(Transform t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
        var rt=new RenderTexture(384,512,24);rt.name="Bouquet view "+kind;AssetDatabase.CreateAsset(rt,$"{Output}/Previews/Bouquet{kind}.renderTexture");
        var c=new GameObject("Catalogue camera "+kind).AddComponent<Camera>();c.transform.SetParent(room);c.transform.position=new Vector3(400+kind*5,.53f,-1.4f);c.transform.LookAt(new Vector3(400+kind*5,.27f,0));c.orthographic=true;c.orthographicSize=.35f;c.nearClipPlane=.03f;c.farClipPlane=5;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=C("F6E8DA");c.cullingMask=1<<31;c.depth=-10+kind;c.targetTexture=rt;
        return rt;
    }
    static Texture2D RenderBouquet(GameObject root,int kind)
    {
        root.transform.position=new Vector3(400,0,0);foreach(Transform t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
        var c=new GameObject("Preview camera").AddComponent<Camera>();c.transform.position=new Vector3(400,.53f,-1.4f);c.transform.LookAt(new Vector3(400,.27f,0));c.orthographic=true;c.orthographicSize=.35f;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=C("F6E8DA");c.cullingMask=1<<31;
        var rt=new RenderTexture(384,512,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(384,512,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,384,512),0,0);tex.Apply();string path=$"{Output}/Previews/Bouquet{kind}.png";File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(c.gameObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
        root.transform.position=Vector3.zero;foreach(Transform t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=0;AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static AudioClip CreateAudio(string name,float duration,int mode)
    {
        const int rate=22050;int count=(int)(rate*duration);string path=$"{Output}/Audio/{name}.wav";var random=new System.Random(91);float[] data=new float[count];
        if(mode==0){int[][] chords={new[]{60,64,67,71},new[]{57,60,64,67},new[]{53,57,60,64},new[]{55,59,62,67}};for(int beat=0;beat<48;beat++){int midi=chords[(beat/12)%4][beat%4]+(beat%8==7?12:0);float hz=440*Mathf.Pow(2,(midi-69)/12f);int start=beat*rate/2;for(int j=0;j<rate*3&&start+j<count;j++){float t=j/(float)rate;float env=Mathf.Min(t/.015f,1)*Mathf.Exp(-t*2.2f);data[start+j]+=(Mathf.Sin(6.283185f*hz*t)+.3f*Mathf.Sin(6.283185f*hz*2*t)+.12f*Mathf.Sin(6.283185f*hz*3*t))*env*.12f;}}}
        else if(mode>=4){
            float filtered=0;
            for(int i=0;i<count;i++){float t=i/(float)rate;float noise=(float)random.NextDouble()*2-1;filtered=Mathf.Lerp(filtered,noise,.18f);
                if(mode==8)data[i]=filtered*.32f*Mathf.Sin(Mathf.PI*t/duration)*(0.5f+.5f*Mathf.Sin(t*44));
                else {float heel=Mathf.Exp(-t*46)*Mathf.Min(t*650,1),toe=t>.074f?Mathf.Exp(-(t-.074f)*58):0;data[i]=Mathf.Sin(t*6.283185f*(105+(mode-4)*13))*.35f*heel+filtered*(heel*.3f+toe*.15f);}
            }
        }
        else for(int i=0;i<count;i++){float t=i/(float)rate;data[i]=mode==1?Mathf.Sin(6.283185f*(t<.22f?660:990)*t)*.23f*Mathf.Min(t*60,1)*Mathf.Exp(-t*4):mode==2?((float)random.NextDouble()*2-1)*.38f*Mathf.Exp(-t*19):((float)random.NextDouble()-.5f)*.04f+Mathf.Sin(t*6.283185f*105)*.04f;}
        using(var w=new BinaryWriter(File.Create(path))){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);foreach(float f in data)w.Write((short)(Mathf.Clamp(f,-1,1)*32760));}
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
