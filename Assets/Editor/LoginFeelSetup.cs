using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LoginFeelSetup
{
    static LoginFeelSetup() { EditorApplication.delayCall += Apply; }
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || System.IO.File.Exists("Library/LoginFeel.done")) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/LoginScene.unity");
        if (!scene.isLoaded) return;
        Transform canvas = null;
        foreach (var root in scene.GetRootGameObjects()) if (root.name == "LoginCanvas") canvas = root.transform;
        if (canvas == null) return;
        System.IO.Directory.CreateDirectory("Assets/07.Animations/LoginFeel");
        AssetDatabase.Refresh();
        var group = new GameObject("TerminalMotion", typeof(RectTransform));
        var rt = group.GetComponent<RectTransform>(); rt.SetParent(canvas, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = new Vector2(1536, 1024);
        foreach (var name in new[] { "CombatTerminalArtwork", "LoginPanel", "RegisterPanel" })
        {
            var child = canvas.Find(name); if (child != null) child.SetParent(rt, false);
        }
        var clip = new AnimationClip(); clip.frameRate = 30;
        clip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.y", new AnimationCurve(new Keyframe(0, 0), new Keyframe(2, 3), new Keyframe(4, 0)));
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, "Assets/07.Animations/LoginFeel/TerminalIdle.anim");
        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/07.Animations/LoginFeel/TerminalIdle.controller");
        controller.AddMotion(clip); group.AddComponent<Animator>().runtimeAnimatorController = controller;
        var led = new GameObject("StatusLightPulse", typeof(RectTransform), typeof(Image));
        var lr = led.GetComponent<RectTransform>(); lr.SetParent(rt, false); lr.anchoredPosition = new Vector2(142, 332); lr.sizeDelta = new Vector2(10, 10);
        var image = led.GetComponent<Image>(); image.color = new Color(1, .6f, .1f, .2f); image.raycastTarget = false;
        var pulse = new AnimationClip(); pulse.frameRate = 30;
        pulse.SetCurve("", typeof(Image), "m_Color.a", new AnimationCurve(new Keyframe(0, .08f), new Keyframe(.8f, .6f), new Keyframe(1.6f, .08f)));
        settings = AnimationUtility.GetAnimationClipSettings(pulse); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(pulse, settings);
        AssetDatabase.CreateAsset(pulse, "Assets/07.Animations/LoginFeel/StatusPulse.anim");
        var pc = AnimatorController.CreateAnimatorControllerAtPath("Assets/07.Animations/LoginFeel/StatusPulse.controller"); pc.AddMotion(pulse); led.AddComponent<Animator>().runtimeAnimatorController = pc;
        var audio = group.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.volume = .18f; audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/06.Sounds/TerminalClick.wav");
        foreach (var b in canvas.GetComponentsInChildren<Button>(true)) UnityEventTools.AddPersistentListener(b.onClick, audio.Play);
        int[] xs = { -110, 3, 115 }; int[] ys = { -111, -177, -242, -306 };
        for (int row = 0; row < 4; row++) for (int col = 0; col < 3; col++)
        {
            var key = new GameObject("KeyFeedback_" + row + "_" + col, typeof(RectTransform), typeof(Image), typeof(Button));
            var kr = key.GetComponent<RectTransform>(); kr.SetParent(rt, false); kr.anchoredPosition = new Vector2(xs[col], ys[row]); kr.sizeDelta = new Vector2(96, 52);
            var im = key.GetComponent<Image>(); var b = key.GetComponent<Button>(); b.targetGraphic = im;
            var colors = b.colors; colors.normalColor = new Color(1, 1, 1, 0); colors.highlightedColor = new Color(.7f, .9f, .6f, .22f); colors.selectedColor = new Color(1, 1, 1, 0); colors.pressedColor = new Color(0, 0, 0, .6f); colors.fadeDuration = .05f; b.colors = colors;
            UnityEventTools.AddPersistentListener(b.onClick, audio.Play);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        System.IO.File.WriteAllText("Library/LoginFeel.done", "Idle motion, status pulse, keypad feedback and click audio saved.");
    }
}
