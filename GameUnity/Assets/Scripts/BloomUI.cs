using UnityEngine;
using UnityEngine.InputSystem;
public partial class BloomBoomGame
{
    GUIStyle label,heading,small,button;
    bool albumOpen;
    void Styles(){if(label!=null)return;label=new GUIStyle(GUI.skin.label){font=font,fontSize=20,wordWrap=true,normal={textColor=new Color(.99f,.96f,.92f)}};heading=new GUIStyle(label){fontSize=51,fontStyle=FontStyle.Bold};small=new GUIStyle(label){fontSize=14};button=new GUIStyle(GUI.skin.button){font=font,fontSize=20,fontStyle=FontStyle.Bold,normal={background=Texture2D.whiteTexture,textColor=new Color(.12f,.23f,.20f)},hover={background=Texture2D.whiteTexture,textColor=Color.black},active={background=Texture2D.whiteTexture,textColor=Color.black},border=new RectOffset(0,0,0,0)};}
    static Color Panel=>new Color(.055f,.12f,.105f,.96f);
    void Fill(Rect rect,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    bool Button(Rect rect,string text){GUI.backgroundColor=new Color(.94f,.72f,.71f);bool clicked=GUI.Button(rect,text,button);GUI.backgroundColor=Color.white;return clicked;}
    bool PointerOverUI(){var p=Mouse.current.position.ReadValue();p=new Vector2(p.x*1280/Screen.width,(Screen.height-p.y)*720/Screen.height);return new Rect(689,624,571,96).Contains(p)||new Rect(28,108,345,46).Contains(p);}
    void OnGUI()
    {
        if(!initialized)return;Styles();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
        if(stage==Stage.Title){
            Fill(new Rect(0,0,535,720),Panel);
            if(appIcon)GUI.DrawTexture(new Rect(48,52,48,48),appIcon,ScaleMode.ScaleToFit);
            GUI.Label(new Rect(112,64,400,40),"BLOOM BOOM / ЦВЕТЫ И МОМЕНТЫ",small);
            GUI.Label(new Rect(45,160,480,220),"Букет.\nМомент.\nТы.",heading);
            GUI.Label(new Rect(48,413,452,85),"Девять видов цветов. Шесть мест в сумке.\nИ маленькая история, которую выбираешь ты.",label);
            if(Button(new Rect(48,535,410,64),"▶   НАЧАТЬ"))BeginStory();
            GUI.Label(new Rect(48,637,475,55),"WASD — ходить  •  мышь — осматриваться\nE / ЛКМ — действие  •  Q — кошелёк  •  Esc — меню",small);return;
        }
        if(stage==Stage.Story){
            Fill(new Rect(45,350,1190,330),Panel);GUI.Label(new Rect(78,377,750,35),$"ИСТОРИЯ  /  0{page+1}",small);
            GUI.Label(new Rect(78,422,1110,155),chapters[page].Substring(0,Mathf.Min(chapters[page].Length,(int)typed)),label);
            if(Button(new Rect(939,594,255,54),typed<chapters[page].Length?"ПОКАЗАТЬ ТЕКСТ":page==2?"ВОЙТИ В ТЦ  →":"ДАЛЕЕ  →"))NextStory();return;
        }
        if(stage==Stage.PhotoChoice){
            Fill(new Rect(0,0,1280,720),new Color(.02f,.07f,.05f,.55f));Fill(new Rect(200,128,880,468),Panel);
            GUI.Label(new Rect(246,160,760,40),$"БУКЕТ В СУМКЕ  /  {inventory.Count} ИЗ {Capacity}",small);
            GUI.Label(new Rect(245,210,790,80),"Ещё одно воспоминание?",new GUIStyle(heading){fontSize=38});
            GUI.Label(new Rect(247,301,775,110),$"«{bouquets[purchased].title}» уже ваш.\nФото с букетом вернёт {bouquets[purchased].price/2:N0} ₽ — 50% его цены.\nА можно просто продолжить покупки: без фото и без скидки.",label);
            if(Button(new Rect(246,438,785,55),"В ФОТОБУДКУ  /  ВЕРНУТЬ 50%"))ChoosePhoto(true);
            if(Button(new Rect(246,511,785,49),"БЕЗ ФОТО И СКИДКИ"))ChoosePhoto(false);return;
        }
        if(stage==Stage.Complete){
            Fill(new Rect(0,0,1280,720),new Color(.04f,.10f,.085f,.96f));
            GUI.DrawTexture(new Rect(65,67,435,515),lastDiscount?(Texture)memory:bouquets[purchased].preview,ScaleMode.ScaleToFit);
            GUI.Label(new Rect(568,82,640,135),lastDiscount?"Ради таких\nмоментов.":"Ещё один\nповод улыбнуться.",heading);
            GUI.Label(new Rect(575,254,590,194),$"«{bouquets[purchased].title}» — в вашей сумке.\n\n"+(lastDiscount?$"Фото сохранено в альбоме.\nВозврат 50%: {bouquets[purchased].price/2:N0} ₽.":"Без фотографии — без скидки.\nБукет оплачен по полной стоимости.")+$"\n\nБукеты: {inventory.Count} / {Capacity}   •   Потрачено: {totalSpent-totalRefund:N0} ₽",label);
            if(Button(new Rect(575,507,570,62),"ПРОДОЛЖИТЬ ИГРУ  →"))ContinueShopping();
            if(Button(new Rect(575,587,570,48),"НАЧАТЬ ЗАНОВО"))Restart();
            GUI.Label(new Rect(75,627,430,35),"Только игровые платежи и скидки.",small);return;
        }
        if(albumOpen){
            Fill(new Rect(0,0,1280,720),Panel);GUI.DrawTexture(new Rect(190,30,480,650),memory,ScaleMode.ScaleToFit);
            GUI.Label(new Rect(727,200,480,150),"Твой маленький\nфотоальбом",new GUIStyle(heading){fontSize=39});
            GUI.Label(new Rect(731,360,450,80),$"Фотографий: {photoCount}\nВсего возвращено: {totalRefund:N0} ₽",label);
            if(Button(new Rect(730,491,410,60),"ЗАКРЫТЬ АЛЬБОМ")){albumOpen=false;Time.timeScale=1;SetCursor(false);}return;
        }
        Fill(new Rect(30,26,820,69),Panel);GUI.Label(new Rect(49,36,790,29),Objective(),label);
        GUI.Label(new Rect(49,68,790,21),"WASD — идти    E / ЛКМ — действие    Q — кошелёк    Tab — курсор    Esc — меню",small);
        if(stage==Stage.Payment&&cursorMode&&!paused){if(Button(new Rect(30,109,340,42),"ВЫБРАТЬ ДРУГОЙ БУКЕТ"))CancelPurchase();}
        if(stage==Stage.GoToBooth&&cursorMode&&!paused){if(Button(new Rect(30,109,340,42),"ПРОПУСТИТЬ ФОТО / БЕЗ СКИДКИ"))ChoosePhoto(false);}
        if(!cursorMode&&!paused)Fill(new Rect(637,357,6,6),focused?new Color(1,.7f,.65f):Color.white);
        if(focused!=null&&!paused){Fill(new Rect(30,649,650,55),Panel);GUI.Label(new Rect(45,659,623,41),"[E / ЛКМ]  "+Prompt(focused),label);}
        if(Time.time<toastUntil){Fill(new Rect(30,164,850,40),Panel);GUI.Label(new Rect(47,173,816,29),toast,small);}
        InventoryUI();
        if(flash>0)Fill(new Rect(0,0,1280,720),new Color(1,1,1,flash));
        if(paused){Fill(new Rect(0,0,1280,720),new Color(.035f,.075f,.06f,.86f));GUI.Label(new Rect(475,195,500,80),"Пауза",heading);if(Button(new Rect(440,315,400,62),"ПРОДОЛЖИТЬ"))SetPaused(false);if(Button(new Rect(440,393,400,55),"В НАЧАЛО"))Restart();if(Button(new Rect(440,465,400,55),"ВЫЙТИ"))Application.Quit();}
    }
    void InventoryUI()
    {
        Fill(new Rect(696,626,558,88),Panel);GUI.Label(new Rect(852,631,390,22),$"СУМКА  {inventory.Count} / {Capacity}   •   Tab — курсор",small);
        Rect wr=new Rect(710,653,60,51),ar=new Rect(779,653,60,51);
        bool interactive=cursorMode&&!paused;GUI.enabled=interactive&&!wallet.Busy&&(stage==Stage.Choose||stage==Stage.Payment);
        if(GUI.Button(wr,new GUIContent("",wallet.IsOut?"Убрать кошелёк [Q]":"Достать кошелёк [Q]"),GUIStyle.none))ToggleWallet();GUI.enabled=true;
        Fill(new Rect(wr.x+7,wr.y+9,46,31),wallet.IsOut?new Color(.66f,.44f,.29f):new Color(.42f,.28f,.19f));
        Fill(new Rect(wr.x+29,wr.y+20,23,12),new Color(.28f,.17f,.11f));Fill(new Rect(wr.x+43,wr.y+24,4,4),new Color(.91f,.74f,.4f));
        GUI.Label(new Rect(707,632,70,20),"КОШЕЛЁК",new GUIStyle(small){fontSize=11});
        GUI.enabled=interactive&&hasPhoto;if(GUI.Button(ar,new GUIContent("",$"Фотоальбом: {photoCount}"),GUIStyle.none)){albumOpen=true;Time.timeScale=0;SetCursor(true);}GUI.enabled=true;
        Fill(ar,new Color(.16f,.26f,.22f));if(hasPhoto)GUI.DrawTexture(new Rect(ar.x+10,ar.y+2,35,46),memory,ScaleMode.ScaleToFit);else GUI.Label(new Rect(ar.x+8,ar.y+17,50,25),"ФОТО",small);
        GUI.Label(new Rect(783,632,68,20),photoCount>0?$"ФОТО ×{photoCount}":"АЛЬБОМ",new GUIStyle(small){fontSize=11});
        for(int i=0;i<Capacity;i++){
            Rect r=new Rect(852+i*64,657,57,47);Fill(r,new Color(.18f,.29f,.24f));
            if(i<inventory.Count){var b=bouquets[inventory[i]];GUI.DrawTexture(r,b.preview,ScaleMode.ScaleToFit);if(interactive&&r.Contains(Event.current.mousePosition)){Fill(new Rect(852,592,387,28),Panel);GUI.Label(new Rect(865,598,370,25),b.title,small);}}
            else GUI.Label(new Rect(r.x+22,r.y+14,30,25),(i+1).ToString(),small);
        }
        if(interactive&&!string.IsNullOrEmpty(GUI.tooltip)){Fill(new Rect(703,593,450,29),Panel);GUI.Label(new Rect(716,598,428,23),GUI.tooltip,small);}
    }
}
