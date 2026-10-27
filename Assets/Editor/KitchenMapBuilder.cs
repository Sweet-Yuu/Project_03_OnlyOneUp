using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trình tạo bản đồ nhà bếp (Kitchen_Map) chuyên nghiệp và sống động.
/// Đã được thiết kế lại để loại bỏ sự lặp lại đơn điệu ở hai bên tường,
/// bổ sung các phân khu chức năng chân thực (Rửa chén, Kho lạnh, Bàn ăn, Quầy phục vụ, Sơ chế)
/// và tích hợp hệ thống câu đố phối hợp (Co-op Burner Puzzle & Security Shutter Gate).
/// </summary>
public static class KitchenMapBuilder
{
    private const string AssetBase = "Assets/_Duong/_Art/KayKit_Restaurant_Bits_1.0/Assets/fbx (unity)/";
    private const string MatBase = "Assets/Quang/Material/";

    [MenuItem("Tools/Build Kitchen Map %#k")]
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

        // 2. Tải và chuẩn bị vật liệu thẩm mỹ
        Material deadMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Dead.mat");
        Material winMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Win_Area.mat");
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Wood_Color.mat");

        Material fireMat = GetOrCreateMaterial("Assets/Materials/Kitchen_FireHazard.mat",
            new Color(1f, 0.25f, 0f, 1f), new Color(1f, 0.2f, 0f) * 3f);
        Material plateIdleMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Plate_Idle.mat",
            new Color(0.18f, 0.18f, 0.2f, 1f), new Color(1f, 0.15f, 0.05f) * 2f, 0.8f, 0.6f);
        Material plateActiveMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Plate_Active.mat",
            new Color(0.25f, 0.22f, 0.15f, 1f), new Color(1f, 0.8f, 0.1f) * 3f, 0.8f, 0.7f);
        Material plateSolvedMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Plate_Solved.mat",
            new Color(0.15f, 0.25f, 0.18f, 1f), new Color(0.1f, 1f, 0.35f) * 3.5f, 0.8f, 0.8f);
        Material gateMetalMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Gate_Metal.mat",
            new Color(0.22f, 0.24f, 0.27f, 1f), Color.black, 0.85f, 0.45f);
        Material lampRedMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Lamp_Red.mat",
            new Color(1f, 0.1f, 0.05f, 1f), new Color(1f, 0.1f, 0.05f) * 3f);
        Material lampGreenMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Lamp_Green.mat",
            new Color(0.1f, 1f, 0.3f, 1f), new Color(0.1f, 1f, 0.3f) * 3f);

        // 3. Khởi tạo cấu trúc Root phân vùng
        GameObject mapRoot = new GameObject("Map_Kitchen");
        Undo.RegisterCreatedObjectUndo(mapRoot, "Create Map_Kitchen");

        GameObject roomGroup = CreateChild(mapRoot, "Room_Architecture");
        GameObject zone1Group = CreateChild(mapRoot, "Zone1_PrepIsland");
        GameObject zone2Group = CreateChild(mapRoot, "Zone2_CookingFloor");
        GameObject zone3Group = CreateChild(mapRoot, "Zone3_PantryStorage");
        GameObject zone4Group = CreateChild(mapRoot, "Zone4_ExitDoorway");
        GameObject deadZoneGroup = CreateChild(mapRoot, "DeadZones");
        GameObject lightsGroup = CreateChild(mapRoot, "Environment_Lighting");
        GameObject gameplayGroup = CreateChild(mapRoot, "Gameplay");

        // =========================================================================
        // 1. ROOM ARCHITECTURE (SÀN GẠCH & TƯỜNG BAO KIẾN TRÚC ĐA DẠNG)
        // =========================================================================
        // Sàn gạch ca rô kích thước lớn bao phủ toàn bộ lòng bếp
        GameObject floorParent = CreateChild(roomGroup, "Kitchen_Floor_Grid");
        float tileSize = 16f;
        for (float x = -40f; x <= 40f; x += tileSize)
        {
            for (float z = -16f; z <= 152f; z += tileSize)
            {
                SpawnProp("floor_kitchen", floorParent.transform, new Vector3(x, 0f, z), Vector3.zero, new Vector3(4f, 2f, 4f), true, 6);
            }
        }

        // Tường bao quanh phòng bếp: Đa dạng hóa kiến trúc với cửa sổ mở, cửa sổ đóng và ô trả món
        GameObject wallParent = CreateChild(roomGroup, "Kitchen_Perimeter_Walls");
        float wallWidth = 16f;

        // Tường trái (X = -48): Kết hợp hài hòa tường kín và cửa sổ lấy sáng tự nhiên
        for (float z = -16f; z <= 152f; z += wallWidth)
        {
            string wallType = "wall";
            if (Mathf.Abs(z - 16f) < 5f || Mathf.Abs(z - 112f) < 5f) wallType = "wall_window_open";
            else if (Mathf.Abs(z - (-16f)) < 5f || Mathf.Abs(z - 64f) < 5f) wallType = "wall_window_closed";
            SpawnProp(wallType, wallParent.transform, new Vector3(-48f, 1f, z), new Vector3(0f, 90f, 0f), new Vector3(4f, 8f, 4f), true);
        }

        // Tường phải (X = +48): Bố trí ô trả món nhà hàng (Order Window) và cửa sổ, loại bỏ lặp lại hàng loạt
        for (float z = -16f; z <= 152f; z += wallWidth)
        {
            string wallType = "wall";
            if (Mathf.Abs(z - 0f) < 5f) wallType = "wall_orderwindow_decorated";
            else if (Mathf.Abs(z - 16f) < 5f) wallType = "wall_orderwindow";
            else if (Mathf.Abs(z - 48f) < 5f || Mathf.Abs(z - 96f) < 5f) wallType = "wall_window_open";
            else if (Mathf.Abs(z - 128f) < 5f) wallType = "wall_window_closed";
            SpawnProp(wallType, wallParent.transform, new Vector3(48f, 1f, z), new Vector3(0f, -90f, 0f), new Vector3(4f, 8f, 4f), true);
        }

        // Tường sau lưng bàn đảo xuất phát (Z = -24)
        for (float x = -40f; x <= 40f; x += wallWidth)
        {
            SpawnProp("wall", wallParent.transform, new Vector3(x, 1f, -24f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        }

        // Tường cuối bếp có ô cửa thông ra ngoài đất (Z = 152)
        for (float x = -40f; x <= 40f; x += wallWidth)
        {
            if (Mathf.Abs(x) < 12f) continue;
            SpawnProp("wall", wallParent.transform, new Vector3(x, 1f, 152f), new Vector3(0f, 180f, 0f), new Vector3(4f, 8f, 4f), true);
        }
        SpawnProp("wall", wallParent.transform, new Vector3(-12f, 1f, 152f), new Vector3(0f, 180f, 0f), new Vector3(2f, 8f, 4f), true);
        SpawnProp("wall", wallParent.transform, new Vector3(12f, 1f, 152f), new Vector3(0f, 180f, 0f), new Vector3(2f, 8f, 4f), true);
        SpawnProp("wall", wallParent.transform, new Vector3(0f, 17f, 152f), new Vector3(0f, 180f, 0f), new Vector3(4f, 4f, 4f), true);

        // Cột trụ kiến trúc chịu lực 4 góc phòng
        SpawnProp("pillar_A", roomGroup.transform, new Vector3(-48f, 1f, -24f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        SpawnProp("pillar_A", roomGroup.transform, new Vector3(48f, 1f, -24f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        SpawnProp("pillar_A", roomGroup.transform, new Vector3(-48f, 1f, 152f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        SpawnProp("pillar_A", roomGroup.transform, new Vector3(48f, 1f, 152f), Vector3.zero, new Vector3(4f, 8f, 4f), true);

        // =========================================================================
        // 2. CHI TIẾT NỘI THẤT DỌC HAI BÊN TƯỜNG (DIVERSITY FACILITY STATIONS)
        // =========================================================================
        // --- TƯỜNG TRÁI: KHU VỰC VỆ SINH, BÀN ĂN & KHO LẠNH ---
        GameObject leftStations = CreateChild(roomGroup, "Left_Wall_Stations");

        // 1. Góc bàn ăn & nghỉ ngơi nhân viên (Z = -16 đến 16)
        SpawnProp("table_round_A_decorated", leftStations.transform, new Vector3(-40f, 1f, -6f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnProp("chair_A", leftStations.transform, new Vector3(-44f, 1f, -6f), new Vector3(0f, 90f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnProp("chair_B", leftStations.transform, new Vector3(-36f, 1f, -6f), new Vector3(0f, -90f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnProp("table_round_B", leftStations.transform, new Vector3(-40f, 1f, 10f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnProp("chair_stool", leftStations.transform, new Vector3(-40f, 1f, 16f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnProp("plate", leftStations.transform, new Vector3(-40f, 5.2f, 10f), Vector3.zero, new Vector3(5f, 2f, 5f), true, 6);
        SpawnProp("bowl", leftStations.transform, new Vector3(-40f, 5.5f, 10f), Vector3.zero, new Vector3(4f, 4f, 4f), true);

        // 2. Trạm rửa chén bát & vệ sinh (Z = 30 đến 64)
        SpawnProp("kitchencounter_sink_backsplash", leftStations.transform, new Vector3(-43f, 1f, 36f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("dishrack_plates", leftStations.transform, new Vector3(-43f, 6.2f, 36f), new Vector3(0f, 90f, 0f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnProp("kitchentable_sink_large_decorated", leftStations.transform, new Vector3(-43f, 1f, 52f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("dishrack", leftStations.transform, new Vector3(-43f, 1f, 62f), new Vector3(0f, 90f, 0f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnProp("shelf_papertowel_decorated", leftStations.transform, new Vector3(-45.5f, 10f, 44f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);
        SpawnProp("towelrail", leftStations.transform, new Vector3(-45.5f, 8f, 60f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);
        SpawnProp("bowl_dirty", leftStations.transform, new Vector3(-43f, 6.2f, 52f), Vector3.zero, new Vector3(4f, 4f, 4f), true);

        // 3. Khu bảo quản lạnh & thực phẩm tươi (Z = 70 đến 104)
        SpawnProp("fridge_A_decorated", leftStations.transform, new Vector3(-43f, 1f, 76f), new Vector3(0f, 90f, 0f), new Vector3(5f, 7f, 5f), true, 6);
        SpawnProp("fridge_B", leftStations.transform, new Vector3(-43f, 1f, 88f), new Vector3(0f, 90f, 0f), new Vector3(5f, 7f, 5f), true, 6);
        SpawnProp("crate_cheese", leftStations.transform, new Vector3(-43f, 1f, 96f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnProp("crate_tomatoes", leftStations.transform, new Vector3(-43f, 5f, 96f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnProp("crate_lettuce", leftStations.transform, new Vector3(-37f, 1f, 96f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnProp("jar_A_large", leftStations.transform, new Vector3(-43f, 1f, 102f), Vector3.zero, new Vector3(5f, 5f, 5f), true);
        SpawnProp("jar_B_large", leftStations.transform, new Vector3(-39f, 1f, 102f), Vector3.zero, new Vector3(5f, 5f, 5f), true);

        // 4. Khu làm bánh mì & dự trữ bột (Z = 110 đến 144)
        SpawnProp("kitchentable_B_large", leftStations.transform, new Vector3(-42f, 1f, 120f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("crate_buns", leftStations.transform, new Vector3(-42f, 6.2f, 120f), Vector3.zero, new Vector3(4f, 3f, 4f), true, 6);
        SpawnProp("kitchencounter_straight_B_backsplash", leftStations.transform, new Vector3(-43f, 1f, 134f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("kitchencabinet_half", leftStations.transform, new Vector3(-45.5f, 12f, 120f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);
        SpawnProp("kitchencabinet_half", leftStations.transform, new Vector3(-45.5f, 12f, 134f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);

        // --- TƯỜNG PHẢI: QUẦY PHỤC VỤ, BẾP NẤU CHÍNH & SƠ CHẾ THỰC PHẨM ---
        GameObject rightStations = CreateChild(roomGroup, "Right_Wall_Stations");

        // 1. Cửa trả món nhà hàng & quầy phục vụ (Z = -16 đến 16)
        SpawnProp("kitchencounter_straight_B_backsplash", rightStations.transform, new Vector3(43f, 1f, -4f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("kitchentable_A_large_decorated", rightStations.transform, new Vector3(43f, 1f, 10f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("menu", rightStations.transform, new Vector3(43f, 6.2f, -4f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnProp("food_burger", rightStations.transform, new Vector3(43f, 6.8f, 10f), Vector3.zero, new Vector3(5f, 5f, 5f), true);
        SpawnProp("food_dinner", rightStations.transform, new Vector3(43f, 6.8f, 6f), Vector3.zero, new Vector3(5f, 5f, 5f), true);
        SpawnProp("ketchup", rightStations.transform, new Vector3(41f, 6.2f, 10f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnProp("mustard", rightStations.transform, new Vector3(41f, 6.2f, 6f), Vector3.zero, new Vector3(4f, 4f, 4f), true);

        // 2. Quầy nước sốt & cửa sổ đón nắng (Z = 30 đến 48)
        SpawnProp("kitchencounter_straight_A_backsplash", rightStations.transform, new Vector3(43f, 1f, 38f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("jar_C_large", rightStations.transform, new Vector3(43f, 6.2f, 35f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnProp("jar_D_large", rightStations.transform, new Vector3(43f, 6.2f, 41f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnProp("jar_A_medium", rightStations.transform, new Vector3(40f, 6.2f, 38f), Vector3.zero, new Vector3(4f, 4f, 4f), true);

        // 3. Bếp nấu chính độc nhất của bếp trưởng (Z = 58 đến 76) - CHỈ 1 TRẠM DUY NHẤT
        SpawnProp("oven", rightStations.transform, new Vector3(43f, 1f, 60f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("stove_multi", rightStations.transform, new Vector3(43f, 1f, 68f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("extractorhood", rightStations.transform, new Vector3(43f, 10f, 68f), new Vector3(0f, -90f, 0f), new Vector3(6f, 3.5f, 6f), true, 6);
        SpawnProp("pot_A_stew", rightStations.transform, new Vector3(43f, 6.2f, 68f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnProp("pan_A", rightStations.transform, new Vector3(40f, 6.2f, 68f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnProp("kitchencounter_straight_A", rightStations.transform, new Vector3(43f, 1f, 76f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);

        // 4. Quầy sơ chế thịt & thớt cắt (Z = 86 đến 112)
        SpawnProp("kitchentable_B", rightStations.transform, new Vector3(42f, 1f, 94f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnProp("cuttingboard", rightStations.transform, new Vector3(42f, 6.2f, 94f), Vector3.zero, new Vector3(5f, 2f, 5f), true, 6);
        SpawnProp("knife", rightStations.transform, new Vector3(42f, 6.5f, 94f), new Vector3(0f, 45f, 90f), new Vector3(4f, 4f, 4f), true);
        SpawnProp("crate_steak", rightStations.transform, new Vector3(43f, 1f, 104f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnProp("crate_ham", rightStations.transform, new Vector3(37f, 1f, 104f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnProp("shelf_papertowel", rightStations.transform, new Vector3(45.5f, 10f, 94f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true);

        // 5. Kho củ quả & kệ lưu trữ (Z = 118 đến 148)
        SpawnProp("crate_potatoes", rightStations.transform, new Vector3(43f, 1f, 124f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnProp("crate_onions", rightStations.transform, new Vector3(43f, 5f, 124f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnProp("crate_carrots", rightStations.transform, new Vector3(37f, 1f, 124f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnProp("kitchencabinet", rightStations.transform, new Vector3(45.5f, 10f, 124f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true);
        SpawnProp("kitchencabinet_corner", rightStations.transform, new Vector3(45.5f, 10f, 138f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true);

        // =========================================================================
        // 3. KHU VỰC 1: BÀN ĐẢO BẾP KHỞI ĐẦU (Zone1_PrepIsland) [Z = 0 đến 42, Y = 16]
        // =========================================================================
        GameObject islandBase = CreateChild(zone1Group, "Island_Table_Base");
        SpawnProp("kitchentable_A_large", islandBase.transform, new Vector3(-7f, 0f, 6f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnProp("kitchentable_A_large", islandBase.transform, new Vector3(7f, 0f, 6f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnProp("kitchentable_A_large", islandBase.transform, new Vector3(-7f, 0f, 22f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnProp("kitchentable_A_large", islandBase.transform, new Vector3(7f, 0f, 22f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);

        GameObject tableTopFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(tableTopFloor, "Create TableTopFloor");
        tableTopFloor.name = "Island_TableTop_Surface";
        tableTopFloor.transform.SetParent(islandBase.transform);
        tableTopFloor.transform.position = new Vector3(0f, 16.02f, 14f);
        tableTopFloor.transform.localScale = new Vector3(28f, 0.2f, 32f);
        tableTopFloor.layer = 6;
        if (woodMat != null) tableTopFloor.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        GameObject islandProps = CreateChild(zone1Group, "Island_Obstacles");
        SpawnProp("cuttingboard", islandProps.transform, new Vector3(-6f, 16.3f, 14f), new Vector3(0f, 15f, 0f), new Vector3(6f, 2f, 6f), true, 6);
        SpawnProp("plate", islandProps.transform, new Vector3(6f, 16.2f, 14f), Vector3.zero, new Vector3(7f, 2f, 7f), true, 6);
        SpawnProp("food_burger", islandProps.transform, new Vector3(6f, 16.8f, 14f), Vector3.zero, new Vector3(6f, 6f, 6f), true);
        SpawnProp("knife", islandProps.transform, new Vector3(0f, 16.5f, 19f), new Vector3(0f, 90f, 90f), new Vector3(6f, 6f, 6f), true, 6);
        SpawnProp("ketchup", islandProps.transform, new Vector3(-4f, 16.2f, 24f), Vector3.zero, new Vector3(5f, 5f, 5f), true);
        SpawnProp("mustard", islandProps.transform, new Vector3(4f, 16.2f, 24f), Vector3.zero, new Vector3(5f, 5f, 5f), true);
        SpawnProp("jar_A_large", islandProps.transform, new Vector3(0f, 16.2f, 27f), Vector3.zero, new Vector3(5f, 5f, 5f), true);

        // Đoạn hạ cánh an toàn xuống sàn bếp
        SpawnProp("cuttingboard", islandProps.transform, new Vector3(0f, 13f, 32f), new Vector3(18f, 0f, 0f), new Vector3(8f, 2f, 8f), true, 6);
        SpawnProp("crate", islandProps.transform, new Vector3(0f, 1f, 38f), Vector3.zero, new Vector3(5f, 6.5f, 5f), true, 6);
        SpawnProp("crate_buns", islandProps.transform, new Vector3(0f, 1f, 43f), Vector3.zero, new Vector3(5f, 2f, 5f), true, 6);

        // =========================================================================
        // 4. KHU VỰC 2: SÀN BẾP & CÂU ĐỐ BẾP LỬA ĐÔI (Zone2_CookingFloor & Puzzle) [Z = 45 đến 95]
        // =========================================================================
        // Chuỗi parkour trung tâm dẫn tới trước khu vực giải đố
        SpawnProp("chair_stool", zone2Group.transform, new Vector3(0f, 1f, 52f), Vector3.zero, new Vector3(4f, 3f, 4f), true, 6);
        SpawnProp("kitchentable_B", zone2Group.transform, new Vector3(-4f, 1f, 60f), Vector3.zero, new Vector3(3.5f, 2.5f, 3.5f), true, 6);

        // Bếp đơn và chảo lửa trung tâm (Z = 70)
        SpawnProp("stove_single", zone2Group.transform, new Vector3(0f, 1f, 70f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnProp("pan_006", zone2Group.transform, new Vector3(0f, 5.2f, 70f), Vector3.zero, new Vector3(5f, 2.5f, 5f), true);
        SpawnProp("lid_large", zone2Group.transform, new Vector3(0f, 6.5f, 70f), new Vector3(180f, 0f, 0f), new Vector3(5f, 1.2f, 5f), true, 6);
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_StoveFire_Center", new Vector3(0f, 5.5f, 70f), new Vector3(3.5f, 1.2f, 3.5f), fireMat, false);

        // Bàn đệm bước chân (Z = 76)
        SpawnProp("dishrack", zone2Group.transform, new Vector3(0f, 1f, 76f), Vector3.zero, new Vector3(4.5f, 2.5f, 4.5f), true, 6);

        // -------------------------------------------------------------------------
        // HỆ THỐNG CÂU ĐỐ PHỐI HỢP: CO-OP BURNER PRESSURE PLATES & SECURITY GATE
        // -------------------------------------------------------------------------
        GameObject puzzleRoot = CreateChild(gameplayGroup, "Kitchen_Coop_Puzzle");
        var puzzleCtrl = puzzleRoot.AddComponent<KitchenCoopPuzzleController>();

        // 1. Hai bàn đạp cảm ứng bếp nhiệt đặt ở Z = 82 (Cách nhau 3.6m - chuẩn cự ly 2 chú vịt)
        KitchenPressurePlate plateLeft = CreatePressurePlate(puzzleRoot.transform, "PressurePlate_Left",
            new Vector3(-1.8f, 1.0f, 82f), plateIdleMat, plateActiveMat, plateSolvedMat);
        KitchenPressurePlate plateRight = CreatePressurePlate(puzzleRoot.transform, "PressurePlate_Right",
            new Vector3(1.8f, 1.0f, 82f), plateIdleMat, plateActiveMat, plateSolvedMat);

        // 2. Hàng rào bẫy lửa hazard chắn ngay trước cổng kho (Z = 89)
        GameObject hazardBarrier = CreateHazardFireBarrier(puzzleRoot.transform, "Puzzle_Fire_Hazard_Barrier",
            new Vector3(0f, 1.0f, 89f), fireMat, deadMat);

        // 3. Vòm cửa kho và Cửa sập an toàn chặn lối đi ở Z = 96
        GameObject pantryArch = CreateChild(zone3Group, "Pantry_Archway");
        SpawnProp("wall_doorway", pantryArch.transform, new Vector3(0f, 1f, 96f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnProp("wall", pantryArch.transform, new Vector3(-16f, 1f, 96f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        SpawnProp("wall", pantryArch.transform, new Vector3(16f, 1f, 96f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        SpawnProp("wall", pantryArch.transform, new Vector3(0f, 17f, 96f), Vector3.zero, new Vector3(4f, 4f, 4f), true);

        // Cửa sập cơ quan (KitchenPuzzleGate) trượt lên trần khi giải xong
        KitchenPuzzleGate puzzleGate = CreatePuzzleGate(pantryArch.transform, "Kitchen_Security_Gate",
            new Vector3(0f, 1.0f, 96f), gateMetalMat, lampRedMat, lampGreenMat);

        // Thiết lập liên kết cho bộ điều khiển câu đố
        puzzleCtrl.SetupForEditor(new KitchenPressurePlate[] { plateLeft, plateRight }, puzzleGate, hazardBarrier);

        // =========================================================================
        // 5. KHU VỰC 3: KHO CHỨA THỰC PHẨM (Zone3_PantryStorage) [Z = 98 đến 148]
        // Bố trí lộ trình leo trèo vững chãi, các kiện hàng được nâng đỡ hợp lý
        // =========================================================================
        SpawnProp("crate_carrots", zone3Group.transform, new Vector3(-3.5f, 1f, 106f), Vector3.zero, new Vector3(5f, 3.5f, 5f), true, 6);
        SpawnProp("crate_cheese", zone3Group.transform, new Vector3(3.5f, 1f, 114f), Vector3.zero, new Vector3(5f, 4.0f, 5f), true, 6);
        SpawnProp("kitchentable_B_large", zone3Group.transform, new Vector3(0f, 1f, 122f), Vector3.zero, new Vector3(5f, 3.0f, 5f), true, 6);
        SpawnProp("crate_potatoes", zone3Group.transform, new Vector3(-2.5f, 1f, 130f), Vector3.zero, new Vector3(5f, 4.0f, 5f), true, 6);

        // Tủ lạnh đôi khổng lồ đứng sừng sững hai bên hành lang kho (Z = 138)
        SpawnProp("fridge_A_decorated", zone3Group.transform, new Vector3(-8f, 1f, 138f), Vector3.zero, new Vector3(5f, 7f, 5f), true, 6);
        SpawnProp("fridge_B", zone3Group.transform, new Vector3(8f, 1f, 138f), Vector3.zero, new Vector3(5f, 7f, 5f), true, 6);
        SpawnProp("kitchentable_A", zone3Group.transform, new Vector3(0f, 1f, 138f), Vector3.zero, new Vector3(4f, 2.5f, 5f), true, 6);

        // =========================================================================
        // 6. KHU VỰC 4: CỬA RA NGOÀI KHU ĐẤT (Zone4_ExitDoorway - Win Area) [Z = 150 đến 180]
        // =========================================================================
        GameObject doorFrame = CreateChild(zone4Group, "Exit_Doorway_Frame");
        SpawnProp("wall_doorway", doorFrame.transform, new Vector3(0f, 1f, 152f), new Vector3(0f, 180f, 0f), new Vector3(4f, 4f, 4f), true, 6);
        GameObject doorInst = SpawnProp("door_A", doorFrame.transform, new Vector3(-3.2f, 1f, 152f), new Vector3(0f, -60f, 0f), new Vector3(4f, 4f, 4f), true, 6);

        Material woodDoorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kitchen_ExitDoor_Wood.mat");
        if (woodDoorMat != null && doorInst != null)
        {
            var doorRend = doorInst.GetComponentInChildren<Renderer>();
            if (doorRend != null) doorRend.sharedMaterial = woodDoorMat;
        }

        GameObject doorStep = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(doorStep, "Create DoorStep");
        doorStep.name = "Door_Threshold_Bridge";
        doorStep.transform.SetParent(zone4Group.transform);
        doorStep.transform.position = new Vector3(0f, 0.5f, 154f);
        doorStep.transform.localScale = new Vector3(12f, 1f, 8f);
        doorStep.layer = 6;
        if (woodMat != null) doorStep.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        // VÙNG CHIẾN THẮNG (WIN AREA)
        GameObject winArea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(winArea, "Create WinArea");
        winArea.name = "Win_Area_Doorway";
        winArea.tag = "Win_Area";
        winArea.transform.SetParent(zone4Group.transform);
        winArea.transform.position = new Vector3(0f, 4f, 153.5f);
        winArea.transform.localScale = new Vector3(8f, 6f, 2.5f);
        var winCol = winArea.GetComponent<BoxCollider>();
        winCol.isTrigger = false;
        var winRenderer = winArea.GetComponent<MeshRenderer>();
        if (winRenderer != null) winRenderer.enabled = false;
        winArea.AddComponent<KitchenLevelTransition>();

        // Outdoor Land Preview
        GameObject outdoorGroup = CreateChild(zone4Group, "Outdoor_Land_Preview");
        Terrain terrain = GameObject.Find("Terrain")?.GetComponent<Terrain>();
        if (terrain != null)
        {
            terrain.transform.position = new Vector3(-500f, -10f, -200f);
            TerrainData td = terrain.terrainData;
            int res = td.heightmapResolution;
            float[,] heights = td.GetHeights(0, 0, res, res);
            for (int ty = 0; ty < res; ty++)
                for (int tx = 0; tx < res; tx++)
                    heights[ty, tx] = 0f;
            td.SetHeights(0, 0, heights);
        }

        GameObject landPlateau = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(landPlateau, "Create LandPlateau");
        landPlateau.name = "Outdoor_GreenHill";
        landPlateau.layer = 6;
        landPlateau.transform.SetParent(outdoorGroup.transform);
        landPlateau.transform.position = new Vector3(0f, -0.5f, 175f);
        landPlateau.transform.localScale = new Vector3(50f, 3f, 40f);
        Material dirtyRoadMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "DirtyRoad.mat");
        if (dirtyRoadMat != null) landPlateau.GetComponent<MeshRenderer>().sharedMaterial = dirtyRoadMat;

        GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quang/Model3D/BigRock2.fbx");
        if (rockPrefab != null)
        {
            var r1 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, outdoorGroup.transform);
            r1.transform.position = new Vector3(-12f, 1f, 168f);
            r1.transform.localScale = new Vector3(3f, 3f, 3f);
            EnsureCollider(r1, true);

            var r2 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, outdoorGroup.transform);
            r2.transform.position = new Vector3(14f, 1f, 172f);
            r2.transform.localScale = new Vector3(4f, 4f, 4f);
            EnsureCollider(r2, true);
        }

        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quang/Model3D/pine_tree.fbx");
        if (treePrefab != null)
        {
            var t1 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, outdoorGroup.transform);
            t1.transform.position = new Vector3(-18f, 1f, 176f);
            t1.transform.localScale = new Vector3(2f, 2f, 2f);
            EnsureCollider(t1, true);

            var t2 = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, outdoorGroup.transform);
            t2.transform.position = new Vector3(18f, 1f, 180f);
            t2.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            EnsureCollider(t2, true);
        }

        // =========================================================================
        // 7. CÁC DEAD ZONE CHUYÊN BIỆT
        // =========================================================================
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_GlobalFall", new Vector3(0f, -10f, 75f), new Vector3(160f, 4f, 260f), deadMat, false);

        // =========================================================================
        // 8. CHECKPOINTS (CỘT CỜ CHECKPOINT TẠI CÁC MỐC TRỌNG YẾU)
        // =========================================================================
        GameObject flagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quang/Model3D/Checkpoint_Flag3.fbx");
        if (flagPrefab != null)
        {
            CreateCheckpoint(gameplayGroup.transform, flagPrefab, 1, new Vector3(0f, 1f, 46f), "Checkpoint_1_FloorArrival");
            CreateCheckpoint(gameplayGroup.transform, flagPrefab, 2, new Vector3(0f, 1f, 98f), "Checkpoint_2_PantryEntrance");
            CreateCheckpoint(gameplayGroup.transform, flagPrefab, 3, new Vector3(0f, 1f, 146f), "Checkpoint_3_DoorApproach");
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
        // 10. ENVIRONMENT LIGHTING (ÁNH SÁNG MÔI TRƯỜNG & HIỆU ỨNG ÁNH LỬA)
        // =========================================================================
        GameObject sunObj = GameObject.Find("Directional Light");
        if (sunObj != null)
        {
            sunObj.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var sunLight = sunObj.GetComponent<Light>();
            if (sunLight != null)
            {
                sunLight.color = new Color(1f, 0.95f, 0.88f);
                sunLight.intensity = 1.35f;
                sunLight.shadows = LightShadows.Soft;
            }
        }

        CreatePointLight(lightsGroup.transform, "Light_IslandTable", new Vector3(0f, 25f, 15f), new Color(1f, 0.9f, 0.75f), 2.5f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_LeftWashStation", new Vector3(-38f, 10f, 45f), new Color(0.85f, 0.95f, 1f), 2.8f, 28f);
        CreatePointLight(lightsGroup.transform, "Light_RightChefStation", new Vector3(38f, 10f, 68f), new Color(1f, 0.6f, 0.2f), 3.5f, 28f);
        CreatePointLight(lightsGroup.transform, "Light_StoveFire_Center", new Vector3(0f, 7f, 70f), new Color(1f, 0.25f, 0f), 5.5f, 30f);
        CreatePointLight(lightsGroup.transform, "Light_PuzzleZone", new Vector3(0f, 8f, 85f), new Color(1f, 0.85f, 0.5f), 3.0f, 25f);
        CreatePointLight(lightsGroup.transform, "Light_Pantry", new Vector3(0f, 14f, 120f), new Color(0.9f, 0.95f, 1f), 2.5f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_ExitPortal", new Vector3(0f, 12f, 153f), new Color(1f, 0.92f, 0.6f), 4.0f, 25f);

        EditorSceneManager.MarkSceneDirty(activeScene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("<color=green><b>Kitchen_Map built successfully with diverse stations and co-op puzzle!</b></color>");
    }

    private static KitchenPressurePlate CreatePressurePlate(Transform parent, string name, Vector3 pos,
        Material idleMat, Material activeMat, Material solvedMat)
    {
        GameObject plateRoot = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(plateRoot, "Create " + name);
        plateRoot.transform.SetParent(parent);
        plateRoot.transform.position = pos;

        // Vành kim loại đế bàn đạp nằm sát sàn
        GameObject baseRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseRim.name = "Plate_Base_Rim";
        baseRim.transform.SetParent(plateRoot.transform);
        baseRim.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        baseRim.transform.localScale = new Vector3(2.6f, 0.05f, 2.6f);
        baseRim.layer = 6;
        if (idleMat != null) baseRim.GetComponent<MeshRenderer>().sharedMaterial = idleMat;

        // Tấm đệm giẫm cảm ứng di động (Moving Pad)
        GameObject movingPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        movingPad.name = "Plate_Moving_Pad";
        movingPad.transform.SetParent(plateRoot.transform);
        movingPad.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        movingPad.transform.localScale = new Vector3(2.1f, 0.08f, 2.1f);
        movingPad.layer = 6;
        var padRend = movingPad.GetComponent<MeshRenderer>();
        if (idleMat != null) padRend.sharedMaterial = idleMat;

        // Đèn vầng sáng báo hiệu
        GameObject lightObj = new GameObject("Plate_Status_Light");
        lightObj.transform.SetParent(plateRoot.transform);
        lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var pLight = lightObj.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1f, 0.2f, 0.1f);
        pLight.intensity = 2.5f;
        pLight.range = 5.0f;
        pLight.shadows = LightShadows.None;

        // Collider trigger nhận diện nhân vật giẫm lên
        var triggerCol = plateRoot.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(2.5f, 1.6f, 2.5f);
        triggerCol.center = new Vector3(0f, 0.8f, 0f);

        var comp = plateRoot.AddComponent<KitchenPressurePlate>();
        comp.SetupForEditor(movingPad.transform, padRend, pLight, idleMat, activeMat, solvedMat);
        return comp;
    }

    private static GameObject CreateHazardFireBarrier(Transform parent, string name, Vector3 pos, Material fireMat, Material deadMat)
    {
        GameObject barrierRoot = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(barrierRoot, "Create " + name);
        barrierRoot.transform.SetParent(parent);
        barrierRoot.transform.position = pos;

        // 3 vòi phun lửa/họng đốt bếp gas rực đỏ dàn hàng ngang
        float[] xOffsets = new float[] { -2.0f, 0f, 2.0f };
        for (int i = 0; i < xOffsets.Length; i++)
        {
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flame.name = $"Flame_Emitter_{i}";
            flame.transform.SetParent(barrierRoot.transform);
            flame.transform.localPosition = new Vector3(xOffsets[i], 0.4f, 0f);
            flame.transform.localScale = new Vector3(1.4f, 0.8f, 1.4f);
            var col = flame.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var rend = flame.GetComponent<MeshRenderer>();
            if (rend != null && fireMat != null) rend.sharedMaterial = fireMat;
        }

        // Đèn hắt ánh lửa bập bùng
        GameObject fireLightObj = new GameObject("Barrier_Fire_Light");
        fireLightObj.transform.SetParent(barrierRoot.transform);
        fireLightObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var l = fireLightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.28f, 0.05f);
        l.intensity = 6.0f;
        l.range = 14f;

        // Collider trigger DeadZone gây vỡ trứng/hạ gục nếu cố vượt qua khi chưa giải đố
        GameObject deadZoneObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deadZoneObj.name = "DeadZone_Hazard_Trigger";
        deadZoneObj.tag = "Dead";
        deadZoneObj.transform.SetParent(barrierRoot.transform);
        deadZoneObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        deadZoneObj.transform.localScale = new Vector3(7.0f, 2.0f, 2.5f);
        var dzCol = deadZoneObj.GetComponent<BoxCollider>();
        if (dzCol != null) dzCol.isTrigger = true;
        var dzRend = deadZoneObj.GetComponent<MeshRenderer>();
        if (dzRend != null) dzRend.enabled = false;

        return barrierRoot;
    }

    private static KitchenPuzzleGate CreatePuzzleGate(Transform parent, string name, Vector3 pos,
        Material gateMetalMat, Material lampRedMat, Material lampGreenMat)
    {
        GameObject gateRoot = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(gateRoot, "Create " + name);
        gateRoot.transform.SetParent(parent);
        gateRoot.transform.position = pos;

        // Tấm cửa sập kim loại dày chắn kín hoàn toàn ô vòm cửa từ Y=1 đến Y=9
        GameObject shutter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shutter.name = "Sliding_Security_Shutter";
        shutter.layer = 6; // Ground, ngăn chặn di chuyển xuyên qua
        shutter.transform.SetParent(gateRoot.transform);
        shutter.transform.localPosition = new Vector3(0f, 4.5f, 0f);
        shutter.transform.localScale = new Vector3(7.2f, 8.0f, 0.8f);
        if (gateMetalMat != null) shutter.GetComponent<MeshRenderer>().sharedMaterial = gateMetalMat;

        // Hai quả cầu đèn báo hiệu trạng thái gắn trên hai trụ cửa
        GameObject lampLeft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampLeft.name = "Gate_Status_Lamp_Left";
        lampLeft.transform.SetParent(gateRoot.transform);
        lampLeft.transform.localPosition = new Vector3(-3.8f, 5.0f, -0.6f);
        lampLeft.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        var colL = lampLeft.GetComponent<Collider>();
        if (colL != null) Object.DestroyImmediate(colL);
        var rendL = lampLeft.GetComponent<MeshRenderer>();
        if (lampRedMat != null) rendL.sharedMaterial = lampRedMat;

        GameObject lampRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampRight.name = "Gate_Status_Lamp_Right";
        lampRight.transform.SetParent(gateRoot.transform);
        lampRight.transform.localPosition = new Vector3(3.8f, 5.0f, -0.6f);
        lampRight.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        var colR = lampRight.GetComponent<Collider>();
        if (colR != null) Object.DestroyImmediate(colR);
        var rendR = lampRight.GetComponent<MeshRenderer>();
        if (lampRedMat != null) rendR.sharedMaterial = lampRedMat;

        var gateComp = gateRoot.AddComponent<KitchenPuzzleGate>();
        gateComp.SetupForEditor(shutter.transform, new Renderer[] { rendL, rendR }, lampRedMat, lampGreenMat);
        return gateComp;
    }

    private static Material GetOrCreateMaterial(string path, Color color, Color emissionColor, float metallic = 0f, float smoothness = 0.5f)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.color = color;
            if (emissionColor != Color.black)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            string folder = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string[] parts = folder.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
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

    private static void CreateDeadZone(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool visible = false)
    {
        GameObject dz = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(dz, "Create " + name);
        dz.name = name;
        dz.tag = "Dead";
        dz.transform.SetParent(parent);
        dz.transform.position = pos;
        dz.transform.localScale = size;
        var col = dz.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
        var r = dz.GetComponent<MeshRenderer>();
        if (r != null)
        {
            if (visible && mat != null) r.sharedMaterial = mat;
            else r.enabled = false;
        }
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
