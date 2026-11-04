using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 강한육군체 TTF로 TMP용 SDF 폰트 에셋을 만든다.
// TMP는 TTF를 바로 못 쓰고 SDF 에셋이 있어야 하는데, 이건 에디터에서만 만들 수 있어서 메뉴로 빼뒀다.
// 한 번 실행해서 .asset이 생기면 다시 쓸 일은 없다.
public static class ArmyFontAssetBuilder
{
    private const int SamplingPointSize = 90;
    private const int AtlasPadding = 9;
    private const int AtlasSize = 1024;

    private static readonly string[] SourcePaths =
    {
        "Assets/08.Fonts/강한육군 Bold.ttf",
        "Assets/08.Fonts/강한육군 Medium.ttf",
    };

    [MenuItem("Tools/강한육군체 SDF 폰트 만들기")]
    public static void Build()
    {
        foreach (string sourcePath in SourcePaths)
        {
            Create(sourcePath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // 에셋이 아직 없을 때만 한 번 만든다. 이미 있으면 아무것도 하지 않는다.
    [InitializeOnLoadMethod]
    private static void BuildIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            foreach (string sourcePath in SourcePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetPathOf(sourcePath)) != null) continue;

                Create(sourcePath);
            }

            AssetDatabase.SaveAssets();
        };
    }

    private static string TargetPathOf(string sourcePath)
    {
        return $"{Path.GetDirectoryName(sourcePath)}/{Path.GetFileNameWithoutExtension(sourcePath)} SDF.asset";
    }

    private static void Create(string sourcePath)
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (font == null)
        {
            Debug.LogError($"폰트 파일을 찾지 못했습니다: {sourcePath}");
            return;
        }

        string targetPath = TargetPathOf(sourcePath);

        // 글리프를 미리 다 구워두면 한글 때문에 아틀라스가 터진다.
        // Dynamic으로 두면 화면에 나온 글자만 그때그때 아틀라스에 채워 넣는다.
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            font,
            SamplingPointSize,
            AtlasPadding,
            GlyphRenderMode.SDFAA,
            AtlasSize,
            AtlasSize,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            Debug.LogError($"폰트 에셋을 만들지 못했습니다: {sourcePath}");
            return;
        }

        fontAsset.name = Path.GetFileNameWithoutExtension(targetPath);

        AssetDatabase.DeleteAsset(targetPath);
        AssetDatabase.CreateAsset(fontAsset, targetPath);

        // 아틀라스 텍스처와 머티리얼은 폰트 에셋 안에 같이 넣어둔다.
        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
        {
            fontAsset.atlasTextures[0].name = $"{fontAsset.name} Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = $"{fontAsset.name} Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        Debug.Log($"폰트 에셋을 만들었습니다: {targetPath}");
    }
}
