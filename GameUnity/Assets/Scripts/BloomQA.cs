using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public partial class BloomBoomGame
{
    // Opt-in end-to-end test in the rendered Windows executable, not a mock scene.
    IEnumerator SelfTest()
    {
        string dir=Path.Combine(Application.dataPath,"..","QA");Directory.CreateDirectory(dir);
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        yield return new WaitForSeconds(1);yield return Capture(dir,"01-title-v2");
        var testKeyboard=InputSystem.AddDevice<Keyboard>();testKeyboard.MakeCurrent();
        yield return Press(testKeyboard,Key.Enter);Require(stage==Stage.Story,"Enter starts story through Input System");
        typed=9999;yield return Capture(dir,"02-story-v2");EnterMall();yield return new WaitForSeconds(.2f);
        Vector3 before=player.transform.position;automation=false;InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.W));yield return new WaitForSeconds(.9f);InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());yield return null;automation=true;
        Require(Vector3.Distance(before,player.transform.position)>.3f,"W moves the real first-person controller");Require(stepsPlayed>0,"Walking triggers footstep sounds");int stepCount=stepsPlayed;yield return new WaitForSeconds(.2f);Require(stepsPlayed==stepCount,"No footsteps while stationary");
        Require(bouquets.Select(b=>b.title).Distinct().Count()==9&&bouquets.Select(b=>b.preview).Distinct().Count()==9,"Nine distinct flower types with matching previews");
        Teleport(new Vector3(-1,.03f,3.8f),modelRoot.position+new Vector3(2,1.7f,0));yield return Capture(dir,"03-nine-varieties");
        Cycle(4);Require(selected==4,"Catalogue selects stocked cell");
        for(int order=0;order<Capacity;order++){
            int itemIndex=selected,oldSpent=totalSpent,oldRefund=totalRefund;
            ConfirmBouquet();Require(stage==Stage.Payment,"Order "+order+" selected");
            var pay=FindAction(BloomAction.Pay);Teleport(new Vector3(pay.transform.position.x-.18f,.03f,pay.transform.position.z-1.12f),pay.transform.position);
            yield return null;FindFocus();Require(focused==pay,"Terminal physics raycast");
            Activate(pay);Require(stage==Stage.Payment&&totalSpent==oldSpent,"Cannot pay without taking card from wallet");
            yield return Press(testKeyboard,Key.Q);
            Require(wallet.IsOut&&wallet.Busy,"Q equips 3D wallet and hands");
            if(order==0){
                yield return new WaitForSeconds(.85f);yield return Capture(dir,"04-wallet-opening");
                SetPaused(true);var flapBefore=wallet.flap.localRotation;yield return new WaitForSecondsRealtime(.15f);Require(Quaternion.Angle(flapBefore,wallet.flap.localRotation)<.01f,"Pause freezes wallet animation");SetPaused(false);
            }
            yield return WaitFor(()=>wallet.CardReady&&!wallet.Busy,6,"Card drawn from wallet");
            if(order==0)yield return Capture(dir,"05-card-in-hand");
            Aim(pay.transform.position);FindFocus();yield return Press(testKeyboard,Key.E);Require(stage==Stage.Paying,"E initiates card payment");
            Activate(pay);Require(stage==Stage.Paying,"Repeated input cannot duplicate payment");
            yield return new WaitForSeconds(1.03f);
            Require(Vector3.Distance(wallet.card.position,pay.transform.position)<.09f,"Card physically reaches terminal");
            if(order==0)yield return Capture(dir,"06-card-at-terminal");
            yield return WaitFor(()=>stage==Stage.Pickup,8,"Paid compartment opens");
            Require(totalSpent==oldSpent+bouquets[itemIndex].price&&!wallet.IsOut,"Charged exactly once and wallet returned");
            var item=bouquets[itemIndex];Teleport(new Vector3(item.pickup.transform.position.x,.03f,5.8f),item.pickup.transform.position);yield return null;FindFocus();
            if(focused!=item.pickup){
                var ray=new Ray(playerCamera.transform.position,playerCamera.transform.forward);
                var hits=Physics.RaycastAll(ray,3.1f,~((1<<2)|(1<<30)|(1<<31))).OrderBy(h=>h.distance);
                foreach(var hit in hits)Debug.Log("PICKUP_DIAGNOSTIC: "+hit.collider.name+" @ "+hit.point+" distance "+hit.distance);
                Debug.Log("PICKUP_TARGET: "+item.pickup.transform.position+" CAMERA: "+playerCamera.transform.position+" DIRECTION: "+ray.direction);
                yield return Capture(dir,"failure-pickup-"+itemIndex);
            }
            Require(focused==item.pickup,"Correct bouquet pickup raycast "+itemIndex);Activate(focused);Activate(item.pickup);
            yield return WaitFor(()=>stage==Stage.PhotoChoice,3,"Optional photo decision shown");
            Require(inventory.Count==order+1&&inventory.Last()==itemIndex&&!item.flowers.activeSelf,"Inventory retains each purchased bouquet");
            Require(Quaternion.Angle(item.door.localRotation,Quaternion.identity)<.1f,"Compartment closes after pickup");
            if(order==0)yield return Capture(dir,"07-optional-photo");
            bool usePhoto=order==1||order==2;ChoosePhoto(usePhoto);
            if(usePhoto){
                yield return new WaitForSeconds(1.4f);Teleport(new Vector3(boothCenter.position.x,.03f,5.0f),boothCenter.position+Vector3.up*1.5f);
                for(float time=0;time<3&&player.transform.position.z<boothCenter.position.z-.1f;time+=Time.deltaTime){Move(Vector2.up);yield return null;}
                Require(IsInBooth,"Walk through original booth doorway");var photo=FindAction(BloomAction.Photo);Aim(photo.transform.position);yield return null;FindFocus();Require(focused==photo,"Interior photo screen raycast");Activate(focused);
                yield return WaitFor(()=>stage==Stage.PhotoReady,8,"Countdown and physical print complete");Aim(paper.position);FindFocus();Require(focused!=null&&focused.action==BloomAction.Print,"Printed photo raycast");
                if(order==1)yield return Capture(dir,"08-printed-photo");Activate(focused);
                Require(stage==Stage.Complete&&lastDiscount&&totalRefund==oldRefund+item.price/2,"50% refunded exactly for photographed order");
                Activate(FindAction(BloomAction.Photo));Require(totalRefund==oldRefund+item.price/2,"Cannot duplicate photo refund");
            }else Require(stage==Stage.Complete&&!lastDiscount&&totalRefund==oldRefund,"Skip photo gives no discount");
            if(order==1)yield return Capture(dir,"09-photo-receipt");
            ContinueShopping();Require(stage==Stage.Choose&&inventory.Count==order+1&&!bouquets[selected].sold,"Continue game allows another in-stock bouquet");
        }
        int finalSpent=totalSpent;ConfirmBouquet();Require(stage==Stage.Choose&&totalSpent==finalSpent,"Full inventory blocks seventh purchase before charge");
        Require(inventory.Distinct().Count()==6&&bouquets.Count(b=>b.sold)==6,"Six separate sold cells, no duplicated inventory");Require(photoCount==2,"Photo booth reusable for next purchase");
        Teleport(new Vector3(-1,.03f,4.1f),catalogueScreen.bounds.center);yield return Capture(dir,"10-full-inventory");
        Restart();Require(inventory.Count==0&&!hasBouquet&&purchased==-1&&stage==Stage.Title&&!wallet.IsOut&&totalSpent==0&&totalRefund==0&&bouquets.All(b=>!b.sold),"Restart resets all orders, props, stock and receipts");
        InputSystem.RemoveDevice(testKeyboard);
        File.WriteAllText(Path.Combine(dir,"PASS.txt"),"PASS v2: keyboard movement + footsteps; nine flower types; card required; wallet equip/open/draw/tap/stow; pause; physical terminal reach; duplicate-input protection; six purchases; stock depletion; both optional-photo paths; two repeat photo sessions; 50% refunds; continuation; full inventory blocks charge; restart.");
        Debug.Log("BLOOM_SELF_TEST_PASS_V2");Application.Quit(0);
    }
    IEnumerator Press(Keyboard keyboard,Key key){InputSystem.EnableDevice(keyboard);keyboard.MakeCurrent();automation=false;InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;automation=true;}
    IEnumerator WaitFor(Func<bool> condition,float timeout,string message){float start=Time.realtimeSinceStartup;while(!condition()&&Time.realtimeSinceStartup-start<timeout)yield return null;Require(condition(),message);}
    BloomInteractable FindAction(BloomAction a){foreach(var t in FindObjectsByType<BloomInteractable>())if(t.action==a)return t;throw new Exception("Missing interaction "+a);}
    void Require(bool success,string message){if(!success){Debug.LogError("BLOOM_SELF_TEST_FAIL: "+message);Application.Quit(2);throw new Exception(message);}Debug.Log("CHECK_PASS: "+message);}
    void Teleport(Vector3 pos,Vector3 aim){player.enabled=false;player.transform.position=pos;player.enabled=true;Aim(aim);Physics.SyncTransforms();}
    void Aim(Vector3 point){Vector3 e=Quaternion.LookRotation(point-playerCamera.transform.position).eulerAngles;yaw=e.y;pitch=e.x>180?e.x-360:e.x;ApplyLook();}
    IEnumerator Capture(string dir,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));yield return new WaitForSeconds(.2f);}
}
