using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// Editor-only, fixed-view rendering evidence. It does not change saved gameplay state.
[InitializeOnLoad]
public static class OptimizationBenchmark
{
    private const string Key = "OptimizationBenchmark.Label";
    private static readonly List<float> FrameTimes = new List<float>();
    private static readonly List<int> Batches = new List<int>();
    private static readonly List<int> Triangles = new List<int>();
    private static readonly List<int> Passes = new List<int>();
    private static int _frames;
    private static bool _configured;
    private static string _label;
    private static string _directory;

    static OptimizationBenchmark()
    {
        EditorApplication.update += Update;
    }

    public static void Begin(string label, string directory)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
        Directory.CreateDirectory(directory);
        SessionState.SetString(Key, label);
        SessionState.SetString(Key + ".Directory", directory);
        ProfilerDriver.ClearAllFrames();
        ProfilerDriver.profileEditor = false;
        ProfilerDriver.deepProfiling = false;
        ProfilerDriver.enabled = true;
        var profilerType = typeof(Editor).Assembly.GetType("UnityEditor.ProfilerWindow");
        foreach (var profiler in Resources.FindObjectsOfTypeAll(profilerType).OfType<EditorWindow>()) profiler.Close();
        var gameType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var game = EditorWindow.GetWindow(gameType);
        game.maximized = true;
        gameType.GetProperty("showStats", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(game, true);
        game.Focus();
        EditorApplication.isPlaying = true;
    }

    private static void Update()
    {
        string label = SessionState.GetString(Key, "");
        if (string.IsNullOrEmpty(label) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (!_configured)
            {
                _label = label;
                _directory = SessionState.GetString(Key + ".Directory", "");
                var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                if (manager == null) return;
                manager.StopAllCoroutines();
                typeof(GameManager).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, GameState.Ready);
                var managerData = new SerializedObject(manager);
                var text = managerData.FindProperty("_stateText").objectReferenceValue as Component;
                if (text != null) text.gameObject.SetActive(false);
                var player = UnityEngine.Object.FindFirstObjectByType<Player>();
                player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Camera.main.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                _configured = true;
                _frames = 0;
                FrameTimes.Clear(); Batches.Clear(); Triangles.Clear(); Passes.Clear();
            }
            var gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            gameManager.StopAllCoroutines();
            typeof(GameManager).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gameManager, GameState.Ready);
            _frames++;
            if (_frames < 180) return;
            FrameTimes.Add(Time.unscaledDeltaTime * 1000f);
            Batches.Add(UnityStats.batches);
            Triangles.Add(UnityStats.triangles);
            Passes.Add(UnityStats.setPassCalls);
            if (FrameTimes.Count < 300) return;
            ProfilerDriver.enabled = false;
            FrameTimes.Clear();
            int lastFrame = ProfilerDriver.lastFrameIndex - 1;
            for (int frame = Math.Max(ProfilerDriver.firstFrameIndex, lastFrame - 299); frame <= lastFrame; frame++)
            {
                using (var data = ProfilerDriver.GetRawFrameDataView(frame, 0))
                {
                    if (data.valid) FrameTimes.Add(data.frameTimeMs);
                }
            }
            bool profileSaved = ProfilerDriver.SaveProfile(Path.Combine(_directory, _label + "-profiler.raw"));
            var result = new Result
            {
                label = _label,
                sampleCount = FrameTimes.Count,
                meanFrameMs = FrameTimes.Average(),
                medianFrameMs = FrameTimes.OrderBy(v => v).ElementAt(FrameTimes.Count / 2),
                p95FrameMs = FrameTimes.OrderBy(v => v).ElementAt((int)(FrameTimes.Count * 0.95f)),
                meanBatches = Batches.Average(),
                meanTriangles = Triangles.Average(),
                meanSetPassCalls = Passes.Average(),
                textureBytes = UnityStats.usedTextureMemorySize,
                screen = UnityStats.screenRes,
                profilerSaved = profileSaved,
                unityVersion = Application.unityVersion,
                condition = "Play mode; fixed camera yaw 90; Ready state (AI/input held); 180 warm-up frames; 300 samples; Editor measurement, not a build benchmark."
            };
            File.WriteAllText(Path.Combine(_directory, _label + "-metrics.json"), JsonUtility.ToJson(result, true));
            ScreenCapture.CaptureScreenshot(Path.Combine(_directory, _label + "-game.png"));
            CaptureWindow(EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")), Path.Combine(_directory, _label + "-stats.png"));
            SessionState.SetString(Key, "");
            _configured = false;
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
        catch (Exception exception)
        {
            SessionState.SetString(Key, "");
            _configured = false;
            ProfilerDriver.enabled = false;
            Debug.LogException(exception);
            EditorApplication.isPlaying = false;
        }
    }

    public static void CaptureWindow(EditorWindow window, string path)
    {
        var rect = window.position;
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);
        var colors = InternalEditorUtility.ReadScreenPixel(rect.position, width, height);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.SetPixels(colors);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    [Serializable]
    private sealed class Result
    {
        public string label;
        public int sampleCount;
        public float meanFrameMs;
        public float medianFrameMs;
        public float p95FrameMs;
        public double meanBatches;
        public double meanTriangles;
        public double meanSetPassCalls;
        public long textureBytes;
        public string screen;
        public bool profilerSaved;
        public string unityVersion;
        public string condition;
    }
}
