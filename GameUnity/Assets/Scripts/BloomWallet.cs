using System;
using System.Collections;
using UnityEngine;

// A real, camera-relative 3D prop. The card actually reaches the world-space reader.
public class BloomWallet : MonoBehaviour
{
    public Transform rig, leather, flap, card, leftHand, rightHand;
    public Transform leftSleeve,rightSleeve,leftWrist,rightWrist;
    public bool Busy { get; private set; }
    public bool IsOut { get; private set; }
    public bool CardReady { get; private set; }
    public Vector3 LastTapPosition { get; private set; }
    readonly Vector3 ready=new Vector3(.19f,-.105f,.52f);
    Vector3 Pocket=>leather.TransformPoint(new Vector3(.005f,.018f,-.026f));
    Quaternion PocketRotation=>leather.rotation;
    IEnumerator Tween(float seconds,Action<float> pose){for(float t=0;t<seconds;t+=Time.deltaTime){pose(Mathf.SmoothStep(0,1,t/seconds));yield return null;}pose(1);}
    void RightToCard(){rightHand.position=card.TransformPoint(new Vector3(.041f,-.048f,.002f));rightHand.rotation=card.rotation*Quaternion.Euler(0,0,-20);}
    public void ResetPose()
    {
        StopAllCoroutines();Busy=IsOut=CardReady=false;rig.gameObject.SetActive(false);rig.localPosition=Vector3.zero;
        flap.localRotation=Quaternion.identity;card.position=Pocket;card.rotation=PocketRotation;RightToCard();
    }
    public IEnumerator Equip()
    {
        if(Busy||IsOut)yield break;Busy=true;IsOut=true;CardReady=false;
        rig.gameObject.SetActive(true);flap.localRotation=Quaternion.identity;
        card.position=Pocket;card.rotation=PocketRotation;rightHand.localPosition=new Vector3(.12f,-.35f,.38f);
        yield return Tween(.55f,t=>rig.localPosition=Vector3.Lerp(new Vector3(0,-.65f,-.05f),Vector3.zero,t));
        Vector3 handStart=rightHand.position,edge=leather.TransformPoint(new Vector3(.06f,.035f,-.05f));
        yield return Tween(.32f,t=>rightHand.position=Vector3.Lerp(handStart,edge,t));
        yield return Tween(.7f,t=>{flap.localRotation=Quaternion.Euler(0,155*t,0);rightHand.position=flap.TransformPoint(new Vector3(.115f,.015f,-.055f));});
        handStart=rightHand.position;Vector3 grip=card.TransformPoint(new Vector3(.041f,-.048f,.002f));
        yield return Tween(.3f,t=>rightHand.position=Vector3.Lerp(handStart,grip,t));
        Vector3 start=rig.InverseTransformPoint(card.position);Quaternion rot=card.localRotation;
        yield return Tween(.7f,t=>{card.localPosition=Vector3.Lerp(start,ready,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.095f);card.localRotation=Quaternion.Slerp(rot,Quaternion.Euler(-8,-13,8),t);RightToCard();});
        CardReady=true;Busy=false;
    }
    public IEnumerator Tap(Vector3 terminal,Camera camera)
    {
        if(!CardReady||Busy)yield break;Busy=true;CardReady=false;
        Vector3 start=card.position,end=terminal+Vector3.back*.065f;
        Quaternion from=card.rotation,to=Quaternion.identity;
        yield return Tween(.9f,t=>{card.position=Vector3.Lerp(start,end,t)+camera.transform.up*(Mathf.Sin(t*Mathf.PI)*.05f);card.rotation=Quaternion.Slerp(from,to,t);RightToCard();});
        LastTapPosition=card.position;yield return new WaitForSeconds(.35f);Busy=false;
    }
    public IEnumerator Stow()
    {
        if(!IsOut)yield break;Busy=true;CardReady=false;
        Vector3 start=card.position;Quaternion rot=card.rotation;
        yield return Tween(.5f,t=>{card.position=Vector3.Lerp(start,rig.TransformPoint(ready),t);card.rotation=Quaternion.Slerp(rot,rig.rotation*Quaternion.Euler(-8,-13,8),t);RightToCard();});
        start=card.position;rot=card.rotation;
        yield return Tween(.6f,t=>{card.position=Vector3.Lerp(start,Pocket,t)+rig.up*(Mathf.Sin(t*Mathf.PI)*.08f);card.rotation=Quaternion.Slerp(rot,PocketRotation,t);RightToCard();});
        yield return Tween(.5f,t=>{flap.localRotation=Quaternion.Euler(0,155*(1-t),0);rightHand.position=flap.TransformPoint(new Vector3(.115f,.015f,-.055f));});
        yield return Tween(.45f,t=>rig.localPosition=Vector3.Lerp(Vector3.zero,new Vector3(0,-.65f,0),t));
        ResetPose();
    }
    void LateUpdate()
    {
        if(!IsOut)return;
        Arm(leftSleeve,leftWrist,new Vector3(-.36f,-.64f,.13f),leftHand);
        Arm(rightSleeve,rightWrist,new Vector3(.36f,-.64f,.13f),rightHand);
    }
    void Arm(Transform sleeve,Transform wrist,Vector3 shoulder,Transform hand)
    {
        Vector3 a=rig.TransformPoint(shoulder),b=hand.TransformPoint(new Vector3(0,-.043f,0)),mid=Vector3.Lerp(a,b,.78f);
        Segment(sleeve,a,mid,.064f);Segment(wrist,mid,b,.043f);
    }
    static void Segment(Transform t,Vector3 a,Vector3 b,float width){t.position=(a+b)/2;t.rotation=Quaternion.FromToRotation(Vector3.up,b-a);t.localScale=new Vector3(width,(b-a).magnitude/2,width);}
}
