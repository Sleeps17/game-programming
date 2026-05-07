using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AnimatedCharacterVisual
{
    public const string RiggedCharacterPath = "Assets/Art/Animations/Idle.fbx";
    public const string PlayerRiggedPrefabPath = "Assets/Art/Characters/Rigged/Player.prefab";
    public const string PlayerRiggedFbxPath = "Assets/Art/Characters/Rigged/Player.fbx";
    public const string GuardRiggedPrefabPath = "Assets/Art/Characters/Rigged/Guard.prefab";
    public const string GuardRiggedFbxPath = "Assets/Art/Characters/Rigged/Guard.fbx";
    public const string KayKitMediumPrefabPath =
        "Assets/KayKit/Characters/KayKit - Free Sample (for Unity)/Prefabs/Mannequin_Medium.prefab";
    public const string KayKitMediumModelPath =
        "Assets/KayKit/Characters/KayKit - Free Sample (for Unity)/Models/Mannequin_Medium.fbx";
    public const string PlayerControllerPath = "Assets/Art/AnimatorControllers/PlayerAnimator.controller";
    public const string GuardControllerPath = "Assets/Art/AnimatorControllers/GuardAnimator.controller";

    private static readonly Dictionary<int, Material> SkinMaterials = new Dictionary<int, Material>();

    public static Animator EnsureAnimator(
        GameObject owner,
        Animator currentAnimator,
        string controllerPath,
        Color tint)
    {
        var childAnimator = owner.GetComponentInChildren<Animator>(true);

#if UNITY_EDITOR
        var candidateAnimator = currentAnimator != null ? currentAnimator : childAnimator;
        if (ShouldReplaceWithPreferredAsset(candidateAnimator, controllerPath))
        {
            var replacement = CreateEditorRiggedVisual(owner.transform, controllerPath, tint);
            if (replacement != null) return replacement;
        }
#endif

        bool isPlayer = controllerPath == PlayerControllerPath;

        if (IsUsableAnimator(currentAnimator))
        {
            ConfigureAnimator(currentAnimator, controllerPath);
            ApplyTint(currentAnimator.gameObject, tint, isPlayer);
            AlignVisualFeetToPivot(currentAnimator.gameObject);
            return currentAnimator;
        }

        if (IsUsableAnimator(childAnimator))
        {
            ConfigureAnimator(childAnimator, controllerPath);
            ApplyTint(childAnimator.gameObject, tint, isPlayer);
            AlignVisualFeetToPivot(childAnimator.gameObject);
            return childAnimator;
        }

#if UNITY_EDITOR
        var visual = CreateEditorRiggedVisual(owner.transform, controllerPath, tint);
        if (visual != null) return visual;
#endif

        return currentAnimator != null ? currentAnimator : childAnimator;
    }

    public static void ConfigureAnimator(Animator target, string controllerPath)
    {
        if (target == null) return;

#if UNITY_EDITOR
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller != null && target.runtimeAnimatorController != controller)
            target.runtimeAnimatorController = controller;
#endif

        target.applyRootMotion = false;
        target.updateMode = AnimatorUpdateMode.Normal;
        target.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
    }

    public static void ApplyTint(GameObject visualRoot, Color tint, bool forceReplaceMaterials = false)
    {
        if (visualRoot == null) return;

        var skinMaterial = GetSkinMaterial(tint);
        var block = new MaterialPropertyBlock();
        foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            // forceReplaceMaterials = жёстко перекрыть материал (для игрока-тени, чтобы текстуры
            // FBX не светились); иначе сохраняем URP-материалы и заменяем только Standard-слоты,
            // которые в URP отрисуются магентой.
            bool needsReplace = forceReplaceMaterials || !AllMaterialsAreUrp(renderer.sharedMaterials);
            if (skinMaterial != null && needsReplace)
            {
                var count = Mathf.Max(1, renderer.sharedMaterials.Length);
                var materials = new Material[count];
                for (int i = 0; i < count; i++)
                    materials[i] = skinMaterial;
                renderer.sharedMaterials = materials;
            }

            block.Clear();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            if (forceReplaceMaterials)
                block.SetColor("_EmissionColor", Color.black);
            renderer.SetPropertyBlock(block);
        }
    }

    // У FBX от Mixamo/Maya pivot обычно на бёдрах — модель оказывается в воздухе.
    // Считаем AABB рендереров и поднимаем визуал так, чтобы низ совпадал с Y pivot'а.
    private static void AlignVisualFeetToPivot(GameObject visual)
    {
        if (visual == null) return;

        Bounds? combined = null;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled) continue;
            var b = renderer.bounds;
            if (b.size == Vector3.zero) continue;
            if (combined == null) combined = b;
            else { var c = combined.Value; c.Encapsulate(b); combined = c; }
        }
        if (combined == null) return;

        float pivotY = visual.transform.position.y;
        float feetY = combined.Value.min.y;
        float dy = pivotY - feetY;
        if (Mathf.Abs(dy) < 0.001f) return;

        var lp = visual.transform.localPosition;
        visual.transform.localPosition = new Vector3(lp.x, lp.y + dy, lp.z);
    }

    private static bool AllMaterialsAreUrp(Material[] materials)
    {
        if (materials == null || materials.Length == 0) return false;
        foreach (var mat in materials)
        {
            if (mat == null) return false;
            var name = mat.shader?.name ?? "";
            if (!name.StartsWith("Universal Render Pipeline") && !name.StartsWith("Shader Graphs/"))
                return false;
        }
        return true;
    }

    private static Material GetSkinMaterial(Color tint)
    {
        var color = (Color32)tint;
        int key = color.r << 24 | color.g << 16 | color.b << 8 | color.a;
        if (SkinMaterials.TryGetValue(key, out var material) && material != null)
            return material;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return null;

        material = new Material(shader)
        {
            name = $"UmbraCharacterSkin_{color.r:X2}{color.g:X2}{color.b:X2}"
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", tint);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.25f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", tint * 0.25f);
        }

        SkinMaterials[key] = material;
        return material;
    }

    private static bool IsUsableAnimator(Animator target)
    {
        return target != null &&
            target.avatar != null &&
            target.avatar.isValid;
    }

#if UNITY_EDITOR
    public static GameObject LoadRiggedCharacterAsset(string controllerPath)
    {
        var preferredPaths = controllerPath == GuardControllerPath
            ? new[] { GuardRiggedPrefabPath, GuardRiggedFbxPath, KayKitMediumPrefabPath, KayKitMediumModelPath }
            : new[] { PlayerRiggedPrefabPath, PlayerRiggedFbxPath, KayKitMediumPrefabPath, KayKitMediumModelPath };

        foreach (var path in preferredPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null) return asset;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(RiggedCharacterPath);
    }

    private static Animator CreateEditorRiggedVisual(Transform owner, string controllerPath, Color tint)
    {
        var prefab = LoadRiggedCharacterAsset(controllerPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[Umbra] Rigged animation model not found. Tried role asset paths and {RiggedCharacterPath}");
            return null;
        }

        HideExistingVisuals(owner);

        var visual = Object.Instantiate(prefab, owner);
        visual.name = "AnimatedVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        ConfigureAnimator(animator, controllerPath);
        ApplyTint(visual, tint, controllerPath == PlayerControllerPath);
        AlignVisualFeetToPivot(visual);
        return animator;
    }

    private static bool ShouldReplaceWithPreferredAsset(Animator target, string controllerPath)
    {
        if (target == null) return false;

        var preferred = LoadRiggedCharacterAsset(controllerPath);
        var preferredPath = AssetDatabase.GetAssetPath(preferred);
        var avatarPath = target.avatar != null ? AssetDatabase.GetAssetPath(target.avatar) : string.Empty;

        if (string.IsNullOrEmpty(preferredPath) || string.IsNullOrEmpty(avatarPath))
            return false;

        if (preferredPath.StartsWith("Assets/KayKit", System.StringComparison.OrdinalIgnoreCase))
            return !avatarPath.StartsWith("Assets/KayKit", System.StringComparison.OrdinalIgnoreCase);

        if (preferredPath.StartsWith("Assets/Art/Characters/Rigged", System.StringComparison.OrdinalIgnoreCase))
            return !avatarPath.StartsWith("Assets/Art/Characters/Rigged", System.StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private static void HideExistingVisuals(Transform owner)
    {
        for (int i = 0; i < owner.childCount; i++)
        {
            var child = owner.GetChild(i);
            if (child.name == "Visual" ||
                child.name == "AnimatedVisual" ||
                child.GetComponentInChildren<Animator>(true) != null)
            {
                child.gameObject.SetActive(false);
            }
        }

    }
#endif
}
