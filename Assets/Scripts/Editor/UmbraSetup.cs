// Umbra — авто-сборка сцен. Меню: "Umbra > Setup Everything".
// Требуемые пакеты: Cinemachine, AI Navigation, TextMeshPro, URP.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UmbraSetup
{
    private const string ScenesPath = "Assets/Scenes";

    [MenuItem("Umbra/Setup Everything (Run This First)")]
    public static void SetupEverything()
    {
        bool ok = EditorUtility.DisplayDialog("Umbra — Full Setup",
            "Will run, in order:\n" +
            "  1. Fix URP to 3D Renderer\n" +
            "  2. Configure imported FBXes as Humanoid\n" +
            "  3. Generate Animator Controllers\n" +
            "  4. Build all 5 scenes\n" +
            "  5. Configure Build Settings\n\n" +
            "Packages required BEFORE running:\n" +
            "  • Cinemachine\n  • AI Navigation\n  • TextMeshPro\n  • URP\n\n" +
            "Continue?", "Yes, do everything", "Cancel");
        if (!ok) return;

        FixUrpRenderer.Fix();
        AssetImportSetup.ConfigureAll();
        AnimatorControllerSetup.GenerateAll();

        SetupLayersAndTags();
        EnsureScenesFolder();

        BuildIntroScene();
        LevelBuilders.BuildLevel1();
        LevelBuilders.BuildLevel2();
        LevelBuilders.BuildLevel3();
        BuildOutroScene();

        ConfigureBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Umbra — Done!",
            "Everything is set up.\n\n" +
            "Manual steps (only if needed):\n" +
            "  • In each level scene, Window > AI > Navigation → Bake (auto-baked but you can re-bake)\n" +
            "  • Window > Rendering > Lighting → Generate Lighting (optional)\n\n" +
            "Press Play on Intro scene to start.", "OK");
    }

    [MenuItem("Umbra/Rebuild Level 1")]
    public static void RebuildL1() { EnsureScenesFolder(); LevelBuilders.BuildLevel1(); }

    [MenuItem("Umbra/Rebuild Level 2")]
    public static void RebuildL2() { EnsureScenesFolder(); LevelBuilders.BuildLevel2(); }

    [MenuItem("Umbra/Rebuild Level 3")]
    public static void RebuildL3() { EnsureScenesFolder(); LevelBuilders.BuildLevel3(); }

    [MenuItem("Umbra/Rebuild Intro Scene")]
    public static void RebuildIntro() { EnsureScenesFolder(); BuildIntroScene(); }

    [MenuItem("Umbra/Rebuild Outro Scene")]
    public static void RebuildOutro() { EnsureScenesFolder(); BuildOutroScene(); }

    static void SetupLayersAndTags()
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        AddLayerIfMissing(tagManager, "Wall");
        AddLayerIfMissing(tagManager, "AbsorbableLight");
        AddLayerIfMissing(tagManager, "Player");

        AddTagIfMissing(tagManager, "Guard");

        tagManager.ApplyModifiedProperties();
        Debug.Log("[Umbra] Layers and tags configured.");
    }

    static void AddLayerIfMissing(SerializedObject so, string layerName)
    {
        var layers = so.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (slot.stringValue == layerName) return;
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                return;
            }
        }
        Debug.LogWarning($"[Umbra] Cannot add layer '{layerName}' — no free slots above index 7.");
    }

    static void AddTagIfMissing(SerializedObject so, string tagName)
    {
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
    }

    static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesPath}/Intro.unity",  true),
            new EditorBuildSettingsScene($"{ScenesPath}/Level1.unity", true),
            new EditorBuildSettingsScene($"{ScenesPath}/Level2.unity", true),
            new EditorBuildSettingsScene($"{ScenesPath}/Level3.unity", true),
            new EditorBuildSettingsScene($"{ScenesPath}/Outro.unity",  true),
        };
        Debug.Log("[Umbra] Build Settings configured (5 scenes, indices 0–4).");
    }

    static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder(ScenesPath))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    static void BuildIntroScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Сюда кладём персистентные менеджеры (DontDestroyOnLoad).
        SceneBuilderUtils.CreateManagers();

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.AddComponent<AudioListener>();

        var canvas = CreateCanvas("IntroCanvas");

        CreateImage(canvas.transform, "BG", Color.black,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        CreateLabel(canvas.transform, "Title", "UMBRA",
            new Vector2(0, 100), new Vector2(700, 130), 80, FontStyles.Bold,
            new Color(0.85f, 0.85f, 1f));

        CreateLabel(canvas.transform, "Story",
            "You are a creature born of shadow.\nLight burns your essence away.\nInfiltrate the facility. Reach the exit.\nUse darkness as your shield.",
            new Vector2(0, -30), new Vector2(560, 140), 22, FontStyles.Normal,
            new Color(0.6f, 0.6f, 0.8f));

        CreateLabel(canvas.transform, "Controls",
            "WASD — Move    Shift — Sprint    C — Crouch    E — Absorb/Release light\nTrackpad / Right-mouse — Orbit camera    Esc — Pause",
            new Vector2(0, -140), new Vector2(760, 60), 16, FontStyles.Normal,
            new Color(0.4f, 0.4f, 0.55f));

        var startBtn = CreateButton(canvas.transform, "StartButton",
            "ENTER THE SHADOWS", new Vector2(0, -220), new Vector2(260, 58));

        var introUI = new GameObject("IntroUI");
        introUI.AddComponent<IntroUIBridge>();

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Intro.unity");
        Debug.Log("[Umbra] Intro scene saved.");
    }

    static void BuildOutroScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.AddComponent<AudioListener>();

        var canvas = CreateCanvas("OutroCanvas");

        CreateImage(canvas.transform, "BG", Color.black,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        CreateLabel(canvas.transform, "WinTitle", "ESCAPED",
            new Vector2(0, 120), new Vector2(700, 130), 80, FontStyles.Bold,
            new Color(0.3f, 1f, 0.6f));

        CreateLabel(canvas.transform, "WinSub",
            "The shadows carried you to freedom.",
            new Vector2(0, 20), new Vector2(600, 60), 26, FontStyles.Italic,
            new Color(0.6f, 0.85f, 0.7f));

        CreateButton(canvas.transform, "PlayAgainBtn",
            "PLAY AGAIN", new Vector2(-100, -120), new Vector2(200, 55));

        CreateButton(canvas.transform, "QuitBtn",
            "QUIT", new Vector2(110, -120), new Vector2(140, 55));

        var outroUI = new GameObject("OutroUI");
        outroUI.AddComponent<OutroUIBridge>();

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Outro.unity");
        Debug.Log("[Umbra] Outro scene saved.");
    }

    static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        go.AddComponent<GraphicRaycaster>();

        // StandaloneInputModule (legacy) — InputSystemUIInputModule в этой версии падает на AssignDefaultActions().
        var evSys = new GameObject("EventSystem");
        evSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evSys.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        return canvas;
    }

    static void CreateImage(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static void CreateLabel(Transform parent, string name, string text,
        Vector2 anchoredPos, Vector2 size, float fontSize,
        FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = tmp.rectTransform;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.22f, 0.95f);

        var btn = go.AddComponent<Button>();
        var cs = btn.colors;
        cs.highlightedColor = new Color(0.2f, 0.2f, 0.4f);
        cs.pressedColor     = new Color(0.05f, 0.05f, 0.15f);
        btn.colors = cs;

        var rt = img.rectTransform;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var lrt = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        return btn;
    }
}
