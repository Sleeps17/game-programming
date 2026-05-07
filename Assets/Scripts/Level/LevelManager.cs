using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

// Создаёт недостающие персистентные менеджеры — позволяет Play любую сцену в одиночку.
[DefaultExecutionOrder(-1000)]
public class LevelManager : MonoBehaviour
{
    [Header("Level Info")]
    [SerializeField] private string levelTitle = "Level 1";
    [SerializeField] private int levelNumber = 1;

    private void Awake()
    {
        BootstrapPersistentManagers();
        BootstrapLightRegistry();
        RebuildNavMeshSurfaces();
        SetupShadowAtmosphere();
    }

    private IEnumerator Start()
    {
        GameManager.Instance?.EnterPlayingState();
        yield return null;
        UIManager.Instance?.ShowHUD();
        Debug.Log($"[LevelManager] Started: {levelTitle}");
    }

    private static void BootstrapPersistentManagers()
    {
        GameObject go = null;

        if (GameManager.Instance == null)
        {
            go = GetManagersRoot(go);
            go.AddComponent<GameManager>();
        }

        if (SceneLoader.Instance == null)
        {
            go = GetManagersRoot(go);
            go.AddComponent<SceneLoader>();
        }

        if (UIManager.Instance == null)
        {
            go = GetManagersRoot(go);
            go.AddComponent<UIManager>();
        }
    }

    private static GameObject GetManagersRoot(GameObject current)
    {
        if (current != null) return current;
        var existing = GameObject.Find("_Managers");
        return existing != null ? existing : new GameObject("_Managers");
    }

    private static void BootstrapLightRegistry()
    {
        if (LightSourceRegistry.Instance != null) return;

        var go = new GameObject("LightSourceRegistry");
        go.AddComponent<LightSourceRegistry>();
    }

    private static void RebuildNavMeshSurfaces()
    {
        var surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        PrepareDynamicObjectsForNavMeshBuild();

        foreach (var surface in surfaces)
        {
            if (surface != null && surface.gameObject.activeInHierarchy)
            {
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();
            }
        }
    }

    private static void PrepareDynamicObjectsForNavMeshBuild()
    {
        foreach (var guard in GameObject.FindGameObjectsWithTag("Guard"))
        {
            var modifier = guard.GetComponent<NavMeshModifier>();
            if (modifier == null) modifier = guard.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
        }
    }

    private static void SetupShadowAtmosphere()
    {
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
        RenderSettings.skybox       = null;

        // Агрессивный туман: всё дальше ~18м угасает в чёрный — прячем стены и комнаты вне текущего сектора.
        RenderSettings.fog              = true;
        RenderSettings.fogColor         = Color.black;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 6f;
        RenderSettings.fogEndDistance   = 18f;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.farClipPlane = 22f;
        }

        var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in allLights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 0.04f;
                l.color = new Color(0.5f, 0.55f, 0.8f);
            }
            else if (l.type == LightType.Point)
            {
                l.intensity *= 1.5f;
                l.shadows    = LightShadows.Soft;
                AttachBulb(l);
            }
            else if (l.type == LightType.Spot)
            {
                l.intensity *= 1.5f;
                l.shadows    = LightShadows.Soft;
                AttachBulb(l);
            }
        }
    }

    // Маленькая эмиссивная сфера у лампы — чтобы её было видно издали в темноте.
    private static void AttachBulb(Light l)
    {
        var existing = l.transform.Find("__Bulb");
        if (existing != null) return;

        if (l.type == LightType.Directional) return;

        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "__Bulb";
        bulb.transform.SetParent(l.transform, worldPositionStays: false);
        bulb.transform.localScale = Vector3.one * 0.35f;
        Object.Destroy(bulb.GetComponent<Collider>());

        var mr = bulb.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                     Shader.Find("Unlit/Color");
        var bulbMat = new Material(shader);
        var c = l.color * Mathf.Max(l.intensity, 1f);
        if (bulbMat.HasProperty("_BaseColor")) bulbMat.SetColor("_BaseColor", c);
        bulbMat.color = c;
        mr.sharedMaterial = bulbMat;
    }

}
