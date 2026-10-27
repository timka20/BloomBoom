using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable] public class BouquetItem
{
    public string title; public int price; public Texture preview;
    public Transform door; public GameObject flowers; public BloomInteractable pickup;
    [NonSerialized] public bool sold;
}
public partial class BloomBoomGame : MonoBehaviour
{
    public Camera playerCamera;
    public CharacterController player;
    public Transform spawn, modelRoot, boothDoor, boothCenter, paper;
    public BouquetItem[] bouquets;
    public Renderer catalogueScreen;
    public TextMesh catalogueTitle, paymentText, boothText;
    public Texture2D memory, appIcon;
    public Font font;
    public AudioClip musicClip, paymentClip, shutterClip, printClip, walletClip;
    public AudioClip[] footstepClips;
    public Material screenMaterial, worldTextMaterial;
    public BloomWallet wallet;
    public enum Stage { Title, Story, Choose, Payment, Paying, Opening, Pickup, PhotoChoice, GoToBooth, Countdown, Printing, PhotoReady, Complete }
    public Stage stage { get; private set; }
    public int selected { get; private set; }
    public int purchased=-1;
    public bool hasBouquet,hasPhoto,paused;
    public readonly List<int> inventory=new List<int>();
    public const int Capacity=6;
    public int photoCount, totalSpent, totalRefund;
    public bool lastDiscount;
    public bool IsInBooth => Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(boothCenter.position.x,boothCenter.position.z))<1.12f;
    bool Modal => stage==Stage.Title||stage==Stage.Story||stage==Stage.PhotoChoice||stage==Stage.Complete;
    BloomInteractable focused; AudioSource music,sound,steps; Material display;
    float yaw,pitch,gravity,typed,toastUntil,flash,stepDistance; int page,stepIndex;
    public int stepsPlayed { get; private set; }
    string toast; bool cursorMode,automation,initialized;
    Vector3 paperHome;
    readonly string[] chapters={
        "Сегодня именины у Лизы. Утром она написала: «Приходи к семи. И без твоего фирменного “я уже выхожу”, когда ты ещё дома».\n\nДо встречи осталось полчаса. Подарок есть. Открытка есть. А вот цветы…",
        "Ты заходишь в торговый центр. В воздухе пахнет кофе, за витринами загорается тёплый свет. Вдруг замечаешь розовый автомат: Bloom Boom.\n\nРозы, тюльпаны, пионы, подсолнухи… Лиза любит сюрпризы. Кажется, ты в правильном месте.",
        "Рядом — акция: «Фото с букетом — 50% стоимости обратно». Фотографироваться необязательно: после покупки ты сам решишь, идти в фотобудку или нет.\n\nКошелёк уже с тобой. Можно выбрать несколько букетов — в сумке шесть мест. Главное, не опоздать к Лизе!"};
    void Awake()
    {
        Application.targetFrameRate=60;QualitySettings.vSyncCount=1;Time.timeScale=1;stage=Stage.Title;
        if(!playerCamera||!player||!modelRoot||!wallet||bouquets==null||bouquets.Length!=18)throw new InvalidOperationException("Bloom scene references missing.");
        playerCamera.enabled=true;display=new Material(screenMaterial);catalogueScreen.sharedMaterial=display;
        Font.textureRebuilt+=RefreshFont;RefreshFont(font);
        music=gameObject.AddComponent<AudioSource>();music.clip=musicClip;music.loop=true;music.volume=.26f;music.Play();
        sound=gameObject.AddComponent<AudioSource>();sound.volume=.6f;steps=gameObject.AddComponent<AudioSource>();steps.volume=.42f;
        paperHome=paper.localPosition;paper.gameObject.SetActive(false);wallet.ResetPose();SetCursor(true);ResetPlayer();RefreshDisplay();initialized=true;
        Debug.Log("BLOOM_READY: original Macanic, 9 flower varieties, 6 slots, optional photo, animated wallet.");
        automation=Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--bloom-self-test");if(automation)StartCoroutine(SelfTest());
    }
    void RefreshFont(Font changed){if(changed==font&&worldTextMaterial)worldTextMaterial.mainTexture=font.material.mainTexture;}
    void OnDestroy(){Font.textureRebuilt-=RefreshFont;Time.timeScale=1;if(display)Destroy(display);}
    void Update()
    {
        if(!initialized)return;
        if(stage==Stage.Story)typed+=Time.deltaTime*37;
        flash=Mathf.MoveTowards(flash,0,Time.deltaTime*2);
        var k=Keyboard.current;var m=Mouse.current;if(k==null||m==null||automation)return;
        if(albumOpen){if(k.escapeKey.wasPressedThisFrame){albumOpen=false;Time.timeScale=1;SetCursor(false);}return;}
        if(k.escapeKey.wasPressedThisFrame&&!Modal)SetPaused(!paused);
        if(paused)return;
        if(stage==Stage.Title){if(k.enterKey.wasPressedThisFrame)BeginStory();return;}
        if(stage==Stage.Story){if(k.enterKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame)NextStory();return;}
        if(Modal)return;
        if(k.tabKey.wasPressedThisFrame)SetCursor(!cursorMode);
        if(k.qKey.wasPressedThisFrame)ToggleWallet();
        if(stage!=Stage.Countdown&&stage!=Stage.Printing&&stage!=Stage.Paying&&!wallet.Busy){
            if(!cursorMode){Vector2 d=m.delta.ReadValue();yaw+=d.x*.095f;pitch=Mathf.Clamp(pitch-d.y*.095f,-78,78);ApplyLook();}
            Move(new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)),k.leftShiftKey.isPressed);
        }
        FindFocus();bool uiHit=cursorMode&&PointerOverUI();
        if((k.eKey.wasPressedThisFrame||(m.leftButton.wasPressedThisFrame&&!uiHit))&&focused!=null)Activate(focused);
        if(stage==Stage.Choose&&Vector3.Distance(player.transform.position,catalogueScreen.bounds.center)<3){if(k.leftArrowKey.wasPressedThisFrame)Cycle(-1);if(k.rightArrowKey.wasPressedThisFrame)Cycle(1);if(k.enterKey.wasPressedThisFrame)ConfirmBouquet();}
    }
    public void Move(Vector2 input,bool fast=false)
    {
        gravity=player.isGrounded?-2:Mathf.Max(gravity-18*Time.deltaTime,-25);
        Vector3 before=player.transform.position,d=player.transform.forward*input.y+player.transform.right*input.x;
        player.Move((Vector3.ClampMagnitude(d,1)*(fast?4:2.7f)+Vector3.up*gravity)*Time.deltaTime);
        Vector3 delta=player.transform.position-before;delta.y=0;
        if(player.isGrounded&&input.sqrMagnitude>.01f&&delta.magnitude>.001f){
            stepDistance+=delta.magnitude;
            if(stepDistance>(fast?1.25f:1.05f)){
                stepDistance=0;stepsPlayed++;
                if(footstepClips!=null&&footstepClips.Length>0){steps.pitch=1+(stepIndex%3-1)*.045f;steps.PlayOneShot(footstepClips[stepIndex++%footstepClips.Length],fast?1:.72f);}
            }
        }else stepDistance=0;
    }
    void ApplyLook(){player.transform.rotation=Quaternion.Euler(0,yaw,0);playerCamera.transform.localRotation=Quaternion.Euler(pitch,0,0);}
    void ResetPlayer(){player.enabled=false;player.transform.position=spawn.position;player.enabled=true;yaw=spawn.eulerAngles.y;pitch=0;gravity=-2;stepDistance=0;ApplyLook();}
    void SetCursor(bool visible){cursorMode=visible;Cursor.lockState=visible?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=visible;}
    void SetPaused(bool value){paused=value;Time.timeScale=value?0:1;SetCursor(value||Modal);if(value)steps.Stop();}
    void OnApplicationFocus(bool focus){if(!focus){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}else if(initialized)SetCursor(cursorMode||paused);}
    public void BeginStory(){stage=Stage.Story;page=0;typed=0;}
    public void NextStory(){if(typed<chapters[page].Length){typed=chapters[page].Length;return;}if(++page<chapters.Length){typed=0;return;}EnterMall();}
    public void EnterMall(){stage=Stage.Choose;SetCursor(false);Notify("WASD — идти. Мышь — осмотреться. Подойдите к экрану автомата.",8);}
    public void Cycle(int direction){if(stage!=Stage.Choose)return;int sign=direction<0?-1:1;for(int n=0;n<Mathf.Abs(direction);n++){for(int i=0;i<bouquets.Length;i++){selected=(selected+sign+bouquets.Length)%bouquets.Length;if(!bouquets[selected].sold)break;}}RefreshDisplay();}
    public void ConfirmBouquet()
    {
        if(stage!=Stage.Choose)return;
        if(inventory.Count>=Capacity){Notify("Сумка заполнена: 6 из 6 букетов. Можно гулять по ТЦ или начать заново.",6);return;}
        if(bouquets[selected].sold){Notify("Этот букет уже куплен. Выберите другую ячейку.");return;}
        purchased=selected;lastDiscount=false;stage=Stage.Payment;paymentText.text="ПРИЛОЖИТЕ\nКАРТУ";
        Notify("Достаньте кошелёк: Q или Tab → нажмите кошелёк в инвентаре.",7);
    }
    public void CancelPurchase(){if(stage!=Stage.Payment||wallet.Busy)return;stage=Stage.Choose;purchased=-1;paymentText.text="ОЖИДАНИЕ";RefreshDisplay();}
    void RefreshDisplay(){var b=bouquets[selected];display.mainTexture=b.preview;catalogueTitle.text=$"{b.title}\n{b.price:N0} ₽  •  ЯЧЕЙКА {selected+1:00}";}
    public void ToggleWallet(){if(paused||Modal||wallet.Busy||(stage!=Stage.Choose&&stage!=Stage.Payment))return;StartCoroutine(wallet.IsOut?StowWallet():EquipWallet());}
    IEnumerator EquipWallet(){sound.PlayOneShot(walletClip,.55f);yield return wallet.Equip();Notify("Карта в руке. Наведитесь на терминал и нажмите ЛКМ / E.",5);}
    IEnumerator StowWallet(){sound.PlayOneShot(walletClip,.4f);yield return wallet.Stow();}
    public string Prompt(BloomInteractable target)
    {
        switch(target.action){
            case BloomAction.Previous:return stage==Stage.Choose?"Предыдущий букет":"";
            case BloomAction.Next:return stage==Stage.Choose?"Следующий букет":"";
            case BloomAction.Confirm:return stage==Stage.Choose?(inventory.Count>=Capacity?"Инвентарь заполнен (6 / 6)":"OK — выбрать этот букет"):"";
            case BloomAction.Screen:return stage==Stage.Choose?"Букеты: ← → листать, Enter / OK выбрать":"";
            case BloomAction.Pay:return stage==Stage.Payment?(wallet.CardReady?"Приложить карту к терминалу":"Достаньте кошелёк — Q / ячейка кошелька"):"";
            case BloomAction.Pickup:return stage==Stage.Pickup&&target.index==purchased?"Забрать букет":"";
            case BloomAction.BoothDoor:return stage==Stage.GoToBooth?"Дверь открыта — заходите":"Фото можно выбрать после покупки букета";
            case BloomAction.Photo:return stage==Stage.GoToBooth?(IsInBooth?"Снять фото с букетом":"Зайдите внутрь с букетом"):"";
            case BloomAction.Print:return stage==Stage.PhotoReady?"Забрать фотографию":"";
            default:return "";
        }
    }
    public void FindFocus(){focused=null;if(Modal||paused||wallet.Busy)return;Ray ray=cursorMode&&Mouse.current!=null?playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue()):new Ray(playerCamera.transform.position,playerCamera.transform.forward);if(Physics.Raycast(ray,out var hit,3.1f,~((1<<2)|(1<<30)|(1<<31)))){var t=hit.collider.GetComponentInParent<BloomInteractable>();if(t&&!string.IsNullOrEmpty(Prompt(t)))focused=t;}}
    public void Activate(BloomInteractable t)
    {
        if(t==null||paused||Modal||string.IsNullOrEmpty(Prompt(t)))return;
        switch(t.action){
            case BloomAction.Previous:Cycle(-1);break;
            case BloomAction.Next:Cycle(1);break;
            case BloomAction.Confirm:ConfirmBouquet();break;
            case BloomAction.Screen:Notify("Стрелки ← → или кнопки на корпусе. Enter / OK — выбрать.",6);break;
            case BloomAction.Pay:
                if(!wallet.CardReady){Notify("Сначала достаньте кошелёк — нажмите Q или его значок в инвентаре.");break;}
                if(Vector3.Distance(playerCamera.transform.position,t.transform.position)>1.8f){Notify("Подойдите чуть ближе к терминалу — нужно дотянуться картой.");break;}
                StartCoroutine(PayAndOpen(t.transform.position));break;
            case BloomAction.Pickup:StartCoroutine(Collect());break;
            case BloomAction.Photo:if(IsInBooth)StartCoroutine(TakePhoto());break;
            case BloomAction.Print:
                hasPhoto=true;photoCount++;lastDiscount=true;totalRefund+=bouquets[purchased].price/2;
                paper.gameObject.SetActive(false);boothText.text="СПАСИБО!";stage=Stage.Complete;focused=null;SetCursor(true);break;
        }
    }
    IEnumerator RotateDoor(Transform door,float degrees,float seconds=.85f){Quaternion from=door.localRotation,to=Quaternion.Euler(0,degrees,0);for(float t=0;t<seconds;t+=Time.deltaTime){door.localRotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,t/seconds));yield return null;}door.localRotation=to;}
    IEnumerator PayAndOpen(Vector3 terminal)
    {
        stage=Stage.Paying;focused=null;toastUntil=0;paymentText.text="ПОДНЕСИТЕ\nКАРТУ";
        yield return wallet.Tap(terminal,playerCamera);
        paymentText.text="ОПЛАТА\nУСПЕШНА";sound.PlayOneShot(paymentClip);totalSpent+=bouquets[purchased].price;bouquets[purchased].sold=true;
        yield return new WaitForSeconds(.5f);yield return wallet.Stow();
        stage=Stage.Opening;Notify($"Оплата успешна. Открывается ячейка {purchased+1:00}.",5);
        yield return RotateDoor(bouquets[purchased].door,105);bouquets[purchased].pickup.gameObject.SetActive(true);stage=Stage.Pickup;
    }
    IEnumerator Collect()
    {
        if(inventory.Contains(purchased)||inventory.Count>=Capacity)yield break;
        hasBouquet=true;inventory.Add(purchased);stage=Stage.Opening;focused=null;
        bouquets[purchased].flowers.SetActive(false);bouquets[purchased].pickup.gameObject.SetActive(false);
        yield return RotateDoor(bouquets[purchased].door,0);
        stage=Stage.PhotoChoice;SetCursor(true);
    }
    public void ChoosePhoto(bool useBooth)
    {
        if(stage!=Stage.PhotoChoice&&stage!=Stage.GoToBooth)return;
        focused=null;
        if(useBooth){stage=Stage.GoToBooth;boothText.text="СНЯТЬ";StartCoroutine(RotateDoor(boothDoor,-100,1.3f));SetCursor(false);Notify("Фотобудка справа. Зайдите внутрь и нажмите «Снять».",6);}
        else {lastDiscount=false;stage=Stage.Complete;SetCursor(true);}
    }
    public void ContinueShopping()
    {
        if(stage!=Stage.Complete)return;
        stage=Stage.Choose;focused=null;paymentText.text="ОЖИДАНИЕ";boothText.text="СНЯТЬ";Cycle(1);SetCursor(false);
        Notify(inventory.Count>=Capacity?"Все шесть мест заняты. Спасибо за покупки! Можно продолжить прогулку.":$"Выберите следующий букет. Свободно мест: {Capacity-inventory.Count}.",6);
    }
    IEnumerator TakePhoto(){stage=Stage.Countdown;focused=null;for(int n=3;n>0;n--){boothText.text=n.ToString();Notify(n.ToString(),1);yield return new WaitForSeconds(1);}sound.PlayOneShot(shutterClip);flash=1;boothText.text="ГОТОВО!\nПЕЧАТАЕМ…";stage=Stage.Printing;yield return new WaitForSeconds(.45f);sound.PlayOneShot(printClip);paper.localPosition=paperHome+Vector3.down*.29f;paper.gameObject.SetActive(true);for(float t=0;t<2;t+=Time.deltaTime){paper.localPosition=Vector3.Lerp(paperHome+Vector3.down*.29f,paperHome,t/2);yield return null;}paper.localPosition=paperHome;boothText.text="ЗАБЕРИТЕ\nФОТО ↓";stage=Stage.PhotoReady;Notify("Фотография напечатана! Заберите её под экраном.",6);}
    void Notify(string s,float seconds=4){toast=s;toastUntil=Time.time+seconds;}
    string Objective(){switch(stage){case Stage.Choose:return inventory.Count>=Capacity?"Сумка заполнена  /  6 прекрасных букетов с вами":"01  /  Выберите букет на экране автомата";case Stage.Payment:return wallet.CardReady?"02  /  Поднесите карту к терминалу — E / ЛКМ":"02  /  Достаньте кошелёк — Q или значок в инвентаре";case Stage.Paying:return "Оплата картой…";case Stage.Opening:case Stage.Pickup:return $"03  /  Заберите букет из ячейки {purchased+1:00}";case Stage.GoToBooth:return "04  /  Зайдите в фотобудку и нажмите «Снять»";case Stage.Countdown:return "Улыбнитесь!";case Stage.Printing:return "Печатаем ваше воспоминание…";case Stage.PhotoReady:return "05  /  Заберите напечатанное фото";default:return "";}}
    public void Restart()
    {
        StopAllCoroutines();Time.timeScale=1;wallet.ResetPose();
        foreach(var b in bouquets){b.sold=false;b.door.localRotation=Quaternion.identity;b.flowers.SetActive(true);b.pickup.gameObject.SetActive(false);}
        boothDoor.localRotation=Quaternion.identity;paper.gameObject.SetActive(false);paper.localPosition=paperHome;
        inventory.Clear();hasBouquet=hasPhoto=paused=lastDiscount=false;photoCount=totalSpent=totalRefund=0;
        purchased=-1;selected=0;focused=null;flash=0;toastUntil=0;albumOpen=false;paymentText.text="ОЖИДАНИЕ";boothText.text="СНЯТЬ";stage=Stage.Title;ResetPlayer();RefreshDisplay();SetCursor(true);
    }
}
