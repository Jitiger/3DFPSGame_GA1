// 플레이어 캐릭터를 발레리노 + 요술봉으로 바꾸는 에디터 도구
// 메뉴: Tools > Ballerino > Swap Player To Ballerino  (되돌리기: Revert Player To Soldier)
// - 기존 Soldier 모델은 지우지 않고 꺼두기만 한다 (언제든 되돌릴 수 있게).
// - 애니메이터 컨트롤러(PlayerAC)와 애니메이션 이벤트(PlayerBombAnimationEvent)는 그대로 옮긴다.
// - 총구 위치(MuzzlePosition)와 발사 시작점(FirePoint)은 요술봉 끝으로 옮긴다.
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BallerinoSwap
{
    private const string ModelPath = "Assets/Ballerino/Ballerino.fbx";
    private const string PaintedMeshPath = "Assets/Ballerino/Ballerino_Painted.asset";
    private const string MaterialPath = "Assets/Ballerino/Ballerino_Toon.mat";
    private const string WandPath = "Assets/Ballerino/Wand.prefab";
    private const string ModelName = "Ballerino";

    [MenuItem("Tools/Ballerino/Swap Player To Ballerino")]
    public static void Swap()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[BallerinoSwap] 씬에서 Player 를 찾지 못했습니다.");
            return;
        }

        if (player.transform.Find(ModelName) != null)
        {
            Debug.LogWarning("[BallerinoSwap] 이미 발레리노로 바뀌어 있습니다.");
            return;
        }

        // 1. 지금 켜져 있는 기존 모델(애니메이터가 있는 자식) 찾기
        Animator oldAnimator = player.GetComponentsInChildren<Animator>(false).FirstOrDefault(a => a.gameObject != player);
        if (oldAnimator == null)
        {
            Debug.LogError("[BallerinoSwap] Player 아래에서 애니메이터가 있는 모델을 찾지 못했습니다.");
            return;
        }

        Transform oldModel = oldAnimator.transform;
        Undo.RegisterFullObjectHierarchyUndo(player, "Swap Player To Ballerino");

        // 2. 발레리노 모델 생성 (기존 모델과 같은 자리)
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, player.scene);
        Undo.RegisterCreatedObjectUndo(model, "Swap Player To Ballerino");
        model.name = ModelName;
        model.transform.SetParent(oldModel.parent, false);
        model.transform.localPosition = oldModel.localPosition;
        model.transform.localRotation = oldModel.localRotation;

        // 키를 기존 모델과 맞춤
        float oldHeight = Height(oldModel.gameObject);
        float newHeight = Height(model);
        if (oldHeight > 0.01f && newHeight > 0.01f)
            model.transform.localScale = Vector3.one * (oldHeight / newHeight);

        // 3. 애니메이터: 기존 컨트롤러와 설정을 그대로
        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = oldAnimator.runtimeAnimatorController;
        animator.applyRootMotion = oldAnimator.applyRootMotion;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (animator.avatar == null)
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();

        // 애니메이션 이벤트를 받는 컴포넌트도 옮김 (폭탄 던지기)
        foreach (MonoBehaviour behaviour in oldModel.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null) continue;
            if (model.GetComponent(behaviour.GetType()) == null)
                model.AddComponent(behaviour.GetType());
        }

        // 4. 색칠한 메쉬 + 툰 머티리얼
        SkinnedMeshRenderer renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
        Mesh painted = AssetDatabase.LoadAssetAtPath<Mesh>(PaintedMeshPath);
        if (painted == null && renderer != null) painted = Paint(renderer, animator);   // 처음 한 번 부위별 색을 칠한 메쉬를 만든다
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (renderer != null)
        {
            if (painted != null) renderer.sharedMesh = painted;
            if (material != null) renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMesh.subMeshCount)).ToArray();
            renderer.updateWhenOffscreen = true;
        }

        // 5. 오른손에 요술봉
        Transform wandTip = AttachWand(model, animator);

        // 6. 총구 위치/발사 시작점을 요술봉 끝으로 (기존 모델을 끄기 전에 옮겨야 함)
        Transform muzzle = FindDeep(player.transform, "MuzzlePosition");
        if (muzzle != null && wandTip != null)
        {
            Undo.SetTransformParent(muzzle, wandTip, "Move Muzzle To Wand");
            muzzle.localPosition = Vector3.zero;
            muzzle.localRotation = Quaternion.identity;
        }

        // 7. 기존 모델은 꺼두기 (Player 아래의 다른 Soldier 모델도 함께)
        foreach (Animator other in player.GetComponentsInChildren<Animator>(true))
        {
            if (other == animator || other.gameObject == player) continue;
            Undo.RecordObject(other.gameObject, "Hide Old Model");
            other.gameObject.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = model;
        Debug.Log($"[BallerinoSwap] 완료: 키 {oldHeight:F2} -> {newHeight * model.transform.localScale.x:F2}, 요술봉 장착, 총구 이동 = {(muzzle != null)}");
    }

    [MenuItem("Tools/Ballerino/Revert Player To Soldier")]
    public static void Revert()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;
        Transform model = player.transform.Find(ModelName);
        if (model == null) return;

        Undo.RegisterFullObjectHierarchyUndo(player, "Revert Player To Soldier");

        // 총구 위치를 다시 기존 총의 자리로 (꺼둔 모델 중 Gun/Muzzle 뼈를 찾아서)
        Transform muzzle = FindDeep(model, "MuzzlePosition");
        Transform oldModel = player.GetComponentsInChildren<Animator>(true).Where(a => a.transform != model && a.gameObject != player).Select(a => a.transform).FirstOrDefault();
        if (muzzle != null && oldModel != null)
        {
            oldModel.gameObject.SetActive(true);
            Transform gunBone = FindDeep(oldModel, "Muzzle") ?? FindDeep(oldModel, "Gun") ?? oldModel;
            Undo.SetTransformParent(muzzle, gunBone, "Restore Muzzle");
            muzzle.localPosition = Vector3.zero;
        }
        else if (oldModel != null)
        {
            oldModel.gameObject.SetActive(true);
        }

        Undo.DestroyObjectImmediate(model.gameObject);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("[BallerinoSwap] Soldier 모델로 되돌렸습니다. 총구 위치는 확인해 주세요.");
    }

    // 요술봉을 오른손에 쥐여주고 끝(WandTip)을 돌려준다.
    private static Transform AttachWand(GameObject model, Animator animator)
    {
        GameObject wandAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WandPath);
        if (wandAsset == null || !animator.isHuman) return null;
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Transform finger = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
        if (hand == null) return null;

        GameObject wand = (GameObject)PrefabUtility.InstantiatePrefab(wandAsset, model.scene);
        Undo.RegisterCreatedObjectUndo(wand, "Attach Wand");
        wand.name = "Magic Wand";
        Transform tip = wand.transform.Find("WandTip");
        Vector3 tipLocal = tip != null ? tip.localPosition : Vector3.up;

        // 끝이 캐릭터 정면, 장식 면이 위를 보도록 (기본 자세 기준)
        Vector3 forward = model.transform.forward;
        wand.transform.rotation = Quaternion.LookRotation(Vector3.up, forward) * Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward, tipLocal.normalized));
        wand.transform.position = finger != null ? Vector3.Lerp(hand.position, finger.position, 0.6f) : hand.position;
        wand.transform.localScale = Vector3.one * model.transform.localScale.x;
        wand.transform.SetParent(hand, true);
        return tip;
    }

    // 부위별 단색을 버텍스 컬러로 칠한 메쉬 사본을 만든다 (발레리노 원본 FBX 좌표 기준 규칙)
    private static Mesh Paint(SkinnedMeshRenderer smr, Animator animator)
    {
        Mesh src = smr.sharedMesh;
        Mesh mesh = Object.Instantiate(src);
        mesh.name = "Ballerino_Painted";
        Vector3[] vs = src.vertices;
        Transform tr = smr.transform;
        Vector3 L(HumanBodyBones b) => tr.InverseTransformPoint(animator.GetBoneTransform(b).position);
        Vector3 up = (L(HumanBodyBones.Head) - L(HumanBodyBones.Hips)).normalized;
        Vector3 fwd = Vector3.ProjectOnPlane(L(HumanBodyBones.LeftToes) - L(HumanBodyBones.LeftFoot), up).normalized;
        Vector3 right = Vector3.Cross(up, fwd).normalized;
        float minU = vs.Min(v => Vector3.Dot(v, up)), maxU = vs.Max(v => Vector3.Dot(v, up));
        float hs = maxU - minU, unit = hs / 1.9019686f;
        Vector3 c = vs.Aggregate(Vector3.zero, (a, v) => a + v) / vs.Length;
        var cols = new Color[vs.Length];
        Color skin = new Color(0.93f, 0.74f, 0.62f), hair = new Color(0.08f, 0.07f, 0.09f), crown = new Color(0.85f, 0.85f, 0.88f),
              pink = new Color(0.86f, 0.24f, 0.47f), heartC = new Color(0.25f, 0.05f, 0.12f), sleeve = new Color(0.80f, 0.76f, 0.92f),
              tutu = new Color(0.78f, 0.74f, 0.86f), tights = new Color(0.62f, 0.62f, 0.66f), shoe = new Color(0.70f, 0.70f, 0.74f);
        const float ZMin = -0.951f, H = 1.9019686f;
        for (int i = 0; i < vs.Length; i++)
        {
            Vector3 d = vs[i] - c;
            float x = Vector3.Dot(d, right) / unit, y = -Vector3.Dot(d, fwd) / unit;
            float h = (Vector3.Dot(vs[i], up) - minU) / hs, z = ZMin + h * H, ax = Mathf.Abs(x);
            Color col = skin;
            if (h < 0.47f) col = tights;
            if (h < 0.035f) col = shoe;
            if (h >= 0.415f + 0.30f * ax && h < 0.50f && ax < 0.19f) col = pink;
            bool torso = h >= 0.47f && h < 0.80f && ax < 0.19f;
            if (torso) col = pink;
            float r = Mathf.Sqrt(x * x + (y - 0.01f) * (y - 0.01f));
            if (h >= 0.50f && h < 0.60f && r > 0.165f) col = tutu;
            float armd = Mathf.Sqrt(Mathf.Pow(z - (ZMin + 0.79f * H), 2) + Mathf.Pow(y - 0.023f, 2));
            if (ax >= 0.19f && h >= 0.72f && h < 0.86f) col = skin;
            if (ax >= 0.13f && ax < 0.30f && h >= 0.70f && h < 0.88f && armd > 0.06f) col = sleeve;
            bool head = h >= 0.86f;
            if (head) col = hair;
            if (head && y < -0.02f && ax < 0.075f && h < 0.965f) col = skin;
            if (h >= 0.835f && h < 0.86f && ax < 0.07f) col = skin;
            if (head && h > 0.975f && ax < 0.05f) col = crown;
            bool front = y < -0.04f;
            if (torso && front && (Heart(x, h, 0.065f, 0.715f, 0.042f, H) || Heart(x, h, -0.065f, 0.715f, 0.042f, H))) col = heartC;
            if (h >= 0.40f && h < 0.50f && front && Heart(x, h, 0f, 0.452f, 0.03f, H)) col = heartC;
            cols[i] = col;
        }

        mesh.colors = cols;
        AssetDatabase.DeleteAsset(PaintedMeshPath);
        AssetDatabase.CreateAsset(mesh, PaintedMeshPath);
        return mesh;
    }

    private static bool Heart(float px, float ph, float cx, float ch, float s, float height)
    {
        float hx = (px - cx) / s, hy = (ph - ch) * height / s;
        float a = hx * hx + hy * hy - 1f;
        return a * a * a - hx * hx * hy * hy * hy < 0f;
    }

    private static float Height(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return 0f;
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
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
