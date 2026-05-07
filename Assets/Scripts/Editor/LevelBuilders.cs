using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Сборка сцен Level1/2/3 — геометрия, свет, охранники, игрок.
public static class LevelBuilders
{
    private const string ScenesPath = "Assets/Scenes";

    // Уровень 1 — "Лаборатория". L-образная планировка, 1 охранник.
    public static void BuildLevel1()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("Level1_Root");
        SceneBuilderUtils.CreateLightRegistry(root);
        SceneBuilderUtils.CreateLevelManager(root, "The Laboratory", 1);
        SceneBuilderUtils.CreateAmbientLight(new Vector3(50, -30, 0), 0.2f);

        // L-форма:
        //   Main lab:   x ∈ [-15, 9], z ∈ [-5, 5]   (24×10, центр -3,0)
        //   North wing: x ∈ [3, 11],  z ∈ [5, 13]   (8×8,   центр 7,9)
        //   Дверь между ними — 3м проём по центру x=7 в северной стене main.
        const float wallH = 3.5f, wallT = 0.4f, hy = wallH * 0.5f;
        const float doorHalf = 1.5f;

        Vector3 mainCenter = new Vector3(-3f, 0f, 0f);
        SceneBuilderUtils.CreateFloor(root.transform, mainCenter, 24f, 10f);

        SceneBuilderUtils.CreateWall(root.transform, "Lab_S",
            mainCenter + new Vector3(0f, hy, -5f), new Vector3(24f + wallT, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "Lab_W",
            mainCenter + new Vector3(-12f, hy, 0f), new Vector3(wallT, wallH, 10f));
        SceneBuilderUtils.CreateWall(root.transform, "Lab_E",
            mainCenter + new Vector3( 12f, hy, 0f), new Vector3(wallT, wallH, 10f));
        // Северная стена main разбита на две — между ними проём шириной 3м у x=7.
        float doorOffset = 10f;
        float leftLen = (12f + (doorOffset - doorHalf));
        float rightLen = (12f - (doorOffset + doorHalf));
        SceneBuilderUtils.CreateWall(root.transform, "Lab_N_Left",
            mainCenter + new Vector3(-12f + leftLen * 0.5f, hy, 5f),
            new Vector3(leftLen, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "Lab_N_Right",
            mainCenter + new Vector3(12f - rightLen * 0.5f, hy, 5f),
            new Vector3(rightLen, wallH, wallT));

        // South wing'а уже закрыта north-стеной main (с проёмом). Нужен только огрызок
        // стены на x ∈ [9, 11], где wing выступает за восточную границу main.
        Vector3 wingCenter = new Vector3(7f, 0f, 9f);
        SceneBuilderUtils.CreateFloor(root.transform, wingCenter, 8f, 8f);
        SceneBuilderUtils.CreateWall(root.transform, "Wing_N",
            wingCenter + new Vector3(0f, hy, 4f), new Vector3(8f + wallT, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "Wing_E",
            wingCenter + new Vector3(4f, hy, 0f), new Vector3(wallT, wallH, 8f));
        SceneBuilderUtils.CreateWall(root.transform, "Wing_W",
            wingCenter + new Vector3(-4f, hy, 0f), new Vector3(wallT, wallH, 8f));
        SceneBuilderUtils.CreateWall(root.transform, "Wing_S_Stub",
            new Vector3(10f, hy, 5f), new Vector3(2f, wallH, wallT));

        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-9f, 0.75f, 2.5f), new Vector3(4f, 1.2f, 1.2f), "LabBench_A");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-9f, 0.75f, -2.5f), new Vector3(4f, 1.2f, 1.2f), "LabBench_B");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(0f, 0.75f, 3f), new Vector3(2f, 1.5f, 2f), "Crate_C");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(2f, 0.75f, -3f), new Vector3(2.5f, 1.5f, 2f), "Barrels_D");

        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(9f, 0.75f, 11.5f), new Vector3(1.6f, 1.5f, 1.6f), "Wing_Crate");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(4.5f, 0.75f, 7f), new Vector3(1.6f, 1.5f, 1.6f), "Wing_Barrel");

        SceneBuilderUtils.AddNavMeshSurface(root);

        SceneBuilderUtils.CreateAbsorbableSpotLight(root.transform,
            "Spot_Static_Entry", new Vector3(-12f, 4.5f, 0f),
            new Vector3(60, 90, 0), 4f, 9f, 38f);

        SceneBuilderUtils.CreateSweepingSpotLight(root.transform,
            "Spot_Sweep_Mid", new Vector3(-3f, 4.5f, 0f),
            new Vector3(60, 0, 0),
            halfArc: 50f, speed: 30f,
            intensity: 4f, range: 10f, angle: 38f);

        // Патрулирующий спот над проёмом — стережёт "бутылочное горлышко" во wing.
        SceneBuilderUtils.CreatePatrollingSpotLight(root.transform,
            "Spot_Patrol_Doorway", new Vector3(7f, 4.5f, 2f),
            new Vector3(60, 0, 0),
            halfRange: 3f, speed: 2.2f,
            intensity: 4f, range: 9f, angle: 36f);

        SceneBuilderUtils.CreateAbsorbablePointLight(root.transform,
            "Point_Wing", new Vector3(7f, 3.5f, 11f), 2.5f, 7f);

        SceneBuilderUtils.CreateTorch(root.transform, "Torch_Lab_W",
            new Vector3(-11.5f, 2f, -4.5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_Lab_E",
            new Vector3(8.5f, 2f, -4.5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_Wing",
            new Vector3(-3f, 2f, 8.5f), 1.4f, 4.5f);

        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/chest.fbx",
            new Vector3(7f, 0f, 12f), new Vector3(0f, 180f, 0f), 1.2f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/weapon-sword.fbx",
            new Vector3(-13f, 0f, -3f), new Vector3(0f, 45f, 0f), 1.0f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/banner.fbx",
            new Vector3(-12f, 1.5f, 1f), new Vector3(0f, 90f, 0f), 1.0f);

        var player = SceneBuilderUtils.CreatePlayer(new Vector3(-13f, 0.5f, 0f));
        SceneBuilderUtils.CreateFollowCamera(player.transform);

        // Охранник ходит "восьмёркой" поперёк лаборатории и заглядывает в проём wing'а.
        SceneBuilderUtils.CreateGuard(root.transform, "Guard_01",
            spawnPos: new Vector3(2f, 0.5f, 0f),
            waypointPositions: new[]
            {
                new Vector3(-2f, 0.5f,  3f),
                new Vector3( 7f, 0.5f,  3f),
                new Vector3( 7f, 0.5f, -3f),
                new Vector3(-2f, 0.5f, -3f),
            },
            viewAngle: 55f, viewRange: 9f);

        SceneBuilderUtils.CreateExit(root.transform, new Vector3(7f, 0.5f, 12.5f));

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level1.unity");
        Debug.Log("[Umbra] Level 1 saved.");
    }

    // Уровень 2 — "Музей". T-форма (две комнаты + коридор + ответвление), 2 охранника.
    public static void BuildLevel2()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("Level2_Root");
        SceneBuilderUtils.CreateLightRegistry(root);
        SceneBuilderUtils.CreateLevelManager(root, "The Museum", 2);

        SceneBuilderUtils.CreateAmbientLight(new Vector3(50, -30, 0), 0.15f);

        // T-форма:
        //   Room A:       x ∈ [-18,-2], z ∈ [-5, 5]  (16×10, центр -10,0)
        //   Corridor:     x ∈ [-2, 4],  z ∈ [-2, 2]  (6×4,   центр  1, 0)
        //   Side Gallery: x ∈ [-1, 3],  z ∈ [-8,-2]  (4×6,   центр  1,-5) — южная ветка
        //   Room B:       x ∈ [ 4,20],  z ∈ [-6, 6]  (16×12, центр 12, 0)
        const float wallH = 3.5f, wallT = 0.4f, hy = wallH * 0.5f;
        const float doorHalf = 1.5f;

        Vector3 roomA = new Vector3(-10f, 0f, 0f);
        SceneBuilderUtils.CreateFloor(root.transform, roomA, 16f, 10f);
        SceneBuilderUtils.CreateWall(root.transform, "WallA_N",
            roomA + new Vector3(0f, hy,  5f), new Vector3(16f + wallT, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "WallA_S",
            roomA + new Vector3(0f, hy, -5f), new Vector3(16f + wallT, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "WallA_W",
            roomA + new Vector3(-8f, hy, 0f), new Vector3(wallT, wallH, 10f));
        SceneBuilderUtils.CreateWall(root.transform, "WallA_E_Top",
            roomA + new Vector3(8f, hy, (5f + doorHalf) * 0.5f),
            new Vector3(wallT, wallH, 5f - doorHalf));
        SceneBuilderUtils.CreateWall(root.transform, "WallA_E_Bot",
            roomA + new Vector3(8f, hy, -(5f + doorHalf) * 0.5f),
            new Vector3(wallT, wallH, 5f - doorHalf));

        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-15f, 0.75f, 2f), new Vector3(2f, 1.5f, 1.6f), "Display_A1");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-15f, 0.75f, -2f), new Vector3(2f, 1.5f, 1.6f), "Display_A2");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-12f, 0.75f, 0f), new Vector3(2.5f, 1.5f, 2f), "Display_A3");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-7f, 0.75f, 2.5f), new Vector3(2f, 1.5f, 1.6f), "Display_A4");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-7f, 0.75f, -2.5f), new Vector3(2f, 1.5f, 1.6f), "Display_A5");

        Vector3 corridorCenter = new Vector3(1f, 0f, 0f);
        SceneBuilderUtils.CreateFloor(root.transform, corridorCenter, 6f, 4f);
        SceneBuilderUtils.CreateWall(root.transform, "Corridor_N",
            corridorCenter + new Vector3(0f, hy,  2f), new Vector3(6f, wallH, wallT));
        // Южная стена коридора разорвана 2м проёмом по центру x=1 (вход в галерею).
        const float galleryDoorHalf = 1f;
        SceneBuilderUtils.CreateWall(root.transform, "Corridor_S_Left",
            corridorCenter + new Vector3(-(3f + galleryDoorHalf) * 0.5f, hy, -2f),
            new Vector3(3f - galleryDoorHalf, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "Corridor_S_Right",
            corridorCenter + new Vector3((3f + galleryDoorHalf) * 0.5f, hy, -2f),
            new Vector3(3f - galleryDoorHalf, wallH, wallT));

        Vector3 gallery = new Vector3(1f, 0f, -5f);
        SceneBuilderUtils.CreateFloor(root.transform, gallery, 4f, 6f);
        SceneBuilderUtils.CreateWall(root.transform, "Gallery_S",
            gallery + new Vector3(0f, hy, -3f), new Vector3(4f + wallT, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "Gallery_W",
            gallery + new Vector3(-2f, hy, 0f), new Vector3(wallT, wallH, 6f));
        SceneBuilderUtils.CreateWall(root.transform, "Gallery_E",
            gallery + new Vector3(2f, hy, 0f), new Vector3(wallT, wallH, 6f));
        // С севера галерея открыта в коридор — отдельной стены не нужно.
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(1f, 0.75f, -7f), new Vector3(1.6f, 1.2f, 1.6f), "Gallery_Plinth");

        Vector3 roomB = new Vector3(12f, 0f, 0f);
        SceneBuilderUtils.CreateFloor(root.transform, roomB, 16f, 12f);
        SceneBuilderUtils.CreateWall(root.transform, "WallB_N",
            roomB + new Vector3(0f, hy,  6f), new Vector3(16f + wallT, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "WallB_S",
            roomB + new Vector3(0f, hy, -6f), new Vector3(16f + wallT, wallH, wallT));
        SceneBuilderUtils.CreateWall(root.transform, "WallB_E",
            roomB + new Vector3(8f, hy, 0f), new Vector3(wallT, wallH, 12f));
        SceneBuilderUtils.CreateWall(root.transform, "WallB_W_Top",
            roomB + new Vector3(-8f, hy, (6f + doorHalf) * 0.5f),
            new Vector3(wallT, wallH, 6f - doorHalf));
        SceneBuilderUtils.CreateWall(root.transform, "WallB_W_Bot",
            roomB + new Vector3(-8f, hy, -(6f + doorHalf) * 0.5f),
            new Vector3(wallT, wallH, 6f - doorHalf));

        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(12f, 0.75f, 0f), new Vector3(3f, 2f, 3f), "Centerpiece");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(7f, 0.75f, 4f), new Vector3(2f, 1.5f, 1.6f), "Display_B1");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(7f, 0.75f, -4f), new Vector3(2f, 1.5f, 1.6f), "Display_B2");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(17f, 0.75f, 4f), new Vector3(2f, 1.5f, 1.6f), "Display_B3");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(17f, 0.75f, -4f), new Vector3(2f, 1.5f, 1.6f), "Display_B4");

        SceneBuilderUtils.AddNavMeshSurface(root);

        SceneBuilderUtils.CreateSweepingSpotLight(root.transform,
            "Spot_A_Sweep", new Vector3(-10f, 4f, 0f),
            new Vector3(60, 0, 0),
            halfArc: 45f, speed: 28f,
            intensity: 3.5f, range: 10f, angle: 38f);
        SceneBuilderUtils.CreateAbsorbablePointLight(root.transform,
            "Point_A2", new Vector3(-15f, 3.5f, 0f), 2f, 7f);

        SceneBuilderUtils.CreatePermanentSpotLight(root.transform,
            "Spot_Corridor", new Vector3(1f, 3f, 0f),
            new Vector3(90, 0, 0), 0.8f, 5f, 40f);

        SceneBuilderUtils.CreateAbsorbableSpotLight(root.transform,
            "Spot_Gallery", new Vector3(1f, 3.5f, -5f),
            new Vector3(90, 0, 0), 3f, 8f, 60f);

        SceneBuilderUtils.CreateSweepingSpotLight(root.transform,
            "Spot_B_Sweep", new Vector3(12f, 4f, 0f),
            new Vector3(60, 0, 0),
            halfArc: 50f, speed: 26f,
            intensity: 3.5f, range: 11f, angle: 40f);
        SceneBuilderUtils.CreatePatrollingSpotLight(root.transform,
            "Spot_B_Patrol", new Vector3(17f, 4f, 0f),
            new Vector3(60, 0, 0),
            halfRange: 3f, speed: 2f,
            intensity: 3.5f, range: 9f, angle: 36f);

        SceneBuilderUtils.CreateTorch(root.transform, "Torch_A_NW",
            new Vector3(-17.5f, 2f, 4.5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_A_SW",
            new Vector3(-17.5f, 2f, -4.5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_B_NE",
            new Vector3(19.5f, 2f, 5.5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_B_SE",
            new Vector3(19.5f, 2f, -5.5f), 1.4f, 4.5f);

        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/banner.fbx",
            new Vector3(-18f, 1.5f, 0f), new Vector3(0f, 90f, 0f), 1.0f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/banner.fbx",
            new Vector3(20f, 1.5f, 0f), new Vector3(0f, -90f, 0f), 1.0f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/chest.fbx",
            new Vector3(1f, 0f, -7.5f), new Vector3(0f, 180f, 0f), 1.2f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/weapon-spear.fbx",
            new Vector3(12f, 0f, 5f), new Vector3(0f, 0f, 0f), 1.0f);

        var player = SceneBuilderUtils.CreatePlayer(new Vector3(-16f, 0.5f, 0f));
        SceneBuilderUtils.CreateFollowCamera(player.transform);

        // Guard_A — "восьмёрка" по комнате A; Guard_B — обход комнаты B по периметру.
        SceneBuilderUtils.CreateGuard(root.transform, "Guard_A",
            new Vector3(-10f, 0.5f, 0f),
            new[]
            {
                new Vector3(-16f, 0.5f,  3f),
                new Vector3(-9f,  0.5f, -3f),
                new Vector3(-3f,  0.5f,  3f),
                new Vector3(-9f,  0.5f,  3f),
            },
            viewAngle: 60f, viewRange: 10f);

        SceneBuilderUtils.CreateGuard(root.transform, "Guard_B",
            new Vector3(12f, 0.5f, 0f),
            new[]
            {
                new Vector3(6f,  0.5f,  4f),
                new Vector3(18f, 0.5f,  4f),
                new Vector3(18f, 0.5f, -4f),
                new Vector3(6f,  0.5f, -4f),
            },
            viewAngle: 65f, viewRange: 10f);

        SceneBuilderUtils.CreateExit(root.transform, new Vector3(19f, 0.5f, 0f));

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level2.unity");
        Debug.Log("[Umbra] Level 2 saved.");
    }

    // Уровень 3 — "Храм". Крестообразная планировка с северным святилищем, 3 охранника.
    public static void BuildLevel3()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("Level3_Root");
        SceneBuilderUtils.CreateLightRegistry(root);
        SceneBuilderUtils.CreateLevelManager(root, "The Temple", 3);

        SceneBuilderUtils.CreateAmbientLight(new Vector3(50, -30, 0), 0.1f);

        // Крестообразная форма с северным святилищем:
        //   Room 1 (вход):     x ∈ [-24,-12], 12×12, центр (-18, 0)  — открыта только восточная стена
        //   Passage 1→2:       x ∈ [-12, -7], 5×4,   центр ( -9.5,0)
        //   Room 2 (центр):    x ∈ [ -7,  7], 14×14, центр (  0, 0)  — E + W + N проёмы
        //   Sanctum (север):   x ∈ [ -3,  3], 6×6,   центр (  0,10)  — только южный проём
        //   Passage 2→3:       x ∈ [  7, 12], 5×4,   центр ( 9.5, 0)
        //   Room 3 (выход):    x ∈ [ 12, 24], 12×12, центр ( 18, 0)  — открыта только западная стена
        BuildTempleRoom(root.transform, new Vector3(-18f, 0f, 0f), 12f, 12f, "R1",
            eastOpen: true, westOpen: false);
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-21f, 1f, 3f), new Vector3(2f, 2f, 2f), "Pillar_R1_A");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-15f, 1f, -3f), new Vector3(2f, 2f, 2f), "Pillar_R1_B");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-22f, 1f, -2f), new Vector3(1.5f, 2f, 1.5f), "Pillar_R1_C");

        BuildPassage(root.transform, new Vector3(-9.5f, 0f, 0f), 5f, 4f, "P12");

        BuildTempleRoom(root.transform, Vector3.zero, 14f, 14f, "R2",
            eastOpen: true, westOpen: true, northOpen: true);

        // Кольцо колонн вокруг центрального алтаря.
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(0f, 1f, 0f), new Vector3(2.5f, 2f, 2.5f), "CentralAltar");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-3.5f, 1f, 3.5f), new Vector3(1.4f, 2f, 1.4f), "Pillar_R2_NW");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3( 3.5f, 1f, 3.5f), new Vector3(1.4f, 2f, 1.4f), "Pillar_R2_NE");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-3.5f, 1f, -3.5f), new Vector3(1.4f, 2f, 1.4f), "Pillar_R2_SW");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3( 3.5f, 1f, -3.5f), new Vector3(1.4f, 2f, 1.4f), "Pillar_R2_SE");
        // Дополнительные колонны по бокам — разбивают длинные линии прямой видимости.
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(-5.5f, 1f, 0f), new Vector3(1.4f, 2f, 1.4f), "Pillar_R2_W");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3( 5.5f, 1f, 0f), new Vector3(1.4f, 2f, 1.4f), "Pillar_R2_E");

        // 6×6 пристройка к северной стене Room 2. Своей южной стены тут нет — её роль
        // играет северная стена Room 2 с проёмом; иначе было бы z-fighting.
        Vector3 sanctum = new Vector3(0f, 0f, 10f);
        const float wallHs = 4f, wallTs = 0.4f, hys = wallHs * 0.5f;
        SceneBuilderUtils.CreateFloor(root.transform, sanctum, 6f, 6f);
        SceneBuilderUtils.CreateWall(root.transform, "Sanctum_N",
            sanctum + new Vector3(0f, hys, 3f), new Vector3(6f + wallTs, wallHs, wallTs));
        SceneBuilderUtils.CreateWall(root.transform, "Sanctum_E",
            sanctum + new Vector3(3f, hys, 0f), new Vector3(wallTs, wallHs, 6f));
        SceneBuilderUtils.CreateWall(root.transform, "Sanctum_W",
            sanctum + new Vector3(-3f, hys, 0f), new Vector3(wallTs, wallHs, 6f));

        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(0f, 1f, 12f), new Vector3(2f, 2f, 2f), "SanctumAltar");

        BuildPassage(root.transform, new Vector3(9.5f, 0f, 0f), 5f, 4f, "P23");

        BuildTempleRoom(root.transform, new Vector3(18f, 0f, 0f), 12f, 12f, "R3",
            eastOpen: false, westOpen: true);
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(15f, 1f, 3f), new Vector3(2f, 2f, 2f), "Pillar_R3_A");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(21f, 1f, -3f), new Vector3(2f, 2f, 2f), "Pillar_R3_B");
        SceneBuilderUtils.CreateObstacle(root.transform,
            new Vector3(22f, 1f, 4f), new Vector3(1.5f, 2f, 1.5f), "Pillar_R3_C");

        SceneBuilderUtils.AddNavMeshSurface(root);

        SceneBuilderUtils.CreateSweepingSpotLight(root.transform,
            "Temple_R1_Sweep", new Vector3(-18f, 4f, 0f),
            new Vector3(60, 0, 0),
            halfArc: 50f, speed: 28f,
            intensity: 3.5f, range: 11f, angle: 40f);

        SceneBuilderUtils.CreateAbsorbablePointLight(root.transform,
            "Temple_R2_W", new Vector3(-4f, 4f, 3f), 3f, 9f);
        SceneBuilderUtils.CreatePatrollingSpotLight(root.transform,
            "Temple_R2_Patrol", new Vector3(4f, 4f, 0f),
            new Vector3(60, 0, 0),
            halfRange: 3f, speed: 2f,
            intensity: 3.5f, range: 9f, angle: 38f);
        SceneBuilderUtils.CreatePermanentSpotLight(root.transform,
            "Temple_R2_Altar", new Vector3(0f, 4.5f, 0f),
            new Vector3(90, 0, 0), 1.5f, 4f, 35f);

        SceneBuilderUtils.CreateAbsorbablePointLight(root.transform,
            "Temple_Sanctum", new Vector3(0f, 4f, 11f), 3f, 8f);

        SceneBuilderUtils.CreateAbsorbablePointLight(root.transform,
            "Temple_R3", new Vector3(18f, 4f, 0f), 3.5f, 11f);
        SceneBuilderUtils.CreatePermanentSpotLight(root.transform,
            "Temple_R3_Perm", new Vector3(22f, 3.5f, -3f),
            new Vector3(90, 0, 0), 1.2f, 7f, 50f);

        SceneBuilderUtils.CreateTorch(root.transform, "Torch_R1_NW",
            new Vector3(-23.5f, 2f, 5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_R1_SW",
            new Vector3(-23.5f, 2f, -5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_R3_NE",
            new Vector3(23.5f, 2f, 5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_R3_SE",
            new Vector3(23.5f, 2f, -5f), 1.4f, 4.5f);
        SceneBuilderUtils.CreateTorch(root.transform, "Torch_Sanctum",
            new Vector3(0f, 2f, 12.5f), 1.4f, 4.5f);

        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/banner.fbx",
            new Vector3(-23.5f, 1.5f, 0f), new Vector3(0f, 90f, 0f), 1.0f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/banner.fbx",
            new Vector3(23.5f, 1.5f, 0f), new Vector3(0f, -90f, 0f), 1.0f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/chest.fbx",
            new Vector3(0f, 0f, 12.5f), new Vector3(0f, 180f, 0f), 1.2f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/weapon-sword.fbx",
            new Vector3(-21f, 0f, 1.5f), new Vector3(0f, 30f, 0f), 1.0f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/coin.fbx",
            new Vector3(-1f, 0.05f, 11.5f), Vector3.zero, 1.5f);
        SceneBuilderUtils.CreateDecor(root.transform, "Assets/Art/Dungeon/coin.fbx",
            new Vector3(1f, 0.05f, 11.7f), Vector3.zero, 1.5f);

        var player = SceneBuilderUtils.CreatePlayer(new Vector3(-22f, 0.5f, 0f));
        SceneBuilderUtils.CreateFollowCamera(player.transform);

        SceneBuilderUtils.CreateGuard(root.transform, "Temple_Guard_1",
            new Vector3(-18f, 0.5f, 0f),
            new[]
            {
                new Vector3(-22f, 0.5f,  3f),
                new Vector3(-14f, 0.5f,  3f),
                new Vector3(-14f, 0.5f, -3f),
                new Vector3(-22f, 0.5f, -3f),
            },
            viewAngle: 70f, viewRange: 11f);

        // Охранник Room 2: "восьмёркой" между колоннами, периодически заглядывает в Sanctum.
        SceneBuilderUtils.CreateGuard(root.transform, "Temple_Guard_2",
            new Vector3(0f, 0.5f, -5f),
            new[]
            {
                new Vector3( 0f, 0.5f, -5f),
                new Vector3( 5f, 0.5f,  5f),
                new Vector3( 0f, 0.5f,  6f),
                new Vector3(-5f, 0.5f,  5f),
            },
            viewAngle: 75f, viewRange: 12f);

        SceneBuilderUtils.CreateGuard(root.transform, "Temple_Guard_3",
            new Vector3(18f, 0.5f, 0f),
            new[]
            {
                new Vector3(14f, 0.5f,  3f),
                new Vector3(22f, 0.5f,  3f),
                new Vector3(22f, 0.5f, -3f),
                new Vector3(14f, 0.5f, -3f),
            },
            viewAngle: 70f, viewRange: 11f);

        SceneBuilderUtils.CreateExit(root.transform, new Vector3(23.5f, 0.5f, 0f));

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level3.unity");
        Debug.Log("[Umbra] Level 3 saved.");
    }

    // Прямоугольная "храмовая" комната с опциональными проёмами по 3м на любой из 4 сторон.
    static void BuildTempleRoom(Transform parent, Vector3 center, float w, float d, string tag,
        bool eastOpen, bool westOpen, bool northOpen = false, bool southOpen = false,
        float doorWidth = 3f)
    {
        SceneBuilderUtils.CreateFloor(parent, center, w, d);

        const float wallH = 4f, wallT = 0.4f;
        float hw = w * 0.5f, hd = d * 0.5f, hy = wallH * 0.5f;
        float doorHalf = doorWidth * 0.5f;

        BuildEndWall(parent, $"Wall_{tag}_N", center + new Vector3(0f, hy,  hd), w, wallH, wallT,
            open: northOpen, doorHalf: doorHalf);
        BuildEndWall(parent, $"Wall_{tag}_S", center + new Vector3(0f, hy, -hd), w, wallH, wallT,
            open: southOpen, doorHalf: doorHalf);

        BuildSideWall(parent, $"Wall_{tag}_E", center + new Vector3( hw, hy, 0f), d, wallH, wallT,
            open: eastOpen, doorHalf: doorHalf);
        BuildSideWall(parent, $"Wall_{tag}_W", center + new Vector3(-hw, hy, 0f), d, wallH, wallT,
            open: westOpen, doorHalf: doorHalf);
    }

    // Северная/южная стена: либо целиком, либо два сегмента с проёмом по центру.
    static void BuildEndWall(Transform parent, string baseName, Vector3 center, float w,
        float wallH, float wallT, bool open, float doorHalf)
    {
        if (!open)
        {
            SceneBuilderUtils.CreateWall(parent, baseName, center,
                new Vector3(w + wallT, wallH, wallT));
            return;
        }
        float hw = w * 0.5f;
        float segLen = hw - doorHalf;
        float segCenter = (hw + doorHalf) * 0.5f;
        SceneBuilderUtils.CreateWall(parent, $"{baseName}_Left",
            center + new Vector3(-segCenter, 0f, 0f), new Vector3(segLen, wallH, wallT));
        SceneBuilderUtils.CreateWall(parent, $"{baseName}_Right",
            center + new Vector3( segCenter, 0f, 0f), new Vector3(segLen, wallH, wallT));
    }

    // Восточная/западная стена: либо целиком, либо два сегмента с проёмом по центру.
    static void BuildSideWall(Transform parent, string baseName, Vector3 center, float d,
        float wallH, float wallT, bool open, float doorHalf)
    {
        if (!open)
        {
            SceneBuilderUtils.CreateWall(parent, baseName, center,
                new Vector3(wallT, wallH, d));
            return;
        }
        float hd = d * 0.5f;
        float segLen = hd - doorHalf;
        float segCenter = (hd + doorHalf) * 0.5f;
        SceneBuilderUtils.CreateWall(parent, $"{baseName}_Top",
            center + new Vector3(0f, 0f,  segCenter), new Vector3(wallT, wallH, segLen));
        SceneBuilderUtils.CreateWall(parent, $"{baseName}_Bot",
            center + new Vector3(0f, 0f, -segCenter), new Vector3(wallT, wallH, segLen));
    }

    static void BuildPassage(Transform parent, Vector3 center, float length, float width, string tag)
    {
        const float wallH = 4f, wallT = 0.4f;
        float hy = wallH * 0.5f;
        SceneBuilderUtils.CreateFloor(parent, center, length, width);
        SceneBuilderUtils.CreateWall(parent, $"Pass_{tag}_N",
            center + new Vector3(0f, hy,  width * 0.5f), new Vector3(length, wallH, wallT));
        SceneBuilderUtils.CreateWall(parent, $"Pass_{tag}_S",
            center + new Vector3(0f, hy, -width * 0.5f), new Vector3(length, wallH, wallT));
    }
}
