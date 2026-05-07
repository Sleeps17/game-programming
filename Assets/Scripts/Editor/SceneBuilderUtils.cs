// Требуемые пакеты: Cinemachine, AI Navigation, TextMeshPro.
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using Unity.AI.Navigation;

public static class SceneBuilderUtils
{
    public static GameObject CreateFloor(Transform parent, Vector3 center, float width, float depth)
    {
        // Базовый плоский кьюб — несёт BoxCollider для игрока и для NavMesh-bake.
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Floor";
        go.transform.SetParent(parent);
        go.transform.position = center - Vector3.up * 0.5f;
        go.transform.localScale = new Vector3(width, 1f, depth);
        go.GetComponent<MeshRenderer>().sharedMaterial = StoneFloorMat();
        MarkNavigationStatic(go);

        // Поверх — тайлы пола из Kenney, чтобы пол выглядел не плоской заливкой.
        var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/Dungeon/floor.fbx");
        if (tilePrefab != null)
        {
            var tilesRoot = new GameObject("FloorTiles");
            tilesRoot.transform.SetParent(parent);
            tilesRoot.transform.position = center;

            int xCount = Mathf.CeilToInt(width);
            int zCount = Mathf.CeilToInt(depth);
            float startX = -width * 0.5f + 0.5f;
            float startZ = -depth * 0.5f + 0.5f;

            for (int x = 0; x < xCount; x++)
            for (int z = 0; z < zCount; z++)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, tilesRoot.transform);
                inst.transform.localPosition = new Vector3(startX + x, 0.01f, startZ + z);
                // Псевдо-случайный поворот на 90° — разбивает регулярность тайлов.
                int rot = ((x * 73856093) ^ (z * 19349663)) & 3;
                inst.transform.localRotation = Quaternion.Euler(0, rot * 90f, 0);
                // Тайлы — чистая декорация, коллайдеры от Kenney срезаем.
                foreach (var c in inst.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            }
        }

        return go;
    }

    public static GameObject CreateWall(Transform parent, string name,
        Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = StoneWallMat();
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.BatchingStatic);
        SetLayer(go, "Wall");
        return go;
    }

    // Материалы сохраняются как ассеты, чтобы сцена ссылалась на файл, а не на in-memory инстанс.
    private const string MaterialsFolder = "Assets/Art/Materials";

    public static Material StoneFloorMat()
        => GetOrCreateMaterial("UmbraStoneFloor", new Color(0.18f, 0.18f, 0.22f));

    public static Material StoneWallMat()
        => GetOrCreateMaterial("UmbraStoneWall",  new Color(0.30f, 0.30f, 0.36f));

    private static Material GetOrCreateMaterial(string name, Color c)
    {
        EnsureMaterialsFolder();
        string path = $"{MaterialsFolder}/{name}.mat";

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = MakeMatteUrpLit(c);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        }
        return mat;
    }

    private static void EnsureMaterialsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
    }

    private static Material MakeMatteUrpLit(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = c;
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0f);
        return mat;
    }

    public static void CreateRoomBorder(Transform parent, Vector3 center,
        float width, float depth, float wallH = 3.5f, float wallT = 0.4f)
    {
        float hw = width * 0.5f, hd = depth * 0.5f, hy = wallH * 0.5f;

        CreateWall(parent, "Wall_N", center + new Vector3(0,  hy,  hd), new Vector3(width + wallT, wallH, wallT));
        CreateWall(parent, "Wall_S", center + new Vector3(0,  hy, -hd), new Vector3(width + wallT, wallH, wallT));
        CreateWall(parent, "Wall_E", center + new Vector3( hw, hy, 0),  new Vector3(wallT, wallH, depth));
        CreateWall(parent, "Wall_W", center + new Vector3(-hw, hy, 0),  new Vector3(wallT, wallH, depth));
    }

    public static GameObject CreateObstacle(Transform parent, Vector3 pos, Vector3 scale, string name = "Obstacle")
    {
        string[] kenneyOptions = {
            "Assets/Art/Dungeon/barrel.fbx",
            "Assets/Art/Dungeon/chest.fbx",
            "Assets/Art/Dungeon/stones.fbx",
            "Assets/Art/Dungeon/rocks.fbx",
            "Assets/Art/Dungeon/wood-structure.fbx",
        };
        // Выбор по хэшу имени — детерминирован, повторные сборки дают тот же результат.
        var pickPath = kenneyOptions[Mathf.Abs(name.GetHashCode()) % kenneyOptions.Length];
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pickPath);

        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos - Vector3.up * 0.5f;
            float kScale = Mathf.Max(scale.x, scale.z, scale.y) * 0.7f;
            go.transform.localScale = Vector3.one * Mathf.Max(0.8f, kScale);

            if (go.GetComponent<Collider>() == null)
            {
                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, 1.5f, 1.2f);
                col.center = new Vector3(0f, 0.75f, 0f);
            }
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
        }

        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.BatchingStatic);
        SetLayer(go, "Wall");
        return go;
    }

    // Чистая декорация без коллайдеров (баннеры, оружие, монеты) — игрок проходит сквозь.
    public static GameObject CreateDecor(Transform parent, string prefabPath, Vector3 pos,
        Vector3 euler = default, float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return null;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        go.transform.eulerAngles = euler;
        go.transform.localScale = Vector3.one * scale;

        foreach (var c in go.GetComponentsInChildren<Collider>())
            UnityEngine.Object.DestroyImmediate(c);

        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
        return go;
    }

    // Настенный факел: постоянный тёплый point-light + визуальная подставка.
    public static GameObject CreateTorch(Transform parent, string name, Vector3 pos,
        float intensity = 1.6f, float range = 5f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var bracket = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Dungeon/wood-support.fbx");
        if (bracket != null)
        {
            var v = (GameObject)PrefabUtility.InstantiatePrefab(bracket, go.transform);
            v.transform.localPosition = Vector3.zero;
            v.transform.localScale = Vector3.one * 0.8f;
            foreach (var c in v.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(c);
        }

        var lightGO = new GameObject("Flame");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        var l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.65f, 0.3f);
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.Soft;

        lightGO.AddComponent<RegisteredLight>();
        return go;
    }

    public static void CreateAmbientLight(Vector3 eulerAngles, float intensity = 0.25f)
    {
        var go = new GameObject("AmbientDirectionalLight");
        go.transform.eulerAngles = eulerAngles;
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = intensity;
        l.color = new Color(0.05f, 0.05f, 0.15f);
        l.shadows = LightShadows.None;
    }

    public static GameObject CreateAbsorbablePointLight(Transform parent, string name,
        Vector3 pos, float intensity = 2f, float range = 8f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.Soft;

        go.AddComponent<RegisteredLight>();

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 2.5f;

        go.AddComponent<AbsorbableLight>();
        SetLayer(go, "AbsorbableLight");

        return go;
    }

    public static GameObject CreateAbsorbableSpotLight(Transform parent, string name,
        Vector3 pos, Vector3 euler, float intensity = 3f, float range = 10f, float angle = 65f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.eulerAngles = euler;

        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.intensity = intensity;
        l.range = range;
        l.spotAngle = angle;
        l.shadows = LightShadows.Soft;

        go.AddComponent<RegisteredLight>();

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 2.5f;

        go.AddComponent<AbsorbableLight>();
        SetLayer(go, "AbsorbableLight");

        return go;
    }

    public static GameObject CreatePermanentSpotLight(Transform parent, string name,
        Vector3 pos, Vector3 euler, float intensity = 1.5f, float range = 8f, float angle = 50f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.eulerAngles = euler;

        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.intensity = intensity;
        l.range = range;
        l.spotAngle = angle;
        l.shadows = LightShadows.Soft;

        go.AddComponent<RegisteredLight>();
        return go;
    }

    // Spot-свет, который качается по yaw — оставляет тёмные сектора, где может прятаться игрок.
    public static GameObject CreateSweepingSpotLight(Transform parent, string name,
        Vector3 pos, Vector3 euler, float halfArc = 45f, float speed = 35f,
        float intensity = 3f, float range = 10f, float angle = 45f, bool absorbable = true)
    {
        var go = MakeSpotLight(parent, name, pos, euler, intensity, range, angle, absorbable);
        var sweep = go.AddComponent<LightSweep>();
        SetPrivate(sweep, "halfArc", halfArc);
        SetPrivate(sweep, "speed",   speed);
        return go;
    }

    // Spot-свет, патрулирующий вдоль X с коротким "затемнением" на концах — окно для прохода.
    public static GameObject CreatePatrollingSpotLight(Transform parent, string name,
        Vector3 pos, Vector3 euler, float halfRange = 4f, float speed = 2.5f,
        float intensity = 3f, float range = 10f, float angle = 45f, bool absorbable = true)
    {
        var go = MakeSpotLight(parent, name, pos, euler, intensity, range, angle, absorbable);
        var patrol = go.AddComponent<LightPatrol>();
        SetPrivate(patrol, "halfRange", halfRange);
        SetPrivate(patrol, "speed",     speed);
        return go;
    }

    private static GameObject MakeSpotLight(Transform parent, string name,
        Vector3 pos, Vector3 euler, float intensity, float range, float angle, bool absorbable)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.eulerAngles = euler;

        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.intensity = intensity;
        l.range = range;
        l.spotAngle = angle;
        l.shadows = LightShadows.Soft;

        go.AddComponent<RegisteredLight>();

        if (absorbable)
        {
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.5f;
            go.AddComponent<AbsorbableLight>();
            SetLayer(go, "AbsorbableLight");
        }
        return go;
    }

    private static void SetPrivate(MonoBehaviour mb, string field, float value)
    {
        var so = new SerializedObject(mb);
        var prop = so.FindProperty(field);
        if (prop != null) { prop.floatValue = value; so.ApplyModifiedProperties(); }
    }

    public static GameObject CreatePlayer(Vector3 pos)
    {
        var root = new GameObject("Player");
        root.tag = "Player";
        root.transform.position = pos;

        var cc = root.AddComponent<CharacterController>();
        cc.height = 0.9f;
        cc.radius = 0.15f;
        cc.center = new Vector3(0f, 0.45f, 0f);

        // Риговая Mixamo/KayKit-модель — Animator-клипы могут её управлять.
        var charPrefab = AnimatedCharacterVisual.LoadRiggedCharacterAsset(
            AnimatedCharacterVisual.PlayerControllerPath);
        if (charPrefab != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 0.5f;

            var animator = visual.GetComponentInChildren<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();

            AnimatedCharacterVisual.ConfigureAnimator(animator, AnimatedCharacterVisual.PlayerControllerPath);
            AnimatedCharacterVisual.ApplyTint(visual, new Color(0.07f, 0.07f, 0.16f));
        }
        else
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Visual";
            fallback.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(fallback.GetComponent<CapsuleCollider>());
            fallback.GetComponent<Renderer>().sharedMaterial = ColorMat(new Color(0.1f, 0.1f, 0.2f));
        }

        root.AddComponent<PlayerController>();
        var health  = root.AddComponent<ShadowHealth>();
        var detector = root.AddComponent<LightDetector>();
        root.AddComponent<LightAbsorber>();

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
        {
            var soDetector = new SerializedObject(detector);
            soDetector.FindProperty("occlusionMask").intValue = 1 << wallLayer;
            soDetector.ApplyModifiedProperties();
        }

        int absLayer = LayerMask.NameToLayer("AbsorbableLight");
        var soController = new SerializedObject(root.GetComponent<PlayerController>());
        if (absLayer >= 0)
            soController.FindProperty("absorbableLightLayer").intValue = 1 << absLayer;
        soController.ApplyModifiedProperties();

        SetLayer(root, "Player");
        return root;
    }

    public static void CreateFollowCamera(Transform playerTransform)
    {
        var mainCamGO = new GameObject("Main Camera");
        mainCamGO.tag = "MainCamera";
        mainCamGO.transform.position = new Vector3(0, 6, -6);
        mainCamGO.AddComponent<Camera>();
        mainCamGO.AddComponent<AudioListener>();
        mainCamGO.AddComponent<CinemachineBrain>();

        // Целимся в "грудь" игрока (pivot модели на ногах, scale 0.5 → ~0.6м);
        // без этого камера смотрела бы в пол.
        Transform aim = playerTransform.Find("CameraTarget");
        if (aim == null)
        {
            var aimGO = new GameObject("CameraTarget");
            aimGO.transform.SetParent(playerTransform, false);
            aimGO.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            aim = aimGO.transform;
        }

        var vcamGO = new GameObject("CM_PlayerFollow");
        var vcam = vcamGO.AddComponent<CinemachineCamera>();
        vcam.Follow = aim;
        vcam.LookAt = aim;

        var follow = vcamGO.AddComponent<CinemachineFollow>();
        // Полу-сверху: ~10.4м дистанции и ~50° наклона — видно и тело игрока, и план уровня.
        follow.FollowOffset = new Vector3(0f, 8f, -6.6f);

        vcamGO.AddComponent<CinemachineRotationComposer>();
        vcamGO.AddComponent<CameraOrbit>();
    }

    public static GameObject CreateGuard(Transform parent, string name,
        Vector3 spawnPos, Vector3[] waypointPositions,
        float viewAngle = 60f, float viewRange = 10f)
    {
        var go = new GameObject(name);
        go.tag = "Guard";
        go.transform.SetParent(parent);
        go.transform.position = spawnPos;

        var charPrefab = AnimatedCharacterVisual.LoadRiggedCharacterAsset(
            AnimatedCharacterVisual.GuardControllerPath);
        if (charPrefab != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab, go.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 0.5f;

            var animator = visual.GetComponentInChildren<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();

            AnimatedCharacterVisual.ConfigureAnimator(animator, AnimatedCharacterVisual.GuardControllerPath);
            AnimatedCharacterVisual.ApplyTint(visual, new Color(0.55f, 0.34f, 0.22f));
        }
        else
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Visual";
            fallback.transform.SetParent(go.transform, false);
            UnityEngine.Object.DestroyImmediate(fallback.GetComponent<CapsuleCollider>());
            fallback.GetComponent<Renderer>().sharedMaterial = ColorMat(new Color(0.75f, 0.2f, 0.2f));
        }

        var capsule = go.AddComponent<CapsuleCollider>();
        capsule.height = 0.9f;
        capsule.radius = 0.175f;
        capsule.center = new Vector3(0f, 0.45f, 0f);

        var agent = go.AddComponent<NavMeshAgent>();
        agent.speed = 2f;
        agent.angularSpeed = 180f;
        agent.stoppingDistance = 0.1f;
        agent.height = 0.9f;
        agent.radius = 0.175f;

        var controller = go.AddComponent<GuardController>();
        var vision     = go.AddComponent<GuardVision>();

        var flashGO = new GameObject("Flashlight");
        flashGO.transform.SetParent(go.transform);
        flashGO.transform.localPosition = new Vector3(0, 0.3f, 0.18f);
        flashGO.transform.localEulerAngles = new Vector3(5f, 0, 0);

        var flashLight = flashGO.AddComponent<Light>();
        flashLight.type = LightType.Spot;
        flashLight.intensity = 2.5f;
        flashLight.range = viewRange + 2f;
        flashLight.spotAngle = viewAngle;
        flashLight.shadows = LightShadows.None;
        flashGO.AddComponent<RegisteredLight>();

        var soVision = new SerializedObject(vision);
        soVision.FindProperty("flashlight").objectReferenceValue = flashLight;
        soVision.FindProperty("viewAngle").floatValue = viewAngle;
        soVision.FindProperty("viewRange").floatValue = viewRange;

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
        {
            soVision.FindProperty("obstacleMask").intValue = 1 << wallLayer;
            soVision.FindProperty("playerMask").intValue =
                1 << LayerMask.NameToLayer("Player");
        }
        soVision.ApplyModifiedProperties();

        var wpParent = new GameObject($"{name}_Waypoints");
        wpParent.transform.SetParent(parent);
        var waypoints = new Transform[waypointPositions.Length];
        for (int i = 0; i < waypointPositions.Length; i++)
        {
            var wp = new GameObject($"WP_{i}");
            wp.transform.SetParent(wpParent.transform);
            wp.transform.position = waypointPositions[i];
            waypoints[i] = wp.transform;
        }

        var soCtrl = new SerializedObject(controller);
        var wpProp = soCtrl.FindProperty("waypoints");
        wpProp.arraySize = waypoints.Length;
        for (int i = 0; i < waypoints.Length; i++)
            wpProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
        soCtrl.ApplyModifiedProperties();

        return go;
    }

    public static void CreateExit(Transform parent, Vector3 pos)
    {
        var go = new GameObject("LevelExit");
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2f, 2.5f, 1.5f);
        col.center = new Vector3(0f, 1.25f, 0f);

        go.AddComponent<LevelGoal>();

        // Если есть префаб ворот из Kenney — берём его, иначе зелёный куб-маяк.
        var gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/Dungeon/gate.fbx");
        if (gatePrefab != null)
        {
            var gate = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab, go.transform);
            gate.name = "GateModel";
            gate.transform.localPosition = Vector3.zero;
        }
        else
        {
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beacon.name = "ExitBeacon";
            beacon.transform.SetParent(go.transform);
            beacon.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            beacon.transform.localScale = new Vector3(1.5f, 2.2f, 0.2f);
            UnityEngine.Object.DestroyImmediate(beacon.GetComponent<BoxCollider>());
            beacon.GetComponent<Renderer>().sharedMaterial =
                ColorMat(new Color(0.2f, 1f, 0.5f));
        }

        var exitLight = new GameObject("ExitLight");
        exitLight.transform.SetParent(go.transform);
        exitLight.transform.localPosition = new Vector3(0, 2f, 0f);
        var el = exitLight.AddComponent<Light>();
        el.type = LightType.Point;
        el.color = new Color(0.3f, 1f, 0.5f);
        el.intensity = 1f;
        el.range = 4f;
    }

    public static void CreateManagers()
    {
        var go = new GameObject("_Managers");
        go.AddComponent<GameManager>();
        go.AddComponent<SceneLoader>();
        go.AddComponent<UIManager>();
    }

    public static void AddNavMeshSurface(GameObject root)
    {
        var surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        // Запекаем на build-time — иначе охранники при Play попадают в мир без NavMesh.
        surface.BuildNavMesh();
    }

    public static void CreateLevelManager(GameObject root, string title, int number)
    {
        var go = new GameObject("LevelManager");
        go.transform.SetParent(root.transform);
        var lm = go.AddComponent<LevelManager>();
        var so = new SerializedObject(lm);
        so.FindProperty("levelTitle").stringValue  = title;
        so.FindProperty("levelNumber").intValue    = number;
        so.ApplyModifiedProperties();
    }

    public static void CreateLightRegistry(GameObject root)
    {
        var go = new GameObject("LightSourceRegistry");
        go.transform.SetParent(root.transform);
        go.AddComponent<LightSourceRegistry>();
    }

    static void MarkNavigationStatic(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.NavigationStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.BatchingStatic);
    }

    public static void SetLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0) go.layer = layer;
    }

    public static Material ColorMat(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }
}
