using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LoginArtSetup
{
    static LoginArtSetup() { EditorApplication.delayCall += Build; }
    static TMP_FontAsset font;
    static RectTransform R(string name, Transform parent, float x, float y, float w, float h)
    {
        var g = new GameObject(name, typeof(RectTransform));
        var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,.5f);
        r.anchoredPosition = new Vector2(x,y); r.sizeDelta = new Vector2(w,h); return r;
    }
    static TMP_Text T(Transform parent,string text,float x,float y,float w,float h)
    {
        var t=R("Text",parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font;t.text=text;t.fontSize=20;t.color=new Color(.84f,.93f,.8f);t.raycastTarget=false;t.alignment=TextAlignmentOptions.Midline;return t;
    }
    static TMP_InputField Input(Transform p,string name,float x,float y,float w,bool password,bool placeholder)
    {
        var r=R(name,p,x,y,w,40);var im=r.gameObject.AddComponent<Image>();im.color=new Color(.015f,.035f,.02f,.9f);
        var f=r.gameObject.AddComponent<TMP_InputField>();f.targetGraphic=im;
        var area=R("Text Area",r,0,0,w-16,36);area.gameObject.AddComponent<RectMask2D>();
        var text=T(area,"",0,0,w-16,36);text.alignment=TextAlignmentOptions.MidlineLeft;
        f.textViewport=area;f.textComponent=(TextMeshProUGUI)text;f.pointSize=22;f.characterLimit=32;
        if(placeholder){var ph=T(area,name,0,0,w-16,36);ph.color=new Color(.6f,.7f,.6f);f.placeholder=ph;}
        if(password)f.contentType=TMP_InputField.ContentType.Password;return f;
    }
    static Button B(Transform p,string label,float x,float y,float w,float h,bool overlay)
    {
        var r=R(label,p,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=Color.white;
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;var c=b.colors;
        c.normalColor=overlay?new Color(1,1,1,0):new Color(.17f,.24f,.14f);
        c.highlightedColor=overlay?new Color(.5f,1,.6f,.25f):new Color(.3f,.45f,.22f);
        c.pressedColor=overlay?new Color(0,0,0,.55f):new Color(.05f,.1f,.03f);
        c.selectedColor=c.highlightedColor;c.fadeDuration=.06f;b.colors=c;
        if(!overlay)T(r,label,0,0,w,h);return b;
    }
    static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("LoginArtDone",false))return;
        if(!System.IO.File.Exists("Assets/04.Images/UI/Login/CombatTerminal.png"))return;
        var scene=SceneManager.GetSceneByPath("Assets/01.Scenes/LoginScene.unity");
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/01.Scenes/LoginScene.unity",OpenSceneMode.Additive);
        foreach(var root in scene.GetRootGameObjects())if(root.name=="LoginCanvas")return;
        SessionState.SetBool("LoginArtDone",true);
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/08.Fonts/KERISKEDU_Line SDF.asset");
        foreach(var name in new[]{"CombatTerminal","ZombieBattle"}){var path="Assets/04.Images/UI/Login/"+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.npotScale=TextureImporterNPOTScale.None;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();}
        var canvas=R("LoginCanvas",null,0,0,1536,1024);SceneManager.MoveGameObjectToScene(canvas.gameObject,scene);
        var cv=canvas.gameObject.AddComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;cv.sortingOrder=200;
        var cs=canvas.gameObject.AddComponent<CanvasScaler>();cs.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;cs.referenceResolution=new Vector2(1536,1024);cs.matchWidthOrHeight=1;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        var bg=R("ZombieBattleBackground",canvas,0,0,0,0);bg.anchorMin=Vector2.zero;bg.anchorMax=Vector2.one;
        var bi=bg.gameObject.AddComponent<RawImage>();bi.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04.Images/UI/Login/ZombieBattle.png");bi.raycastTarget=false;
        var fg=R("CombatTerminalArtwork",canvas,0,0,1536,1024);var fi=fg.gameObject.AddComponent<RawImage>();fi.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04.Images/UI/Login/CombatTerminal.png");fi.raycastTarget=false;
        var lp=R("LoginPanel",canvas,0,0,1536,1024);var rp=R("RegisterPanel",canvas,-5,142,360,318);rp.gameObject.AddComponent<Image>().color=new Color(.025f,.05f,.03f);
        var lid=Input(lp,"아이디",-5,208,300,false,false);var lpw=Input(lp,"비밀번호",-5,104,300,true,false);
        var login=B(lp,"Login",-85,27,150,56,true);var open=B(lp,"Register",86,27,150,56,true);
        T(rp,"요원 등록",0,125,330,35);var rid=Input(rp,"아이디",0,75,310,false,true);var rpw=Input(rp,"비밀번호",0,22,310,true,true);var confirm=Input(rp,"비밀번호 확인",0,-31,310,true,true);
        var register=B(rp,"등록",-80,-98,145,40,false);var back=B(rp,"돌아가기",80,-98,145,40,false);
        var msg=T(canvas,"",0,-27,360,30);msg.fontSize=15;
        var logic=canvas.gameObject.AddComponent<UI_Login>();var so=new SerializedObject(logic);
        string[] keys={"_loginPanel","_registerPanel","_loginId","_loginPassword","_registerId","_registerPassword","_registerConfirmation","_messageText"};
        Object[] values={lp.gameObject,rp.gameObject,lid,lpw,rid,rpw,confirm,msg};
        for(int i=0;i<keys.Length;i++)so.FindProperty(keys[i]).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(login.onClick,logic.OnClickLogin);UnityEventTools.AddPersistentListener(open.onClick,logic.ShowRegister);UnityEventTools.AddPersistentListener(register.onClick,logic.OnClickRegister);UnityEventTools.AddPersistentListener(back.onClick,logic.ShowLogin);
        rp.gameObject.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText("Library/LoginArtSetup.done","Artwork, inputs, registration and button feedback saved.");
    }
}
