using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Генерирует AnimatorController'ы для игрока и охранника. По возможности использует
// клипы из пакета KayKit; иначе — fallback на клипы из Assets/Art/Animations.
public static class AnimatorControllerSetup
{
    private const string ControllersFolder = "Assets/Art/AnimatorControllers";
    private const string KayKitMediumAnimations = "Assets/KayKit/Characters/Animations/Animations/Rig_Medium";
    private static readonly string[] IdleClipCandidates =
    {
        $"{KayKitMediumAnimations}/General/Idle_A.anim",
        "Assets/Art/Animations/Idle.fbx"
    };
    private static readonly string[] RunClipCandidates =
    {
        $"{KayKitMediumAnimations}/Movement Basic/Running_A.anim",
        "Assets/Art/Animations/Running.fbx"
    };
    private static readonly string[] WalkClipCandidates =
    {
        $"{KayKitMediumAnimations}/Movement Basic/Walking_A.anim",
        $"{KayKitMediumAnimations}/Movement Basic/Walking_B.anim",
        $"{KayKitMediumAnimations}/Movement Basic/Walking_C.anim",
        "Assets/Art/Animations/Walking.fbx",
        "Assets/Art/Animations/Walk.fbx",
        "Assets/Art/Animations/Standard Walk.fbx",
        "Assets/Art/Animations/Walk Forward.fbx",
        "Assets/Art/Animations/Walking Forward.fbx"
    };
    private static readonly string[] CrouchClipCandidates =
    {
        $"{KayKitMediumAnimations}/Movement Advanced/Sneaking.anim",
        $"{KayKitMediumAnimations}/Movement Advanced/Crouching.anim",
        "Assets/Art/Animations/Crouched Walking.fbx"
    };
    private static readonly string[] LookingClipCandidates =
    {
        $"{KayKitMediumAnimations}/General/Idle_B.anim",
        "Assets/Art/Animations/Looking Around.fbx"
    };

    [MenuItem("Umbra/Generate Animator Controllers")]
    public static void GenerateAllWithDialog()
    {
        GenerateAll();
        EditorUtility.DisplayDialog("Umbra — Animators",
            "Animator Controllers created in Assets/Art/AnimatorControllers/", "OK");
    }

    public static void GenerateAll()
    {
        EnsureFolder();
        ConfigureAnimationImports();

        BuildPlayerController();
        BuildGuardController();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Umbra] Animator controllers regenerated.");
    }

    static void ConfigureAnimationImports()
    {
        foreach (var fbxPath in FindAnimationFbxPaths())
            ConfigureAnimationImport(fbxPath);
    }

    static void ConfigureAnimationImport(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }
        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        var clips = importer.defaultClipAnimations;
        if (clips != null)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (!clips[i].loopTime || !clips[i].loopPose)
                {
                    clips[i].loopTime = true;
                    clips[i].loopPose = true;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            if (clips != null)
                importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(ControllersFolder))
            AssetDatabase.CreateFolder("Assets/Art", "AnimatorControllers");
    }

    static void BuildPlayerController()
    {
        var path = $"{ControllersFolder}/PlayerAnimator.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed",       AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;

        var idle    = sm.AddState("Idle");
        var walking = sm.AddState("Walking");
        var running = sm.AddState("Running");
        var crouch  = sm.AddState("CrouchedWalking");

        var idleClip = LoadFirstClip(IdleClipCandidates);
        var runClip = LoadFirstClip(RunClipCandidates);
        var walkClip = LoadWalkClip() ?? runClip;

        idle.motion    = idleClip;
        walking.motion = walkClip;
        running.motion = runClip;
        crouch.motion  = LoadFirstClip(CrouchClipCandidates);

        walking.speed = walkClip == runClip ? 0.62f : 1f;
        running.speed = 1f;
        crouch.speed = 1f;

        sm.defaultState = idle;

        AddTransition(idle, walking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsCrouching");
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsSprinting");
        });
        AddTransition(idle, running, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsCrouching");
            t.AddCondition(AnimatorConditionMode.If,      0f,   "IsSprinting");
        });

        AddTransition(walking, idle, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });
        AddTransition(running, idle, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });

        AddTransition(walking, running, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.If,      0f,   "IsSprinting");
        });
        AddTransition(running, walking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsSprinting");
        });

        AddTransition(idle, crouch, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsCrouching");
        });
        AddTransition(walking, crouch, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsCrouching");
        });
        AddTransition(running, crouch, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsCrouching");
        });

        AddTransition(crouch, idle, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.Less,  0.1f, "Speed");
        });
        AddTransition(crouch, walking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsCrouching");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsSprinting");
        });
        AddTransition(crouch, running, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsCrouching");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.If,      0f,   "IsSprinting");
        });

        EditorUtility.SetDirty(ctrl);
        Debug.Log($"[Umbra] PlayerAnimator created at {path}");
    }

    static void BuildGuardController()
    {
        var path = $"{ControllersFolder}/GuardAnimator.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed",     AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsLooking", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;

        var idle    = sm.AddState("Idle");
        var walking = sm.AddState("Walking");
        var running = sm.AddState("Running");
        var looking = sm.AddState("LookingAround");

        var idleClip = LoadFirstClip(IdleClipCandidates);
        var runClip = LoadFirstClip(RunClipCandidates);
        var walkClip = LoadWalkClip() ?? runClip;

        idle.motion    = idleClip;
        walking.motion = walkClip;
        running.motion = runClip;
        looking.motion = LoadFirstClip(LookingClipCandidates) ?? idleClip;

        walking.speed = walkClip == runClip ? 0.62f : 1f;
        running.speed = 1f;

        sm.defaultState = idle;

        AddTransition(idle, walking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsRunning");
        });
        AddTransition(idle, running, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.If,      0f,   "IsRunning");
        });
        AddTransition(walking, idle, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });
        AddTransition(running, idle, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });
        AddTransition(walking, running, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.If,      0f,   "IsRunning");
        });
        AddTransition(running, walking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsRunning");
        });

        AddTransition(idle, looking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsLooking");
        });
        AddTransition(walking, looking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsLooking");
        });
        AddTransition(running, looking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsLooking");
        });
        AddTransition(looking, idle, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsLooking");
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });
        AddTransition(looking, walking, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsLooking");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsRunning");
        });
        AddTransition(looking, running, 0.12f, t => {
            t.AddCondition(AnimatorConditionMode.IfNot,   0f,   "IsLooking");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.If,      0f,   "IsRunning");
        });

        EditorUtility.SetDirty(ctrl);
        Debug.Log($"[Umbra] GuardAnimator created at {path}");
    }

    static AnimationClip LoadClip(string assetPath)
    {
        var directClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (directClip != null && !directClip.name.StartsWith("__preview__"))
            return directClip;

        var clip = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        if (clip == null)
            Debug.LogWarning($"[Umbra] Could not find AnimationClip in {assetPath}");
        return clip;
    }

    static AnimationClip LoadFirstClip(params string[] assetPaths)
    {
        foreach (var path in assetPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) continue;

            var clip = LoadClip(path);
            if (clip != null) return clip;
        }
        return null;
    }

    static AnimationClip LoadWalkClip()
    {
        return LoadFirstClip(WalkClipCandidates) ?? FindClipByFileName("walk", "crouch");
    }

    static AnimationClip FindClipByFileName(string required, string excluded)
    {
        foreach (var path in FindAnimationFbxPaths())
        {
            var lower = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (!lower.Contains(required)) continue;
            if (!string.IsNullOrEmpty(excluded) && lower.Contains(excluded)) continue;

            var clip = LoadClip(path);
            if (clip != null) return clip;
        }
        return null;
    }

    static string[] FindAnimationFbxPaths()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Animations"))
            return System.Array.Empty<string>();

        return AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Animations" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    static void AddTransition(AnimatorState from, AnimatorState to,
        float duration, System.Action<AnimatorStateTransition> setup)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = duration;
        t.canTransitionToSelf = false;
        setup(t);
    }
}
