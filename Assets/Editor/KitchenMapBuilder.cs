using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KitchenMapBuilder
{
    private const string AssetBase = "Assets/_Duong/_Art/KayKit_Restaurant_Bits_1.0/Assets/fbx (unity)/";
    private const string MatBase = "Assets/Quang/Material/";

    [MenuItem("Tools/Build Kitchen Map")]
    public static void BuildMap()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "Kitchen_Map")
        {
            EditorUtility.DisplayDialog("Error", "Please open Kitchen_Map scene first!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Kitchen Map");
        int undoGroup = Undo.GetCurrentGroup();

        // 1. Dọn dẹp đối tượng cũ nếu đã tồn tại
        GameObject existingMap = GameObject.Find("Map_Kitchen");
        if (existingMap != null) Undo.DestroyObjectImmediate(existingMap);

        GameObject oldCam = GameObject.Find("Main Camera");
        if (oldCam != null && GameObject.Find("======Camera=====") != null)
        {
            Undo.DestroyObjectImmediate(oldCam);
        }

        GameObject oldChar = GameObject.Find("Charcater");
        if (oldChar != null)
        {
            Undo.DestroyObjectImmediate(oldChar);
        }

        // Tải các vật liệu sẵn có
        Material deadMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Dead.mat");
        Material winMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Win_Area.mat");
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Wood_Color.mat");

        // Tạo vật liệu lửa cam rực rỡ nếu chưa có
        Material fireMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kitchen_FireHazard.mat");
        if (fireMat == null)
        {
            fireMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            fireMat.name = "Kitchen_FireHazard";
            fireMat.color = new Color(1f, 0.25f, 0f, 1f);
            fireMat.EnableKeyword("_EMISSION");
            fireMat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0f) * 3f);
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateAsset(fireMat, "Assets/Materials/Kitchen_FireHazard.mat");
        }

        // Root chính
        GameObject mapRoot = new GameObject("Map_Kitchen");
        Undo.RegisterCreatedObjectUndo(mapRoot, "Create Map_Kitchen");

        // Nhóm các phân vùng
        GameObject roomGroup = CreateChild(mapRoot, "Room_Architecture");
        GameObject zone1Group = CreateChild(mapRoot, "Zone1_PrepIsland");
        GameObject zone2Group = CreateChild(mapRoot, "Zone2_CookingFloor");
        GameObject zone3Group = CreateChild(mapRoot, "Zone3_PantryStorage");
        GameObject zone4Group = CreateChild(mapRoot, "Zone4_ExitDoorway");
        GameObject deadZoneGroup = CreateChild(mapRoot, "DeadZones");
        GameObject lightsGroup = CreateChild(mapRoot, "Environment_Lighting");
        GameObject gameplayGroup = CreateChild(mapRoot, "Gameplay");

        // =========================================================================
        // 1. ROOM ARCHITECTURE (Tường bao & sàn gạch phòng bếp khổng lồ)
        // =========================================================================
        // Sàn bếp khổng lồ: lát bằng floor_kitchen từ X=-48 đến 48, Z=-20 đến 150
        GameObject floorParent = CreateChild(roomGroup, "Kitchen_Floor_Grid");
        float tileSize = 16f; // Mỗi tấm floor_kitchen gốc 4x4m, scale 4x => 16x16m
        for (float x = -40f; x <= 40f; x += tileSize)
        {
            for (float z = -16f; z <= 152f; z += tileSize)
            {
                SpawnProp("floor_kitchen", floorParent.transform, new Vector3(x, 0f, z), Vector3.zero, new Vector3(4f, 2f, 4f), true, 6);
            }
        }

        // Tường bao quanh phòng bếp (Cao 32m)
        GameObject wallParent = CreateChild(roomGroup, "Kitchen_Perimeter_Walls");
        float wallWidth = 16f; // wall gốc 4m x 4m, scale (4, 8, 4) => rộng 16m, cao 32m
        // Tường trái (X = -48)
        for (float z = -16f; z <= 152f; z += wallWidth)
        {
            string wallType = (Mathf.Abs(z - 48f) < 10f || Mathf.Abs(z - 112f) < 10f) ? "wall_window_open" : "wall";
            SpawnProp(wallType, wallParent.transform, new Vector3(-48f, 16f, z), new Vector3(0f, 90f, 0f), new Vector3(4f, 8f, 4f), true);
        }
        // Tường phải (X = +48)
        for (float z = -16f; z <= 152f; z += wallWidth)
        {
            string wallType = (Mathf.Abs(z - 32f) < 10f || Mathf.Abs(z - 96f) < 10f) ? "wall_window_open" : "wall_decorated";
            SpawnProp(wallType, wallParent.transform, new Vector3(48f, 16f, z), new Vector3(0f, -90f, 0f), new Vector3(4f, 8f, 4f), true);
        }
        // Tường sau lưng bàn bếp (Z = -24)
        for (float x = -40f; x <= 40f; x += wallWidth)
        {
            SpawnProp("wall", wallParent.transform, new Vector3(x, 16f, -24f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        }
        // Tường cuối bếp có ô cửa thông ra ngoài đất (Z = 152)
        for (float x = -40f; x <= 40f; x += wallWidth)
        {
            if (Mathf.Abs(x) < 12f) continue; // Bỏ qua x = -8 và x = +8 để tạo khoảng hở rộng 16m chính giữa
            SpawnProp("wall", wallParent.transform, new Vector3(x, 16f, 152f), new Vector3(0f, 180f, 0f), new Vector3(4f, 8f, 4f), true);
        }
        // Chân tường dưới thềm cửa từ Y = 0 đến 16
        SpawnProp("wall", wallParent.transform, new Vector3(0f, 8f, 152f), new Vector3(0f, 180f, 0f), new Vector3(4f, 4f, 4f), true);

        // Cột trụ kiến trúc các góc
        SpawnProp("pillar_A", roomGroup.transform, new Vector3(-48f, 16f, -24f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        SpawnProp("pillar_A", roomGroup.transform, new Vector3(48f, 16f, -24f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        SpawnProp("pillar_A", roomGroup.transform, new Vector3(-48f, 16f, 152f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        SpawnProp("pillar_A", roomGroup.transform, new Vector3(48f, 16f, 152f), Vector3.zero, new Vector3(4f, 8f, 4f), true);

        // =========================================================================
        // 2. KHU VỰC 1: BÀN ĐẢO BẾP KHỞI ĐẦU (Zone1_PrepIsland) [Z = 0 đến 42, Y = 16]
        // =========================================================================
        GameObject islandBase = CreateChild(zone1Group, "Island_Table_Base");
        SpawnProp("kitchentable_A_large", islandBase.transform, new Vector3(-7f, 0f, 6f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnProp("kitchentable_A_large", islandBase.transform, new Vector3(7f, 0f, 6f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnProp("kitchentable_A_large", islandBase.transform, new Vector3(-7f, 0f, 22f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnProp("kitchentable_A_large", islandBase.transform, new Vector3(7f, 0f, 22f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);

        // Mặt bàn đá cẩm thạch hoàn toàn phẳng cho người chơi và tổ trứng đứng xuất phát
        GameObject tableTopFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(tableTopFloor, "Create TableTopFloor");
        tableTopFloor.name = "Island_TableTop_Surface";
        tableTopFloor.transform.SetParent(islandBase.transform);
        tableTopFloor.transform.position = new Vector3(0f, 16.02f, 14f);
        tableTopFloor.transform.localScale = new Vector3(28f, 0.2f, 32f);
        tableTopFloor.layer = 6; // Ground
        if (woodMat != null) tableTopFloor.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        GameObject islandProps = CreateChild(zone1Group, "Island_Obstacles");
        // Các chướng ngại vật trên bàn đặt phía trước khu vực spawn (từ Z = 14 trở đi)
        SpawnProp("cuttingboard", islandProps.transform, new Vector3(-6f, 16.3f, 14f), new Vector3(0f, 15f, 0f), new Vector3(6f, 2f, 6f), true, 6);
        SpawnProp("plate", islandProps.transform, new Vector3(6f, 16.2f, 14f), Vector3.zero, new Vector3(7f, 2f, 7f), true, 6);
        SpawnProp("food_burger", islandProps.transform, new Vector3(6f, 16.8f, 14f), Vector3.zero, new Vector3(6f, 6f, 6f), true);
        SpawnProp("knife", islandProps.transform, new Vector3(0f, 16.5f, 19f), new Vector3(0f, 90f, 90f), new Vector3(6f, 6f, 6f), true, 6);
        SpawnProp("ketchup", islandProps.transform, new Vector3(-4f, 16.2f, 24f), Vector3.zero, new Vector3(5f, 5f, 5f), true);
        SpawnProp("mustard", islandProps.transform, new Vector3(4f, 16.2f, 24f), Vector3.zero, new Vector3(5f, 5f, 5f), true);
        SpawnProp("jar_A_large", islandProps.transform, new Vector3(0f, 16.2f, 27f), Vector3.zero, new Vector3(5f, 5f, 5f), true);

        // Đoạn hạ cánh an toàn xuống sàn bếp:
        SpawnProp("cuttingboard", islandProps.transform, new Vector3(0f, 13f, 32f), new Vector3(18f, 0f, 0f), new Vector3(8f, 2f, 8f), true, 6);
        SpawnProp("crate", islandProps.transform, new Vector3(0f, 3f, 38f), Vector3.zero, new Vector3(5f, 6f, 5f), true, 6);
        SpawnProp("crate_buns", islandProps.transform, new Vector3(0f, 1f, 43f), Vector3.zero, new Vector3(5f, 2f, 5f), true, 6);

        // =========================================================================
        // 3. KHU VỰC 2: SÀN BẾP & VÙNG BẾP NẤU RỰC LỬA (Zone2_CookingFloor) [Z = 45 đến 95]
        // =========================================================================
        GameObject stoveObstacles = CreateChild(zone2Group, "Stoves_And_Hazards");
        SpawnProp("stove_multi", stoveObstacles.transform, new Vector3(-16f, 3f, 66f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("pot_large", stoveObstacles.transform, new Vector3(-16f, 6f, 66f), Vector3.zero, new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("stove_multi", stoveObstacles.transform, new Vector3(16f, 3f, 66f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("oven", stoveObstacles.transform, new Vector3(16f, 3f, 78f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);

        // Chảo lửa rực cháy giữa sàn
        SpawnProp("pan_006", stoveObstacles.transform, new Vector3(0f, 0.5f, 68f), Vector3.zero, new Vector3(10f, 4f, 10f), true);

        // Vùng lửa chết Dead Zone 2: Đặt khối lửa có gắn tag "Dead"
        GameObject fireHazard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(fireHazard, "Create FireHazard");
        fireHazard.name = "DeadZone_StoveFire_Center";
        fireHazard.tag = "Dead";
        fireHazard.transform.SetParent(deadZoneGroup.transform);
        fireHazard.transform.position = new Vector3(0f, 1.2f, 68f);
        fireHazard.transform.localScale = new Vector3(16f, 2.5f, 16f);
        var fireRenderer = fireHazard.GetComponent<MeshRenderer>();
        if (fireRenderer != null) fireRenderer.sharedMaterial = fireMat;

        GameObject fireLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(fireLeft, "Create FireLeft");
        fireLeft.name = "DeadZone_StoveFire_Left";
        fireLeft.tag = "Dead";
        fireLeft.transform.SetParent(deadZoneGroup.transform);
        fireLeft.transform.position = new Vector3(-16f, 6f, 66f);
        fireLeft.transform.localScale = new Vector3(10f, 2f, 10f);
        if (fireLeft.GetComponent<MeshRenderer>() != null) fireLeft.GetComponent<MeshRenderer>().sharedMaterial = fireMat;

        // Bục nhảy an toàn parkour băng qua vùng lửa:
        SpawnProp("chair_stool", zone2Group.transform, new Vector3(-6f, 1.5f, 54f), Vector3.zero, new Vector3(6f, 4f, 6f), true, 6);
        SpawnProp("kitchencounter_straight_A", zone2Group.transform, new Vector3(-8f, 2.5f, 62f), Vector3.zero, new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("lid_large", zone2Group.transform, new Vector3(0f, 6.2f, 68f), new Vector3(180f, 0f, 0f), new Vector3(7f, 2f, 7f), true, 6);
        SpawnProp("dishrack", zone2Group.transform, new Vector3(7f, 2.2f, 76f), Vector3.zero, new Vector3(6f, 5f, 6f), true, 6);
        SpawnProp("kitchentable_B", zone2Group.transform, new Vector3(0f, 1f, 85f), Vector3.zero, new Vector3(6f, 2f, 6f), true, 6);

        // =========================================================================
        // 4. KHU VỰC 3: KHO CHỨA THỰC PHẨM (Zone3_PantryStorage) [Z = 95 đến 145]
        // =========================================================================
        GameObject pantryArch = CreateChild(zone3Group, "Pantry_Archway");
        SpawnProp("wall_doorway", pantryArch.transform, new Vector3(0f, 8f, 96f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnProp("wall", pantryArch.transform, new Vector3(-16f, 8f, 96f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnProp("wall", pantryArch.transform, new Vector3(16f, 8f, 96f), Vector3.zero, new Vector3(4f, 4f, 4f), true);

        // Parkour vươn cao trong kho (Vertical Climb):
        SpawnProp("crate_carrots", zone3Group.transform, new Vector3(-5f, 1.75f, 104f), Vector3.zero, new Vector3(6f, 4.5f, 6f), true, 6);
        SpawnProp("crate_cheese", zone3Group.transform, new Vector3(4f, 3.25f, 110f), Vector3.zero, new Vector3(6f, 4.5f, 6f), true, 6);
        SpawnProp("crate_potatoes", zone3Group.transform, new Vector3(-4f, 4.75f, 116f), Vector3.zero, new Vector3(6f, 4.5f, 6f), true, 6);
        SpawnProp("kitchencabinet", zone3Group.transform, new Vector3(3f, 6.5f, 122f), Vector3.zero, new Vector3(5f, 7f, 5f), true, 6);

        // Tủ lạnh đôi khổng lồ đứng sừng sững ở Z = 131: Nóc tủ lạnh tại Y = 18m
        SpawnProp("fridge_A_decorated", zone3Group.transform, new Vector3(-7f, 9f, 131f), Vector3.zero, new Vector3(5f, 7.5f, 5f), true, 6);
        SpawnProp("fridge_B", zone3Group.transform, new Vector3(7f, 9f, 131f), Vector3.zero, new Vector3(5f, 7.5f, 5f), true, 6);

        // Cầu ống hút khói inox nối từ nóc tủ lạnh sang cửa ra ngoài (Y = 17.5, Z = 139)
        SpawnProp("extractorhood", zone3Group.transform, new Vector3(0f, 17.5f, 139f), new Vector3(180f, 0f, 0f), new Vector3(7f, 3f, 6f), true, 6);
        // Kệ treo tường trước cửa (Y = 16.5, Z = 145)
        SpawnProp("shelf_papertowel_decorated", zone3Group.transform, new Vector3(0f, 16.5f, 145f), Vector3.zero, new Vector3(7f, 3f, 5f), true, 6);

        // =========================================================================
        // 5. KHU VỰC 4: CỬA RA NGOÀI KHU ĐẤT (Zone4_ExitDoorway - Win Area) [Z = 150 đến 180]
        // =========================================================================
        GameObject doorFrame = CreateChild(zone4Group, "Exit_Doorway_Frame");
        SpawnProp("wall_doorway", doorFrame.transform, new Vector3(0f, 16f, 152f), new Vector3(0f, 180f, 0f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnProp("door_A", doorFrame.transform, new Vector3(-3.2f, 16f, 152f), new Vector3(0f, -60f, 0f), new Vector3(4f, 4f, 4f), true, 6);

        GameObject doorStep = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(doorStep, "Create DoorStep");
        doorStep.name = "Door_Threshold_Bridge";
        doorStep.transform.SetParent(zone4Group.transform);
        doorStep.transform.position = new Vector3(0f, 15.5f, 154f);
        doorStep.transform.localScale = new Vector3(12f, 1f, 8f);
        doorStep.layer = 6; // Ground
        if (woodMat != null) doorStep.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        // VÙNG CHIẾN THẮNG (WIN AREA)
        GameObject winArea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(winArea, "Create WinArea");
        winArea.name = "Win_Area_Doorway";
        winArea.tag = "Win_Area";
        winArea.transform.SetParent(zone4Group.transform);
        winArea.transform.position = new Vector3(0f, 19f, 153.5f);
        winArea.transform.localScale = new Vector3(8f, 6f, 2.5f);
        var winCol = winArea.GetComponent<BoxCollider>();
        winCol.isTrigger = false;
        var winRenderer = winArea.GetComponent<MeshRenderer>();
        if (winRenderer != null)
        {
            if (winMat != null) winRenderer.sharedMaterial = winMat;
            else winRenderer.enabled = false;
        }
        winArea.AddComponent<KitchenLevelTransition>();

        // =========================================================================
        // 6. KHU ĐẤT TỰ NHIÊN PHÍA NGOÀI CỬA & TẠO HÌNH TERRAIN (Outdoor Land Preview)
        // =========================================================================
        GameObject outdoorGroup = CreateChild(zone4Group, "Outdoor_Land_Preview");

        // Điêu khắc và căn chỉnh Unity Terrain tự nhiên phía ngoài cửa
        Terrain terrain = GameObject.Find("Terrain")?.GetComponent<Terrain>();
        if (terrain != null)
        {
            terrain.transform.position = new Vector3(-500f, -10f, -200f);
            TerrainData td = terrain.terrainData;
            int res = td.heightmapResolution;
            float[,] heights = td.GetHeights(0, 0, res, res);

            // Đưa toàn bộ nền sàn về 0 (độ cao thế giới Y = -10, an toàn dưới sàn gạch bếp)
            for (int ty = 0; ty < res; ty++)
                for (int tx = 0; tx < res; tx++)
                    heights[ty, tx] = 0f;

            // Đắp đồi thoai thoải phía sau cánh cửa (Z = 152 đến 260, X = -60 đến +60)
            int xCenter = Mathf.RoundToInt(0.5f * (res - 1));
            int zStart = Mathf.RoundToInt(0.352f * (res - 1));
            int zEnd = Mathf.RoundToInt(0.48f * (res - 1));
            int xRadius = Mathf.RoundToInt(0.12f * (res - 1));

            for (int ty = zStart; ty <= zEnd; ty++)
            {
                float zRatio = (float)(ty - zStart) / (zEnd - zStart);
                float zCurve = Mathf.Sin(zRatio * Mathf.PI * 0.5f);
                for (int tx = xCenter - xRadius; tx <= xCenter + xRadius; tx++)
                {
                    float distFromCenter = Mathf.Abs(tx - xCenter) / (float)xRadius;
                    if (distFromCenter <= 1f)
                    {
                        float xCurve = Mathf.Cos(distFromCenter * Mathf.PI * 0.5f);
                        // 0.05 tương đương với độ cao 30m trong Terrain (World Y = -10 + 30 = 20m)
                        float h = 0.05f * xCurve * zCurve;
                        heights[ty, tx] = Mathf.Max(heights[ty, tx], h);
                    }
                }
            }
            td.SetHeights(0, 0, heights);
        }

        GameObject landPlateau = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(landPlateau, "Create LandPlateau");
        landPlateau.name = "Outdoor_GreenHill";
        landPlateau.layer = 6; // Ground
        landPlateau.transform.SetParent(outdoorGroup.transform);
        landPlateau.transform.position = new Vector3(0f, 18.5f, 175f);
        landPlateau.transform.localScale = new Vector3(50f, 3f, 40f);
        Material dirtyRoadMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "DirtyRoad.mat");
        if (dirtyRoadMat != null) landPlateau.GetComponent<MeshRenderer>().sharedMaterial = dirtyRoadMat;

        GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quang/Model3D/BigRock2.fbx");
        if (rockPrefab != null)
        {
            var r1 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, outdoorGroup.transform);
            r1.transform.position = new Vector3(-12f, 20f, 168f);
            r1.transform.localScale = new Vector3(3f, 3f, 3f);
            EnsureCollider(r1, true);

            var r2 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, outdoorGroup.transform);
            r2.transform.position = new Vector3(14f, 20f, 172f);
            r2.transform.localScale = new Vector3(4f, 4f, 4f);
            EnsureCollider(r2, true);
        }

        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quang/Model3D/pine_tree.fbx");
        if (treePrefab != null)
        {
            var t1 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, outdoorGroup.transform);
            t1.transform.position = new Vector3(-18f, 20f, 176f);
            t1.transform.localScale = new Vector3(2f, 2f, 2f);
            EnsureCollider(t1, true);

            var t2 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, outdoorGroup.transform);
            t2.transform.position = new Vector3(18f, 20f, 180f);
            t2.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            EnsureCollider(t2, true);
        }

        // =========================================================================
        // 7. CÁC DEAD ZONE CHUYÊN BIỆT
        // =========================================================================
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_IslandAbyss", new Vector3(0f, 0.5f, 12f), new Vector3(38f, 2f, 36f), deadMat);
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_PantryChasm", new Vector3(0f, 0.5f, 125f), new Vector3(40f, 2f, 45f), deadMat);
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_GlobalFall", new Vector3(0f, -10f, 75f), new Vector3(160f, 4f, 260f), deadMat);

        // =========================================================================
        // 8. CHECKPOINTS
        // =========================================================================
        GameObject flagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quang/Model3D/Checkpoint_Flag3.fbx");
        if (flagPrefab != null)
        {
            CreateCheckpoint(gameplayGroup.transform, flagPrefab, 1, new Vector3(0f, 0.5f, 46f), "Checkpoint_1_FloorArrival");
            CreateCheckpoint(gameplayGroup.transform, flagPrefab, 2, new Vector3(0f, 0.5f, 98f), "Checkpoint_2_PantryEntrance");
            CreateCheckpoint(gameplayGroup.transform, flagPrefab, 3, new Vector3(0f, 16.5f, 146f), "Checkpoint_3_DoorApproach");
        }

        // =========================================================================
        // 9. GAMEPLAY: SPAWN CENTER & NEST
        // =========================================================================
        Vector3 spawnPos = new Vector3(0f, 16.5f, 6f);
        GameObject spawnCenter = GameObject.Find("SpawnCenter");
        if (spawnCenter == null)
        {
            spawnCenter = new GameObject("SpawnCenter");
            Undo.RegisterCreatedObjectUndo(spawnCenter, "Create SpawnCenter");
        }
        spawnCenter.transform.position = spawnPos;

        GameObject nestObj = GameObject.Find("Nest");
        GameObject nestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Nest.prefab");
        if (nestObj == null && nestPrefab != null)
        {
            nestObj = (GameObject)PrefabUtility.InstantiatePrefab(nestPrefab);
            Undo.RegisterCreatedObjectUndo(nestObj, "Create Nest Instance");
            nestObj.name = "Nest";
        }
        if (nestObj != null)
        {
            nestObj.transform.position = new Vector3(0f, 17.0f, 6f);

            // Đảm bảo đầy đủ NetworkRoundManager và NetworkGameEntryGate như MainMap_Online
            var roundMgr = nestObj.GetComponent<NetworkRoundManager>();
            if (roundMgr == null) roundMgr = nestObj.AddComponent<NetworkRoundManager>();
            var entryGate = nestObj.GetComponent<NetworkGameEntryGate>();
            if (entryGate == null) entryGate = nestObj.AddComponent<NetworkGameEntryGate>();

            var nestComp = nestObj.GetComponent<NetworkNest>();
            if (nestComp != null)
            {
                var soNest = new SerializedObject(nestComp);
                soNest.FindProperty("_assistEggCarry").boolValue = true;
                soNest.FindProperty("_smoothOnlineCarry").boolValue = true;
                soNest.FindProperty("_predictCarryInputs").boolValue = true;
                soNest.FindProperty("_showEggTrajectory").boolValue = true;
                soNest.FindProperty("_progressiveEggDamage").boolValue = true;
                soNest.FindProperty("_combinedJumpWindow").floatValue = 0.694f;
                soNest.FindProperty("_eggLaunchUp").floatValue = 0f;
                soNest.FindProperty("_eggLaunchSide").floatValue = 2f;
                soNest.FindProperty("_impactFallUpSpeed").floatValue = 0f;
                soNest.FindProperty("_impactFallSideSpeed").floatValue = 2f;

                var brokenEggAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quang/Model3D/Broken_egg2.fbx");
                if (brokenEggAsset != null)
                {
                    soNest.FindProperty("_brokenEggVisualPrefab").objectReferenceValue = brokenEggAsset;
                }

                var propStartCenter = soNest.FindProperty("startCenter");
                if (propStartCenter != null) propStartCenter.vector3Value = spawnPos;

                soNest.ApplyModifiedProperties();
            }

            if (roundMgr != null)
            {
                var soRound = new SerializedObject(roundMgr);
                var propStart = soRound.FindProperty("_startCenter");
                if (propStart != null) propStart.vector3Value = spawnPos;
                var propDelay = soRound.FindProperty("_eggBreakRespawnDelay");
                if (propDelay != null) propDelay.floatValue = 0.2f;
                soRound.ApplyModifiedProperties();
            }

            if (entryGate != null && nestComp != null)
            {
                var soGate = new SerializedObject(entryGate);
                var propNest = soGate.FindProperty("_nest");
                if (propNest != null)
                {
                    propNest.objectReferenceValue = nestComp;
                    soGate.ApplyModifiedProperties();
                }
            }

            // Gán gate cho GameLoadingPanel để che màn hình khi tải và mở ra khi sẵn sàng
            var loadingPanel = Object.FindAnyObjectByType<GameLoadingPanel>();
            if (loadingPanel != null && entryGate != null)
            {
                var so = new SerializedObject(loadingPanel);
                var prop = so.FindProperty("_gate");
                if (prop != null)
                {
                    prop.objectReferenceValue = entryGate;
                    so.ApplyModifiedProperties();
                }
            }
        }

        // =========================================================================
        // 10. ENVIRONMENT LIGHTING
        // =========================================================================
        GameObject sunObj = GameObject.Find("Directional Light");
        if (sunObj != null)
        {
            sunObj.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var sunLight = sunObj.GetComponent<Light>();
            if (sunLight != null)
            {
                sunLight.color = new Color(1f, 0.95f, 0.85f);
                sunLight.intensity = 1.3f;
                sunLight.shadows = LightShadows.Soft;
            }
        }

        CreatePointLight(lightsGroup.transform, "Light_IslandTable", new Vector3(0f, 25f, 15f), new Color(1f, 0.9f, 0.75f), 2.5f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_StoveFire_Left", new Vector3(-16f, 8f, 66f), new Color(1f, 0.35f, 0.05f), 4.5f, 25f);
        CreatePointLight(lightsGroup.transform, "Light_StoveFire_Center", new Vector3(0f, 4f, 68f), new Color(1f, 0.25f, 0f), 5.5f, 30f);
        CreatePointLight(lightsGroup.transform, "Light_StoveFire_Right", new Vector3(16f, 8f, 66f), new Color(1f, 0.35f, 0.05f), 4.5f, 25f);
        CreatePointLight(lightsGroup.transform, "Light_Pantry", new Vector3(0f, 22f, 120f), new Color(0.9f, 0.95f, 1f), 2.0f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_ExitPortal", new Vector3(0f, 24f, 153f), new Color(1f, 0.92f, 0.6f), 4.0f, 25f);

        EditorSceneManager.MarkSceneDirty(activeScene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("Kitchen_Map built successfully!");
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, "Create " + name);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        return child;
    }

    private static GameObject SpawnProp(string modelName, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, bool addCollider = true, int layer = 0)
    {
        string path = AssetBase + modelName + ".fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("Missing prop: " + path);
            return null;
        }

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(inst, "Spawn " + modelName);
        inst.transform.position = pos;
        inst.transform.rotation = Quaternion.Euler(rot);
        inst.transform.localScale = scale;
        inst.layer = layer;

        if (addCollider) EnsureCollider(inst, false);
        return inst;
    }

    private static void EnsureCollider(GameObject root, bool isConvex)
    {
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            var go = mf.gameObject;
            if (go.GetComponent<Collider>() == null && mf.sharedMesh != null)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = isConvex;
            }
        }
    }

    private static void CreateDeadZone(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject dz = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(dz, "Create " + name);
        dz.name = name;
        dz.tag = "Dead";
        dz.transform.SetParent(parent);
        dz.transform.position = pos;
        dz.transform.localScale = size;
        var r = dz.GetComponent<MeshRenderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;
    }

    private static void CreateCheckpoint(Transform parent, GameObject flagPrefab, int id, Vector3 pos, string name)
    {
        GameObject flag = (GameObject)PrefabUtility.InstantiatePrefab(flagPrefab, parent);
        Undo.RegisterCreatedObjectUndo(flag, "Create " + name);
        flag.name = name;
        flag.transform.position = pos;
        flag.transform.localScale = new Vector3(2f, 2f, 2f);

        var cp = flag.GetComponent<OnlineCheckpoint>();
        if (cp == null) cp = flag.AddComponent<OnlineCheckpoint>();
        var idField = typeof(OnlineCheckpoint).GetField("_checkpointId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (idField != null) idField.SetValue(cp, id);

        var col = flag.GetComponent<BoxCollider>();
        if (col == null) col = flag.AddComponent<BoxCollider>();
        col.size = new Vector3(4f, 6f, 4f);
        col.center = new Vector3(0f, 3f, 0f);
        col.isTrigger = true;
    }

    private static void CreatePointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject lightObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(lightObj, "Create " + name);
        lightObj.transform.SetParent(parent);
        lightObj.transform.position = pos;
        var l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }
}
