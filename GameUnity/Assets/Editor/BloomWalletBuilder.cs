using UnityEngine;
using UnityEngine.Rendering;

public static partial class BloomBuilder
{
    static Transform LocalPart(string name,Transform parent,Vector3 position,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube)
    {
        var g=Shape(name,Vector3.zero,size,mat,type,false,parent);
        g.transform.localPosition=position;g.transform.localRotation=Quaternion.identity;g.transform.localScale=size;
        g.layer=30;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return g.transform;
    }
    static Transform Group(string name,Transform parent,Vector3 position)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.gameObject.layer=30;return t;
    }
    static void SetupWallet(BloomBoomGame game)
    {
        var component=game.playerCamera.gameObject.AddComponent<BloomWallet>();game.wallet=component;
        var rig=Group("First person hands & leather wallet",game.playerCamera.transform,Vector3.zero);component.rig=rig;
        Material leather=M("Wallet espresso leather",C("593C30")),lining=M("Wallet suede lining",C("8A6250")),thread=M("Wallet saddle stitching",C("D5B391"));
        Material skin=M("Hands warm skin",C("DFA581")),nail=M("Hands nails",C("EBC0A4")),sleeve=M("Sleeves forest knit",C("243E36"));
        var w=Group("Wallet body",rig,new Vector3(-.12f,-.14f,.53f));w.localRotation=Quaternion.Euler(-16,-12,-10);component.leather=w;
        LocalPart("Rounded leather back",w,Vector3.zero,new Vector3(.18f,.128f,.018f),leather,PrimitiveType.Cube);
        LocalPart("Suede interior",w,new Vector3(0,0,-.012f),new Vector3(.167f,.115f,.005f),lining);
        var flap=Group("Folding leaf hinge",w,new Vector3(-.09f,0,0));component.flap=flap;
        LocalPart("Folding leather cover",flap,new Vector3(.09f,0,-.031f),new Vector3(.18f,.128f,.013f),leather);
        LocalPart("Folding suede interior",flap,new Vector3(.09f,0,-.022f),new Vector3(.164f,.112f,.004f),lining);
        for(int row=0;row<3;row++){
            LocalPart("Card pocket",w,new Vector3(0,-.023f-row*.015f,-.021f-row*.002f),new Vector3(.144f,.034f,.004f),leather);
            LocalPart("Pocket stitch",w,new Vector3(0,-.007f-row*.015f,-.024f-row*.002f),new Vector3(.139f,.0013f,.001f),thread);
        }
        for(int i=0;i<22;i++){
            float x=-.078f+i*.0074f;
            LocalPart("Saddle stitch",w,new Vector3(x,-.056f,-.012f),new Vector3(.0034f,.0012f,.0015f),thread);
            LocalPart("Cover stitch",flap,new Vector3(x+.09f,.055f,-.039f),new Vector3(.0034f,.0012f,.0015f),thread);
        }
        var card=Group("Bank card held between fingers",rig,Vector3.zero);component.card=card;
        LocalPart("Sage debit card",card,Vector3.zero,new Vector3(.096f,.062f,.0025f),M("Bank card sage",C("B6C8AE")));
        LocalPart("Card accent",card,new Vector3(0,.025f,-.0016f),new Vector3(.094f,.009f,.001f),M("Card forest stripe",C("264D3C")));
        LocalPart("Gold chip",card,new Vector3(-.026f,.003f,-.002f),new Vector3(.016f,.013f,.001f),M("Card gold chip",C("DAB55F")));
        for(int i=0;i<3;i++)LocalPart("Chip groove",card,new Vector3(-.030f+i*.004f,.003f,-.0027f),new Vector3(.0007f,.011f,.0005f),lining);
        for(int i=0;i<4;i++)LocalPart("Embossed card details",card,new Vector3(-.029f+i*.019f,-.016f,-.002f),new Vector3(.013f,.002f,.001f),M("Card type",C("F1E8CE")));
        component.leftHand=Hand(rig,"Left hand supporting wallet",new Vector3(-.15f,-.23f,.52f),skin,nail,true);
        component.leftHand.localRotation=Quaternion.Euler(-12,0,-18);
        component.rightHand=Hand(rig,"Right hand drawing card",new Vector3(.24f,-.255f,.49f),skin,nail,false);
        component.leftSleeve=LocalPart("Left sleeve",rig,Vector3.zero,Vector3.one,sleeve,PrimitiveType.Cylinder);
        component.rightSleeve=LocalPart("Right sleeve",rig,Vector3.zero,Vector3.one,sleeve,PrimitiveType.Cylinder);
        component.leftWrist=LocalPart("Left wrist",rig,Vector3.zero,Vector3.one,skin,PrimitiveType.Cylinder);
        component.rightWrist=LocalPart("Right wrist",rig,Vector3.zero,Vector3.one,skin,PrimitiveType.Cylinder);
        component.ResetPose();
    }
    static Transform Hand(Transform rig,string name,Vector3 p,Material skin,Material nail,bool left)
    {
        var h=Group(name,rig,p);
        LocalPart("Sculpted palm",h,Vector3.zero,new Vector3(.085f,.11f,.042f),skin,PrimitiveType.Sphere);
        for(int f=0;f<4;f++){
            float x=-.029f+f*.0195f,length=f==0||f==3?.033f:.039f;
            LocalPart("Knuckle",h,new Vector3(x,.045f,0),new Vector3(.020f,.025f,.025f),skin,PrimitiveType.Sphere);
            var a=LocalPart("Proximal finger",h,new Vector3(x,.064f,.004f),new Vector3(.0175f,length,.019f),skin,PrimitiveType.Capsule);a.localRotation=Quaternion.Euler(18,0,0);
            var b=LocalPart("Curled fingertip",h,new Vector3(x,.082f,.017f),new Vector3(.016f,.025f,.018f),skin,PrimitiveType.Capsule);b.localRotation=Quaternion.Euler(64,0,0);
            var n=LocalPart("Fingernail",h,new Vector3(x,.083f,.006f),new Vector3(.010f,.013f,.002f),nail,PrimitiveType.Sphere);n.localRotation=Quaternion.Euler(40,0,0);
        }
        float side=left?1:-1;
        var thumb=LocalPart("Thumb base",h,new Vector3(side*.045f,-.006f,-.005f),new Vector3(.029f,.054f,.027f),skin,PrimitiveType.Capsule);thumb.localRotation=Quaternion.Euler(-20,0,side*-32);
        var tip=LocalPart("Thumb holding prop",h,new Vector3(side*.053f,.026f,-.018f),new Vector3(.025f,.035f,.024f),skin,PrimitiveType.Capsule);tip.localRotation=Quaternion.Euler(-38,0,side*25);
        LocalPart("Thumbnail",h,new Vector3(side*.052f,.028f,-.031f),new Vector3(.014f,.018f,.002f),nail,PrimitiveType.Sphere);
        return h;
    }
}
