// 플레이어 캐릭터를 Origami Star Guardian(Meshy 리깅본) + Crystalblade 로 바꾸는 에디터 도구
// 메뉴: Tools/Star Guardian/Swap Player To Star Guardian   (되돌리기: Revert Player To Soldier)
// - 기존 Soldier 모델은 지우지 않고 꺼두기만 한다.
// - 애니메이터 컨트롤러(PlayerAC)와 애니메이션 이벤트 컴포넌트는 그대로 옮긴다.
// - 총구 위치(MuzzlePosition)는 칼끝으로 옮긴다.
// - 프로젝트 폴더에 StarGuardian_autorun.txt 가 있으면 컴파일 직후 한 번 자동 실행하고 그 파일을 지운다.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StarGuardianSwap
{
    private const string Folder = "Assets/StarGuardian";
    private const string ModelPath = Folder + "/StarGuardian.fbx";
    private const string BaseMapPath = Folder + "/StarGuardian_BaseColor.png";
    private const string NormalMapPath = Folder + "/StarGuardian_Normal.png";
    private const string MetallicMapPath = Folder + "/StarGuardian_Metallic.png";
    private const string MaterialPath = Folder + "/StarGuardian_Lit.mat";

    private const string SwordFolder = "Assets/Meshy_AI_Crystalblade_20261029_texture_fbx/Meshy_AI_Crystalblade_20261029_texture_fbx";
    private const string SwordModelPath = SwordFolder + "/Meshy_AI_Crystalblade_20261029_texture.fbx";
    private const string SwordBasePath = SwordFolder + "/Meshy_AI_Crystalblade_20261029_texture.png";
    private const string SwordNormalPath = SwordFolder + "/Meshy_AI_Crystalblade_20261029_texture_normal.png";
    private const string SwordMetallicPath = SwordFolder + "/Meshy_AI_Crystalblade_20261029_texture_metallic.png";
    private const string SwordMaterialPath = Folder + "/Crystalblade_Lit.mat";
    private const string SwordPrefabPath = Folder + "/Crystalblade.prefab";

    private const string ScenePath = "Assets/01.Scenes/GameScenes.unity";
    private const string ModelName = "StarGuardian";
    private const string AutoRunFile = "StarGuardian_autorun.txt";

    // 칼 길이 = 캐릭터 키 × 이 값
    private const float SwordLengthRatio = 0.55f;

    private const string CaptureFlag = "StarGuardian_capture.txt";
    private const string CaptureOut = "StarGuardian_capture.png";
    private static double captureAt = -1;

    static StarGuardianSwap()
    {
        EditorApplication.update += PollCapture;
        if (!File.Exists(AutoRunFile)) return;
        EditorApplication.delayCall += AutoRun;
    }

    // 확인용: StarGuardian_capture.txt 가 있으면 플레이 모드에서 캐릭터를 앞/옆에서 찍어 PNG 로 저장
    private static void PollCapture()
    {
        if (!File.Exists(CaptureFlag) || File.Exists(AutoRunFile)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = true;
            captureAt = -1;
            return;
        }

        if (captureAt < 0) captureAt = EditorApplication.timeSinceStartup + 2.0;
        if (EditorApplication.timeSinceStartup < captureAt) return;
        File.Delete(CaptureFlag);
        captureAt = -1;
        Capture();
        EditorApplication.isPlaying = false;
    }

    [MenuItem("Tools/Star Guardian/Capture Preview (Play Mode)")]
    public static void Capture()
    {
        GameObject model = GameObject.Find(ModelName);
        if (model == null) { Debug.LogError("[StarGuardianSwap] 캡처: 모델 없음"); return; }
        Renderer[] rs = model.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        Transform t = model.transform;
        float h = Mathf.Max(b.size.y, 1f);
        Vector3 c = b.center;
        Vector3[] dirs = { t.forward * 1.0f - t.right * 0.5f, t.right * 1.0f + t.forward * 0.15f, -t.forward * 0.8f + t.right * 0.7f };
        const int W = 640, H = 720;
        Texture2D sheet = new Texture2D(W * dirs.Length, H, TextureFormat.RGB24, false);
        RenderTexture rt = new RenderTexture(W, H, 24);
        GameObject go = new GameObject("StarGuardianCaptureCam");
        Camera cam = go.AddComponent<Camera>();
        cam.fieldOfView = 35f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.25f, 0.27f, 0.3f);
        cam.targetTexture = rt;
        cam.nearClipPlane = 0.02f;
        for (int i = 0; i < dirs.Length; i++)
        {
            go.transform.position = c + dirs[i].normalized * h * 1.9f + Vector3.up * h * 0.1f;
            go.transform.LookAt(c);
            cam.Render();
            RenderTexture.active = rt;
            sheet.ReadPixels(new Rect(0, 0, W, H), W * i, 0);
        }

        sheet.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(CaptureOut, sheet.EncodeToPNG());
        Object.DestroyImmediate(go);
        rt.Release();
        Debug.Log("[StarGuardianSwap] 캡처 저장: " + Path.GetFullPath(CaptureOut));

        Transform sw = FindDeep(model.transform, "Crystalblade");
        if (sw != null)
        {
            string info = $"칼 lossyScale={sw.lossyScale} pos={sw.position} 몸중심={c} 키={h:F2}";
            foreach (Renderer r in sw.GetComponentsInChildren<Renderer>(true))
                info += $"\n  {r.name} {r.GetType().Name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} bounds={r.bounds.size} mat={(r.sharedMaterial != null ? r.sharedMaterial.shader.name : "null")}";
            foreach (MeshFilter mf in sw.GetComponentsInChildren<MeshFilter>(true))
                info += $"\n  mesh {mf.name} = {(mf.sharedMesh != null ? mf.sharedMesh.vertexCount.ToString() : "NULL")}";
            Transform tipT = FindDeep(sw, "SwordTip");
            if (tipT != null) info += $"\n  tip={tipT.position}";
            File.WriteAllText("StarGuardian_debug.txt", info);
        }
    }

    private static void AutoRun()
    {
        if (!File.Exists(AutoRunFile)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoRun;
            return;
        }

        string mode = File.ReadAllText(AutoRunFile).Trim();
        File.Delete(AutoRunFile);

        // 지금 열린 씬에 Player 가 없고 저장 안 된 변경도 없으면 게임 씬을 연다
        bool wasDirty = EditorSceneManager.GetActiveScene().isDirty;
        if (GameObject.Find("Player") == null && !wasDirty)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        if (mode == "stab")
        {
            UseSwordSlash();
            return;
        }

        if (mode == "reswap") Revert();
        bool ok = Swap();
        if (ok && !wasDirty)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[StarGuardianSwap] 씬 저장 완료");
        }
    }

    // 현재 수업의 Melee Layer와 Slash 트리거를 사용한다.
    [MenuItem("Tools/Star Guardian/Use Sword Slash")]
    public static void UseSwordSlash()
    {
        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/07.Animations/Player/PlayerAC.controller");
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/EEJANAI_Team/FreeSwordAnimations/Animations/slash8.anim");
        var layer = ac != null ? ac.layers.FirstOrDefault(l => l.name == "Melee Layer") : null;
        if (clip == null || layer == null)
        {
            Debug.LogError("[StarGuardianSwap] Melee Layer 또는 slash8 클립을 찾지 못했습니다.");
            return;
        }

        if (!ac.parameters.Any(p => p.name == "Slash"))
            ac.AddParameter("Slash", AnimatorControllerParameterType.Trigger);

        var sm = layer.stateMachine;
        var empty = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Empty") ?? sm.AddState("Empty");
        var slash = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Slash") ?? sm.AddState("Slash");
        slash.motion = clip;
        slash.speed = 1f;
        sm.defaultState = empty;
        foreach (var t in empty.transitions.ToArray()) empty.RemoveTransition(t);
        foreach (var t in slash.transitions.ToArray()) slash.RemoveTransition(t);
        var enter = empty.AddTransition(slash);
        enter.hasExitTime = false;
        enter.duration = 0.1f;
        enter.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Slash");
        var exit = slash.AddTransition(empty);
        exit.hasExitTime = true;
        exit.exitTime = 0.9f;
        exit.duration = 0.1f;
        EditorUtility.SetDirty(ac);
        EditorUtility.SetDirty(sm);
        EditorUtility.SetDirty(slash);
        AssetDatabase.SaveAssets();

        GameObject player = GameObject.Find("Player");
        if (player != null && player.GetComponent<PlayerMeleeAttack>() == null)
        {
            Undo.AddComponent<PlayerMeleeAttack>(player);
            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveScene(player.scene);
        }
    }
    [MenuItem("Tools/Star Guardian/Swap Player To Star Guardian")]
    public static void SwapMenu() => Swap();

    public static bool Swap()
    {
        SetupAssets();

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[StarGuardianSwap] 씬에서 Player 를 찾지 못했습니다.");
            return false;
        }

        Transform existing = player.transform.Find(ModelName) ?? FindDeep(player.transform, ModelName);
        if (existing != null)
        {
            Debug.LogWarning("[StarGuardianSwap] 이미 Star Guardian 으로 바뀌어 있습니다. 다시 하려면 먼저 Revert 하세요.");
            return false;
        }

        // 1. 지금 켜져 있는 기존 모델(애니메이터가 있는 자식)
        Animator oldAnimator = player.GetComponentsInChildren<Animator>(false).FirstOrDefault(a => a.gameObject != player);
        if (oldAnimator == null)
        {
            Debug.LogError("[StarGuardianSwap] Player 아래에서 애니메이터가 있는 모델을 찾지 못했습니다.");
            return false;
        }

        Transform oldModel = oldAnimator.transform;
        Undo.RegisterFullObjectHierarchyUndo(player, "Swap Player To Star Guardian");

        // 2. 새 모델 생성 (기존 모델과 같은 자리)
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelAsset == null)
        {
            Debug.LogError("[StarGuardianSwap] 모델을 찾지 못했습니다: " + ModelPath);
            return false;
        }

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, player.scene);
        Undo.RegisterCreatedObjectUndo(model, "Swap Player To Star Guardian");
        model.name = ModelName;
        model.transform.SetParent(oldModel.parent, false);
        model.transform.localPosition = oldModel.localPosition;
        model.transform.localRotation = oldModel.localRotation;
        model.transform.localScale = Vector3.one;

        // 키를 기존 모델과 맞추고 발이 같은 높이에 오게
        float oldHeight = Height(oldModel.gameObject, out float oldBottom);
        float newHeight = Height(model, out _);
        if (oldHeight > 0.01f && newHeight > 0.01f)
            model.transform.localScale = Vector3.one * (oldHeight / newHeight);
        Height(model, out float newBottom);
        model.transform.position += Vector3.up * (oldBottom - newBottom);

        // 3. 애니메이터: 기존 컨트롤러와 설정을 그대로
        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = oldAnimator.runtimeAnimatorController;
        animator.applyRootMotion = oldAnimator.applyRootMotion;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar != null) animator.avatar = avatar;

        // 애니메이션 이벤트를 받는 컴포넌트 옮기기 (폭탄 던지기 등)
        foreach (MonoBehaviour behaviour in oldModel.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null) continue;
            if (model.GetComponent(behaviour.GetType()) == null)
                model.AddComponent(behaviour.GetType());
        }

        // 4. 머티리얼
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
        }

        // 5. 오른손에 칼
        Transform tip = AttachSword(model, animator, oldHeight > 0.01f ? oldHeight : newHeight);

        // 6. 총구 위치를 칼끝으로 (기존 모델을 끄기 전에)
        Transform muzzle = FindDeep(player.transform, "MuzzlePosition");
        if (muzzle != null && tip != null)
        {
            Undo.SetTransformParent(muzzle, tip, "Move Muzzle To Sword Tip");
            muzzle.localPosition = Vector3.zero;
            muzzle.localRotation = Quaternion.identity;
        }

        // 7. 기존 모델들은 꺼두기
        foreach (Animator other in player.GetComponentsInChildren<Animator>(true))
        {
            if (other == animator || other.gameObject == player) continue;
            if (other.transform.IsChildOf(model.transform)) continue;
            Undo.RecordObject(other.gameObject, "Hide Old Model");
            other.gameObject.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = model;
        Debug.Log($"[StarGuardianSwap] 완료: 키 {oldHeight:F2}, 아바타 휴머노이드 = {animator.isHuman}, 칼 장착 = {tip != null}, 총구 이동 = {muzzle != null && tip != null}");
        return true;
    }

    [MenuItem("Tools/Star Guardian/Revert Player To Soldier")]
    public static void Revert()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;
        Transform model = FindDeep(player.transform, ModelName);
        if (model == null) return;

        Undo.RegisterFullObjectHierarchyUndo(player, "Revert Player To Soldier");

        Transform muzzle = FindDeep(model, "MuzzlePosition");
        List<Transform> oldModels = player.GetComponentsInChildren<Animator>(true)
            .Where(a => a.transform != model && a.gameObject != player && !a.transform.IsChildOf(model))
            .Select(a => a.transform).ToList();
        foreach (Transform old in oldModels) old.gameObject.SetActive(true);

        if (muzzle != null && oldModels.Count > 0)
        {
            Transform gunBone = oldModels.Select(o => FindDeep(o, "Muzzle") ?? FindDeep(o, "Gun")).FirstOrDefault(t => t != null) ?? oldModels[0];
            Undo.SetTransformParent(muzzle, gunBone, "Restore Muzzle");
            muzzle.localPosition = Vector3.zero;
        }

        Undo.DestroyObjectImmediate(model.gameObject);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("[StarGuardianSwap] Soldier 모델로 되돌렸습니다. 총구 위치는 한 번 확인해 주세요.");
    }

    // ───────────────────────── 에셋 준비 ─────────────────────────

    [MenuItem("Tools/Star Guardian/Setup Assets Only")]
    public static void SetupAssets()
    {
        // 캐릭터 FBX: 휴머노이드
        if (AssetImporter.GetAtPath(ModelPath) is ModelImporter mi)
        {
            bool changed = false;
            if (mi.animationType != ModelImporterAnimationType.Human) { mi.animationType = ModelImporterAnimationType.Human; changed = true; }
            if (mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; changed = true; }
            if (changed) mi.SaveAndReimport();
        }

        MakeNormalMap(NormalMapPath);
        MakeNormalMap(SwordNormalPath);

        CreateLit(MaterialPath, BaseMapPath, NormalMapPath, MetallicMapPath, 0.35f);
        CreateLit(SwordMaterialPath, SwordBasePath, SwordNormalPath, SwordMetallicPath, 0.75f);
        AssetDatabase.SaveAssets();
    }

    private static void MakeNormalMap(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
        {
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
    }

    private static Material CreateLit(string path, string basePath, string normalPath, string metallicPath, float smoothness)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (mat == null)
        {
            mat = new Material(lit);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.shader = lit;
        Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
        mat.SetColor("_BaseColor", Color.white);
        if (baseMap != null) { mat.SetTexture("_BaseMap", baseMap); mat.SetTexture("_MainTex", baseMap); }
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
        if (metallic != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ───────────────────────── 칼 ─────────────────────────

    // 칼 프리팹: 루트 = 손잡이 가운데, +Y = 칼날 방향, +X = 칼날 넓은 면, 자식 SwordTip = 칼끝
    private static GameObject BuildSwordPrefab()
    {
        GameObject swordAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SwordModelPath);
        if (swordAsset == null) return null;

        GameObject root = new GameObject("Crystalblade");
        GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(swordAsset);
        mesh.name = "Mesh";
        mesh.transform.SetParent(root.transform, false);

        // 모든 정점(루트 기준 좌표) 모으기
        List<Vector3> pts = new List<Vector3>();
        foreach (MeshFilter mf in mesh.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Matrix4x4 m = root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            foreach (Vector3 v in mf.sharedMesh.vertices) pts.Add(m.MultiplyPoint3x4(v));
        }

        if (pts.Count == 0)
        {
            Object.DestroyImmediate(root);
            return null;
        }

        // 주성분: 첫 번째 = 칼 길이 방향, 두 번째 = 칼날 넓은 면
        Vector3 c = Vector3.zero;
        foreach (Vector3 p in pts) c += p;
        c /= pts.Count;
        double[,] cov = new double[3, 3];
        foreach (Vector3 p in pts)
        {
            Vector3 d = p - c;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    cov[i, j] += d[i] * d[j];
        }

        Vector3 axis = PowerIteration(cov, new Vector3(0.3f, 0.9f, 0.2f));
        double lambda = Rayleigh(cov, axis);
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                cov[i, j] -= lambda * axis[i] * axis[j];
        Vector3 wide = PowerIteration(cov, Vector3.Cross(axis, Vector3.up).sqrMagnitude > 0.01f ? Vector3.Cross(axis, Vector3.up) : Vector3.right);
        wide = Vector3.ProjectOnPlane(wide, axis).normalized;

        // 길이 방향으로 잘라서 가장 넓은 단면 = 가드. 가드에서 짧은 쪽이 손잡이.
        float tMin = float.MaxValue, tMax = float.MinValue;
        foreach (Vector3 p in pts)
        {
            float t = Vector3.Dot(p - c, axis);
            tMin = Mathf.Min(tMin, t);
            tMax = Mathf.Max(tMax, t);
        }

        const int Bins = 24;
        float[] width = new float[Bins];
        foreach (Vector3 p in pts)
        {
            Vector3 d = p - c;
            float t = Vector3.Dot(d, axis);
            int b = Mathf.Clamp((int)((t - tMin) / (tMax - tMin) * Bins), 0, Bins - 1);
            width[b] = Mathf.Max(width[b], Vector3.ProjectOnPlane(d, axis).magnitude);
        }

        int guard = 0;
        for (int b = 1; b < Bins; b++) if (width[b] > width[guard]) guard = b;
        float tGuard = tMin + (guard + 0.5f) * (tMax - tMin) / Bins;
        bool handleAtMin = (tGuard - tMin) < (tMax - tGuard);
        if (!handleAtMin) axis = -axis; // 축이 항상 손잡이 → 칼끝을 향하도록
        float tg = handleAtMin ? tGuard - tMin : tMax - tGuard;       // 손잡이 끝 ~ 가드 길이
        float total = tMax - tMin;
        Vector3 handleEnd = c + axis * (handleAtMin ? tMin : -tMax);
        Vector3 grip = handleEnd + axis * (tg * 0.5f);
        Vector3 tipPoint = handleEnd + axis * total;

        // 메쉬를 옮겨서 손잡이 가운데가 원점, 칼날이 +Y, 넓은 면이 +X 가 되게
        // (FBX 자체의 회전/스케일은 그대로 두고 중간 Pivot 으로 감싼다)
        Quaternion toLocal = Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(wide, axis), axis));
        GameObject pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        mesh.transform.SetParent(pivot.transform, false); // 루트 = 단위 변환이라 좌표 그대로 유지
        // 길이 1 로 정규화 (장착할 때 캐릭터 키에 맞춰 스케일)
        pivot.transform.localRotation = toLocal;
        pivot.transform.localScale = Vector3.one / total;
        pivot.transform.localPosition = -(toLocal * grip) / total;

        GameObject tip = new GameObject("SwordTip");
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = toLocal * (tipPoint - grip) / total;

        // 머티리얼
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(SwordMaterialPath);
        foreach (Renderer r in mesh.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SwordPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Transform AttachSword(GameObject model, Animator animator, float characterHeight)
    {
        GameObject swordPrefab = BuildSwordPrefab();
        if (swordPrefab == null || !animator.isHuman)
        {
            Debug.LogWarning("[StarGuardianSwap] 칼을 붙이지 못했습니다 (칼 모델 없음 또는 아바타가 휴머노이드가 아님).");
            return null;
        }

        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Transform middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) ?? animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        if (hand == null) return null;

        GameObject sword = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab, model.scene);
        Undo.RegisterCreatedObjectUndo(sword, "Attach Sword");
        sword.name = "Crystalblade";

        // 실제 게임에서 쓰는 기본 애니메이션(PlayerAC 기본 상태) 자세로 잠깐 바꿔놓고 맞춘다.
        // T 자세 기준으로 맞추면 총 쥐는 애니메이션에서 칼이 옆을 보게 된다.
        AnimationClip pose = DefaultClip(animator.runtimeAnimatorController);
        bool sampled = false;
        if (pose != null)
        {
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(model, pose, 0f);
            AnimationMode.EndSampling();
            sampled = true;
        }

        // 칼날은 캐릭터 정면(살짝 위), 넓은 면은 위아래
        Vector3 up = model.transform.up;
        Vector3 forward = (model.transform.forward + up * 0.25f).normalized;
        Vector3 side = Vector3.Cross(up, forward).normalized;
        Vector3 flat = Vector3.Cross(forward, side).normalized;
        sword.transform.rotation = Quaternion.LookRotation(Vector3.Cross(flat, forward), forward);
        Vector3 palm = middle != null ? Vector3.Lerp(hand.position, middle.position, 0.55f) : hand.position;
        sword.transform.position = palm;
        sword.transform.localScale = Vector3.one * (characterHeight * SwordLengthRatio);
        sword.transform.SetParent(hand, true);

        if (sampled) AnimationMode.StopAnimationMode();
        Debug.Log($"[StarGuardianSwap] 칼 정렬 기준 자세: {(pose != null ? pose.name : "T 자세")}");
        return sword.transform.Find("SwordTip");
    }

    // 애니메이터 컨트롤러 기본 상태의 클립 (블렌드 트리면 첫 클립)
    private static AnimationClip DefaultClip(RuntimeAnimatorController rac)
    {
        if (rac is UnityEditor.Animations.AnimatorController ac && ac.layers.Length > 0)
        {
            Motion m = ac.layers[0].stateMachine.defaultState != null ? ac.layers[0].stateMachine.defaultState.motion : null;
            while (m is UnityEditor.Animations.BlendTree bt && bt.children.Length > 0) m = bt.children[0].motion;
            if (m is AnimationClip clip) return clip;
        }

        return rac != null ? rac.animationClips.FirstOrDefault() : null;
    }

    private static Vector3 PowerIteration(double[,] m, Vector3 start)
    {
        Vector3 v = start.normalized;
        for (int k = 0; k < 64; k++)
        {
            Vector3 n = new Vector3(
                (float)(m[0, 0] * v.x + m[0, 1] * v.y + m[0, 2] * v.z),
                (float)(m[1, 0] * v.x + m[1, 1] * v.y + m[1, 2] * v.z),
                (float)(m[2, 0] * v.x + m[2, 1] * v.y + m[2, 2] * v.z));
            if (n.sqrMagnitude < 1e-20f) break;
            v = n.normalized;
        }

        return v;
    }

    private static double Rayleigh(double[,] m, Vector3 v)
    {
        double s = 0;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                s += v[i] * m[i, j] * v[j];
        return s;
    }

    // ───────────────────────── 유틸 ─────────────────────────

    private static float Height(GameObject go, out float bottom)
    {
        bottom = go.transform.position.y;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return 0f;
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        bottom = b.min.y;
        return b.size.y;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }

        return null;
    }
}
