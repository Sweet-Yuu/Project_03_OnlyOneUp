#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Hoang.Puzzles;

/// <summary>
/// Trình tạo bản đồ nhà bếp (Kitchen_Map) siêu quy mô (Mega Kitchen Map).
/// Chiều dài hơn 510m (vượt trội so với MainMap_Online) và mở rộng gấp 2.5 lần bản đồ cũ.
/// Tích hợp 6 phân khu ẩm thực chuyên biệt, vách ngăn kín 100% hai bên,
/// cổng vòm công nghiệp cỡ lớn (rộng 14m, cao 14m) đủ cho 2 chú vịt khiêng tổ trứng đi qua,
/// cột cờ checkpoint thu nhỏ gọn gàng, và trang trí nội thất 2 bên tường dày đặc, bắt mắt.
/// </summary>
public static class KitchenMapBuilder
{
    private const string KayKitBase = "Assets/_Duong/_Art/KayKit_Restaurant_Bits_1.0/Assets/fbx (unity)/";
    private const string MedievalBase = "Assets/Low Poly Medieval Environment Pack (35+ Props)/Models/";
    private const string QuangModelBase = "Assets/Quang/Model3D/";
    private const string QuangNatureBase = "Assets/Quang/Model3D/Nature_Kit/";
    private const string MatBase = "Assets/Quang/Material/";
    private const string FastMeshMatBase = "Assets/FastMesh/Materials/";

    private const string RequestPath = "Temp/KitchenMapBuilder/build_request.txt";
    private const string ResultPath = "Temp/KitchenMapBuilder/build_result.txt";

    [InitializeOnLoadMethod]
    private static void Register() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (!File.Exists(RequestPath) || EditorApplication.isCompiling ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;

        File.Delete(RequestPath);
        try
        {
            BuildMap();
            string dir = Path.GetDirectoryName(ResultPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ResultPath, "SUCCESS");
        }
        catch (System.Exception error)
        {
            string dir = Path.GetDirectoryName(ResultPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ResultPath, error.ToString());
            Debug.LogException(error);
        }
    }

    [MenuItem("Tools/Build Kitchen Map %#k")]
    public static void BuildMap()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "Kitchen_Map")
        {
            string scenePath = "Assets/Scenes/Kitchen_Map.unity";
            if (File.Exists(scenePath))
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please open Kitchen_Map scene first!", "OK");
                return;
            }
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Mega Kitchen Map");
        int undoGroup = Undo.GetCurrentGroup();

        // 1. Dọn dẹp đối tượng cũ
        GameObject existingMap = GameObject.Find("Map_Kitchen");
        if (existingMap != null) Undo.DestroyObjectImmediate(existingMap);

        GameObject oldCam = GameObject.Find("Main Camera");
        if (oldCam != null && GameObject.Find("======Camera=====") != null)
        {
            Undo.DestroyObjectImmediate(oldCam);
        }

        GameObject oldChar = GameObject.Find("Charcater");
        if (oldChar != null) Undo.DestroyObjectImmediate(oldChar);

        // 2. Chuẩn bị vật liệu thẩm mỹ chất lượng cao
        Material deadMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Dead.mat");
        Material winMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Win_Area.mat");
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Wood_Color.mat") ??
                            AssetDatabase.LoadAssetAtPath<Material>(FastMeshMatBase + "Wood.mat");
        Material dirtyRoadMat = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "DirtyRoad.mat");
        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>(FastMeshMatBase + "Gold.mat") ??
                           AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Coin.mat");

        Material fireMat = GetOrCreateMaterial("Assets/Materials/Kitchen_FireHazard.mat",
            new Color(1f, 0.25f, 0f, 1f), new Color(1f, 0.2f, 0f) * 3.5f);
        Material plateIdleMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Plate_Idle.mat",
            new Color(0.18f, 0.18f, 0.2f, 1f), new Color(1f, 0.15f, 0.05f) * 2f, 0.8f, 0.6f);
        Material plateActiveMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Plate_Active.mat",
            new Color(0.25f, 0.22f, 0.15f, 1f), new Color(1f, 0.8f, 0.1f) * 3f, 0.8f, 0.7f);
        Material plateSolvedMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Plate_Solved.mat",
            new Color(0.15f, 0.25f, 0.18f, 1f), new Color(0.1f, 1f, 0.35f) * 3.5f, 0.8f, 0.8f);

        Material icePlateIdleMat = GetOrCreateMaterial("Assets/Materials/Kitchen_IcePlate_Idle.mat",
            new Color(0.15f, 0.22f, 0.28f, 1f), new Color(0.1f, 0.4f, 0.8f) * 2f, 0.8f, 0.7f);
        Material icePlateActiveMat = GetOrCreateMaterial("Assets/Materials/Kitchen_IcePlate_Active.mat",
            new Color(0.2f, 0.35f, 0.45f, 1f), new Color(0.2f, 0.8f, 1f) * 3.5f, 0.8f, 0.8f);
        Material icePlateSolvedMat = GetOrCreateMaterial("Assets/Materials/Kitchen_IcePlate_Solved.mat",
            new Color(0.15f, 0.35f, 0.25f, 1f), new Color(0.2f, 1f, 0.6f) * 3.5f, 0.8f, 0.8f);

        Material gateMetalMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Gate_Metal.mat",
            new Color(0.75f, 0.78f, 0.82f, 1f), new Color(0.12f, 0.14f, 0.16f), 0.85f, 0.65f);
        Material cryoGateMetalMat = GetOrCreateMaterial("Assets/Materials/Kitchen_CryoGate_Metal.mat",
            new Color(0.68f, 0.84f, 0.94f, 1f), new Color(0.1f, 0.3f, 0.5f), 0.9f, 0.75f);

        Material lampRedMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Lamp_Red.mat",
            new Color(1f, 0.1f, 0.05f, 1f), new Color(1f, 0.1f, 0.05f) * 3f);
        Material lampGreenMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Lamp_Green.mat",
            new Color(0.1f, 1f, 0.3f, 1f), new Color(0.1f, 1f, 0.3f) * 3f);
        Material lampBlueMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Lamp_Blue.mat",
            new Color(0.1f, 0.5f, 1f, 1f), new Color(0.1f, 0.5f, 1f) * 3.5f);

        Material iceHazardMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Ice_Hazard.mat",
            new Color(0.4f, 0.8f, 1f, 0.85f), new Color(0.1f, 0.4f, 0.9f) * 1.5f, 0.3f, 0.95f);

        // 3. Khởi tạo cấu trúc phân cấp Root
        GameObject mapRoot = new GameObject("Map_Kitchen");
        Undo.RegisterCreatedObjectUndo(mapRoot, "Create Map_Kitchen");

        GameObject roomGroup = CreateChild(mapRoot, "Room_Architecture");
        GameObject zone1Group = CreateChild(mapRoot, "Zone1_PrepIsland");
        GameObject zone2Group = CreateChild(mapRoot, "Zone2_CooklineStoves");
        GameObject zone3Group = CreateChild(mapRoot, "Zone3_DishwashingSeesaw");
        GameObject zone4Group = CreateChild(mapRoot, "Zone4_ColdStorageVault");
        GameObject zone5Group = CreateChild(mapRoot, "Zone5_HighRisePantry");
        GameObject zone6Group = CreateChild(mapRoot, "Zone6_VIPBanquetAndPatio");
        GameObject deadZoneGroup = CreateChild(mapRoot, "DeadZones");
        GameObject lightsGroup = CreateChild(mapRoot, "Environment_Lighting");
        GameObject gameplayGroup = CreateChild(mapRoot, "Gameplay");
        GameObject coinGroup = CreateChild(mapRoot, "Collectibles");

        // =========================================================================
        // PHẦN 1: KIẾN TRÚC SÀN & TƯỜNG BAO TOÀN DIỆN (Z = -24 ĐẾN 480, X = -48 ĐẾN +48)
        // =========================================================================
        GameObject floorParent = CreateChild(roomGroup, "Kitchen_Floor_Grid");
        float tileSize = 16f;

        // Trải sàn gạch nhà bếp kích thước 96m rộng và 504m dài
        for (float x = -48f; x <= 48f; x += tileSize)
        {
            for (float z = -24f; z <= 480f; z += tileSize)
            {
                SpawnKayKit("floor_kitchen", floorParent.transform, new Vector3(x, 0f, z), Vector3.zero, new Vector3(4f, 2f, 4f), true, 6);
            }
        }

        // Tường bao quanh phòng bếp: Kết hợp cửa sổ đón nắng và ô trả món
        GameObject wallParent = CreateChild(roomGroup, "Kitchen_Perimeter_Walls");
        float wallWidth = 16f;

        // Tường trái (X = -48)
        for (float z = -24f; z <= 480f; z += wallWidth)
        {
            string wallType = "wall";
            if (Mathf.Abs(z % 64f - 16f) < 5f) wallType = "wall_window_open";
            else if (Mathf.Abs(z % 64f - 48f) < 5f) wallType = "wall_window_closed";
            SpawnKayKit(wallType, wallParent.transform, new Vector3(-48f, 1f, z), new Vector3(0f, 90f, 0f), new Vector3(4f, 8f, 4f), true);
        }

        // Tường phải (X = +48)
        for (float z = -24f; z <= 480f; z += wallWidth)
        {
            string wallType = "wall";
            if (Mathf.Abs(z % 80f - 16f) < 5f) wallType = "wall_orderwindow_decorated";
            else if (Mathf.Abs(z % 80f - 48f) < 5f) wallType = "wall_orderwindow";
            else if (Mathf.Abs(z % 80f - 64f) < 5f) wallType = "wall_window_open";
            SpawnKayKit(wallType, wallParent.transform, new Vector3(48f, 1f, z), new Vector3(0f, -90f, 0f), new Vector3(4f, 8f, 4f), true);
        }

        // Tường sau lưng bàn xuất phát (Z = -24)
        for (float x = -48f; x <= 48f; x += wallWidth)
        {
            SpawnKayKit("wall", wallParent.transform, new Vector3(x, 1f, -24f), Vector3.zero, new Vector3(4f, 8f, 4f), true);
        }

        // Tường ngăn cách giữa sảnh bếp và sân vườn ngoài trời (Z = 480)
        for (float x = -48f; x <= 48f; x += wallWidth)
        {
            if (Mathf.Abs(x) < 14f) continue;
            SpawnKayKit("wall", wallParent.transform, new Vector3(x, 1f, 480f), new Vector3(0f, 180f, 0f), new Vector3(4f, 8f, 4f), true);
        }
        SpawnKayKit("wall", wallParent.transform, new Vector3(0f, 17f, 480f), new Vector3(0f, 180f, 0f), new Vector3(4f, 4f, 4f), true);

        // =========================================================================
        // PHẦN 2: BỐ TRÍ NỘI THẤT DỌC 2 BÊN TƯỜNG LIÊN TỤC, DÀY ĐẶC & SỐNG ĐỘNG (DỌC 500M)
        // =========================================================================
        GameObject leftStations = CreateChild(roomGroup, "Left_Wall_Facilities");
        GameObject rightStations = CreateChild(roomGroup, "Right_Wall_Facilities");

        // ------------------ DỌC TƯỜNG TRÁI (X ~ -43 ĐẾN -45) ------------------
        // Cụm 1: Quầy chuẩn bị & Khu ăn uống nghỉ ngơi (Z = -16 đến 40)
        SpawnKayKit("table_round_A_decorated", leftStations.transform, new Vector3(-41f, 1f, -8f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnKayKit("chair_A", leftStations.transform, new Vector3(-45f, 1f, -8f), new Vector3(0f, 90f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("chair_B", leftStations.transform, new Vector3(-37f, 1f, -8f), new Vector3(0f, -90f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("table_round_B", leftStations.transform, new Vector3(-41f, 1f, 6f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnKayKit("chair_stool", leftStations.transform, new Vector3(-41f, 1f, 12f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("kitchencounter_straight_A_backsplash", leftStations.transform, new Vector3(-43.5f, 1f, 22f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnKayKit("kitchencounter_straight_B_backsplash", leftStations.transform, new Vector3(-43.5f, 1f, 32f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnKayKit("shelf_papertowel_decorated", leftStations.transform, new Vector3(-46f, 10f, 22f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);
        SpawnKayKit("kitchencabinet", leftStations.transform, new Vector3(-46f, 10f, 32f), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);

        // Cụm 2: Trạm sơ chế rau củ, củ quả & thớt thịt (Z = 50 đến 120)
        for (float zStation = 50f; zStation <= 110f; zStation += 15f)
        {
            SpawnKayKit("kitchencounter_straight_A_backsplash", leftStations.transform, new Vector3(-43.5f, 1f, zStation), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
            SpawnKayKit("cuttingboard", leftStations.transform, new Vector3(-43.5f, 6.2f, zStation), Vector3.zero, new Vector3(4.5f, 2f, 4.5f), true, 6);
            SpawnKayKit("kitchencabinet_half", leftStations.transform, new Vector3(-46f, 10f, zStation), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);
        }
        SpawnKayKit("crate_tomatoes", leftStations.transform, new Vector3(-39f, 1f, 55f), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);
        SpawnKayKit("crate_lettuce", leftStations.transform, new Vector3(-39f, 1f, 70f), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);
        SpawnKayKit("crate_cheese", leftStations.transform, new Vector3(-39f, 1f, 85f), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);
        SpawnKayKit("crate_steak", leftStations.transform, new Vector3(-39f, 1f, 100f), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);

        // Cụm 3: Đại trạm rửa chén bát công nghiệp (Z = 140 đến 215)
        for (float zSink = 145f; zSink <= 205f; zSink += 20f)
        {
            SpawnKayKit("kitchencounter_sink_backsplash", leftStations.transform, new Vector3(-43.5f, 1f, zSink), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
            SpawnKayKit("dishrack_plates", leftStations.transform, new Vector3(-43.5f, 6.2f, zSink), new Vector3(0f, 90f, 0f), new Vector3(4f, 4f, 4f), true, 6);
            SpawnMedieval("Bucket_of_water", leftStations.transform, new Vector3(-38f, 1f, zSink), Vector3.zero, new Vector3(3f, 3f, 3f), true, 6);
            SpawnKayKit("towelrail", leftStations.transform, new Vector3(-46f, 8f, zSink), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);
        }

        // Cụm 4: Hầm tủ lạnh công nghiệp khổng lồ (Z = 235 đến 300)
        for (float zFridge = 238f; zFridge <= 290f; zFridge += 16f)
        {
            string fType = (Mathf.RoundToInt(zFridge) % 32 == 0) ? "fridge_B" : "fridge_A_decorated";
            SpawnKayKit(fType, leftStations.transform, new Vector3(-43.5f, 1f, zFridge), new Vector3(0f, 90f, 0f), new Vector3(5.5f, 8f, 5.5f), true, 6);
            SpawnKayKit("crate_cheese", leftStations.transform, new Vector3(-38f, 1f, zFridge), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);
            SpawnKayKit("crate_cheese", leftStations.transform, new Vector3(-38f, 5f, zFridge), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);
        }

        // Cụm 5: Dãy kho bột mì, nông sản khô & thùng hàng (Z = 315 đến 410)
        for (float zStorage = 320f; zStorage <= 405f; zStorage += 16f)
        {
            SpawnMedieval("Bag_01", leftStations.transform, new Vector3(-43f, 1f, zStorage - 3f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
            SpawnMedieval("Bag_02", leftStations.transform, new Vector3(-43f, 5f, zStorage - 3f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
            SpawnMedieval("Barrel", leftStations.transform, new Vector3(-38f, 1f, zStorage + 3f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
            SpawnKayKit("kitchencabinet", leftStations.transform, new Vector3(-46f, 12f, zStorage), new Vector3(0f, 90f, 0f), new Vector3(5f, 5f, 5f), true);
        }

        // Cụm 6: Quầy chuẩn bị tiệc VIP (Z = 425 đến 470)
        for (float zBanquet = 430f; zBanquet <= 465f; zBanquet += 15f)
        {
            SpawnKayKit("kitchentable_A_large_decorated", leftStations.transform, new Vector3(-42f, 1f, zBanquet), new Vector3(0f, 90f, 0f), new Vector3(5f, 4.5f, 5f), true, 6);
            SpawnKayKit("food_dinner", leftStations.transform, new Vector3(-42f, 6.5f, zBanquet), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        }

        // ------------------ DỌC TƯỜNG PHẢI (X ~ +43 ĐẾN +45) ------------------
        // Cụm 1: Quầy trả món, menu & sốt gia vị (Z = -16 đến 40)
        SpawnKayKit("kitchencounter_straight_B_backsplash", rightStations.transform, new Vector3(43.5f, 1f, -6f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnKayKit("menu", rightStations.transform, new Vector3(43.5f, 6.2f, -6f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnKayKit("kitchentable_A_large_decorated", rightStations.transform, new Vector3(43f, 1f, 12f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
        SpawnKayKit("ketchup", rightStations.transform, new Vector3(41f, 6.5f, 12f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnKayKit("mustard", rightStations.transform, new Vector3(41f, 6.5f, 8f), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        SpawnKayKit("kitchencounter_straight_A_backsplash", rightStations.transform, new Vector3(43.5f, 1f, 26f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);

        // Cụm 2: Dãy lò nướng, bếp nấu chính & ống hút khói liên hoàn (Z = 50 đến 120)
        for (float zCook = 52f; zCook <= 112f; zCook += 16f)
        {
            SpawnKayKit("oven", rightStations.transform, new Vector3(43.5f, 1f, zCook - 4f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
            SpawnKayKit("stove_multi", rightStations.transform, new Vector3(43.5f, 1f, zCook + 4f), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
            SpawnKayKit("extractorhood", rightStations.transform, new Vector3(43.5f, 9.5f, zCook + 4f), new Vector3(0f, -90f, 0f), new Vector3(6f, 3.5f, 6f), true, 6);
            SpawnMedieval("Firewood_01", rightStations.transform, new Vector3(38f, 1f, zCook), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        }

        // Cụm 3: Quầy kệ bát đĩa & thùng chứa vệ sinh (Z = 140 đến 215)
        for (float zRack = 145f; zRack <= 205f; zRack += 20f)
        {
            SpawnKayKit("kitchencounter_straight_A", rightStations.transform, new Vector3(43.5f, 1f, zRack), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
            SpawnQuangModel("Dustabin", rightStations.transform, new Vector3(38f, 1f, zRack), Vector3.zero, new Vector3(2.5f, 2.5f, 2.5f), true, 6);
            SpawnKayKit("kitchencabinet_corner", rightStations.transform, new Vector3(46f, 10f, zRack), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true);
        }

        // Cụm 4: Hầm thịt nguội, phô mai & kệ thép trữ đông (Z = 235 đến 300)
        for (float zMeat = 240f; zMeat <= 290f; zMeat += 18f)
        {
            SpawnKayKit("crate_ham", rightStations.transform, new Vector3(43f, 1f, zMeat), Vector3.zero, new Vector3(5f, 4.5f, 5f), true, 6);
            SpawnKayKit("crate_steak", rightStations.transform, new Vector3(38f, 1f, zMeat), Vector3.zero, new Vector3(5f, 4.5f, 5f), true, 6);
            SpawnKayKit("crate_buns", rightStations.transform, new Vector3(43f, 5.5f, zMeat), Vector3.zero, new Vector3(4.5f, 3f, 4.5f), true, 6);
        }

        // Cụm 5: Dãy thùng rượu gỗ & kệ củ quả (Z = 315 đến 410)
        for (float zBarrels = 320f; zBarrels <= 405f; zBarrels += 16f)
        {
            SpawnMedieval("Barrel", rightStations.transform, new Vector3(43f, 1f, zBarrels - 3f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
            SpawnMedieval("Barrel", rightStations.transform, new Vector3(43f, 4.5f, zBarrels - 3f), Vector3.zero, new Vector3(3f, 3f, 3f), true, 6);
            SpawnMedieval("Barrel", rightStations.transform, new Vector3(38f, 1f, zBarrels + 2f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
            SpawnKayKit("crate_potatoes", rightStations.transform, new Vector3(42f, 1f, zBarrels + 6f), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);
            SpawnKayKit("crate_onions", rightStations.transform, new Vector3(42f, 5f, zBarrels + 6f), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);
        }

        // Cụm 6: Quầy bar đồ uống & rượu tiệc hoàng gia (Z = 425 đến 470)
        for (float zBar = 430f; zBar <= 465f; zBar += 15f)
        {
            SpawnKayKit("kitchencounter_straight_B_backsplash", rightStations.transform, new Vector3(43.5f, 1f, zBar), new Vector3(0f, -90f, 0f), new Vector3(5f, 5f, 5f), true, 6);
            SpawnKayKit("jar_A_large", rightStations.transform, new Vector3(43.5f, 6.2f, zBar), Vector3.zero, new Vector3(4f, 4f, 4f), true);
            SpawnKayKit("jar_B_large", rightStations.transform, new Vector3(40f, 6.2f, zBar), Vector3.zero, new Vector3(4f, 4f, 4f), true);
        }

        // =========================================================================
        // PHẦN 3: ZONE 1 - BÀN ĐẢO SƠ CHẾ & LÀM BÁNH (Z = -10 ĐẾN 60, Y = 16 XUỐNG 1)
        // =========================================================================
        GameObject islandBase = CreateChild(zone1Group, "Island_Table_Base");
        SpawnKayKit("kitchentable_A_large", islandBase.transform, new Vector3(-7f, 0f, 6f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnKayKit("kitchentable_A_large", islandBase.transform, new Vector3(7f, 0f, 6f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnKayKit("kitchentable_A_large", islandBase.transform, new Vector3(-7f, 0f, 22f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);
        SpawnKayKit("kitchentable_A_large", islandBase.transform, new Vector3(7f, 0f, 22f), Vector3.zero, new Vector3(5f, 16f, 8f), true, 6);

        GameObject tableTopFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(tableTopFloor, "Create TableTopFloor");
        tableTopFloor.name = "Island_TableTop_Surface";
        tableTopFloor.transform.SetParent(islandBase.transform);
        tableTopFloor.transform.position = new Vector3(0f, 16.02f, 14f);
        tableTopFloor.transform.localScale = new Vector3(28f, 0.2f, 32f);
        tableTopFloor.layer = 6;
        if (woodMat != null) tableTopFloor.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        GameObject islandProps = CreateChild(zone1Group, "Island_Obstacles");
        SpawnKayKit("cuttingboard", islandProps.transform, new Vector3(-6f, 16.3f, 14f), new Vector3(0f, 15f, 0f), new Vector3(6f, 2f, 6f), true, 6);
        SpawnKayKit("plate", islandProps.transform, new Vector3(6f, 16.2f, 14f), Vector3.zero, new Vector3(7f, 2f, 7f), true, 6);
        SpawnKayKit("food_burger", islandProps.transform, new Vector3(6f, 16.8f, 14f), Vector3.zero, new Vector3(6f, 6f, 6f), true);
        SpawnKayKit("knife", islandProps.transform, new Vector3(0f, 16.5f, 19f), new Vector3(0f, 90f, 90f), new Vector3(6f, 6f, 6f), true, 6);
        SpawnKayKit("ketchup", islandProps.transform, new Vector3(-4f, 16.2f, 24f), Vector3.zero, new Vector3(5f, 5f, 5f), true);
        SpawnKayKit("mustard", islandProps.transform, new Vector3(4f, 16.2f, 24f), Vector3.zero, new Vector3(5f, 5f, 5f), true);

        // Cầu trượt thớt gỗ dốc hạ cánh an toàn xuống sàn bếp
        SpawnKayKit("cuttingboard", islandProps.transform, new Vector3(0f, 13f, 32f), new Vector3(18f, 0f, 0f), new Vector3(8f, 2f, 8f), true, 6);
        SpawnKayKit("crate", islandProps.transform, new Vector3(0f, 1f, 38f), Vector3.zero, new Vector3(5f, 6.5f, 5f), true, 6);
        SpawnKayKit("crate_buns", islandProps.transform, new Vector3(0f, 1f, 44f), Vector3.zero, new Vector3(5f, 2f, 5f), true, 6);

        // --- CÂU ĐỐ ZONE 1: ĐÈN LIÊN KẾT ĐẢO SƠ CHẾ (LINKED LIGHTS 3x3) (Z = 49) ---
        GameObject linkedLightsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hoang/Prefabs/Puzzles/LinkedLightsGatePuzzle.prefab");
        if (linkedLightsPrefab != null)
        {
            GameObject puzzleInst = (GameObject)PrefabUtility.InstantiatePrefab(linkedLightsPrefab, zone1Group.transform);
            Undo.RegisterCreatedObjectUndo(puzzleInst, "Spawn Linked Lights Puzzle");
            puzzleInst.name = "Kitchen_Puzzle_PrepIsland_LinkedLights";
            puzzleInst.transform.position = new Vector3(0f, 1f, 49f);
            puzzleInst.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

            var puzzleComp = puzzleInst.GetComponentInChildren<Hoang.Puzzles.LinkedLightsPuzzle>();
            if (puzzleComp != null)
            {
                var soPuzzle = new SerializedObject(puzzleComp);
                var distProp = soPuzzle.FindProperty("_interactionDistance");
                if (distProp != null) distProp.floatValue = 14f;
                soPuzzle.ApplyModifiedProperties();
            }
            var gateComp = puzzleInst.GetComponentInChildren<Hoang.Puzzles.PuzzleGate>();
            if (gateComp != null)
            {
                var soGate = new SerializedObject(gateComp);
                var offsetProp = soGate.FindProperty("_openOffset");
                if (offsetProp != null) offsetProp.vector3Value = new Vector3(0f, 12f, 0f);
                var speedProp = soGate.FindProperty("_moveSpeed");
                if (speedProp != null) speedProp.floatValue = 5.5f;
                soGate.ApplyModifiedProperties();
            }
            ApplyGateWidening(puzzleInst);
        }

        BuildZone1InvisibleBarriers(zone1Group.transform);
        BuildZone1SideClutter(zone1Group.transform);

        // =========================================================================
        // PHẦN 4: ZONE 2 - BẾP NẤU HỰC LỬA & CỔNG KHÍ GAS CÔNG NGHIỆP (Z = 65 ĐẾN 145)
        // =========================================================================
        SpawnKayKit("chair_stool", zone2Group.transform, new Vector3(0f, 1f, 56f), Vector3.zero, new Vector3(4f, 3f, 4f), true, 6);
        SpawnKayKit("kitchentable_B", zone2Group.transform, new Vector3(-4f, 1f, 66f), Vector3.zero, new Vector3(4f, 3f, 4f), true, 6);

        // Bếp đơn và chảo lửa trung tâm (Z = 78)
        SpawnKayKit("stove_single", zone2Group.transform, new Vector3(0f, 1f, 78f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnKayKit("pan_006", zone2Group.transform, new Vector3(0f, 5.5f, 78f), Vector3.zero, new Vector3(5.5f, 3f, 5.5f), true);
        SpawnKayKit("lid_large", zone2Group.transform, new Vector3(0f, 7.0f, 78f), new Vector3(180f, 0f, 0f), new Vector3(5.5f, 1.2f, 5.5f), true, 6);
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_StoveFire_1", new Vector3(0f, 5.8f, 78f), new Vector3(4f, 1.5f, 4f), fireMat, false);

        SpawnKayKit("dishrack", zone2Group.transform, new Vector3(3f, 1f, 92f), Vector3.zero, new Vector3(5f, 3f, 5f), true, 6);
        SpawnKayKit("stove_multi", zone2Group.transform, new Vector3(-1f, 1f, 105f), Vector3.zero, new Vector3(4.5f, 4f, 4.5f), true, 6);
        SpawnKayKit("pot_large", zone2Group.transform, new Vector3(-1f, 5.8f, 105f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_StoveFire_2", new Vector3(-1f, 6.2f, 105f), new Vector3(3.5f, 1.5f, 3.5f), fireMat, false);

        // --- CÂU ĐỐ 1: BÀN ĐẠP ÁP SUẤT NHIỆT KHÍ GAS (Z = 120 ĐẾN 134) ---
        GameObject puzzle1Root = CreateChild(gameplayGroup, "Kitchen_Puzzle1_CooklineGas");
        var puzzle1Ctrl = puzzle1Root.AddComponent<KitchenCoopPuzzleController>();

        KitchenPressurePlate plate1Left = CreatePressurePlate(puzzle1Root.transform, "Plate_Cookline_Left",
            new Vector3(-2.8f, 1.0f, 120f), plateIdleMat, plateActiveMat, plateSolvedMat);
        KitchenPressurePlate plate1Right = CreatePressurePlate(puzzle1Root.transform, "Plate_Cookline_Right",
            new Vector3(2.8f, 1.0f, 120f), plateIdleMat, plateActiveMat, plateSolvedMat);

        GameObject fireBarrier1 = CreateHazardFireBarrier(puzzle1Root.transform, "Hazard_Fire_Cookline",
            new Vector3(0f, 1.0f, 126f), fireMat, deadMat, 12f);

        // VÁCH NGĂN PHÂN KHU BỊT KÍN 100% (X = -48 ĐẾN +48) VỚI CỔNG VÒM KHỔNG LỒ (Z = 133)
        GameObject archway1 = CreateChild(zone2Group, "Cookline_Grand_Archway");
        KitchenPuzzleGate gate1 = BuildGrandPartitionWallWithGate(archway1.transform, 133f, "Cookline_Gate",
            gateMetalMat, lampRedMat, lampGreenMat, woodMat);
        puzzle1Ctrl.SetupForEditor(new KitchenPressurePlate[] { plate1Left, plate1Right }, gate1, fireBarrier1);

        // =========================================================================
        // PHẦN 5: ZONE 3 - BỒN RỬA CHÉN CÔNG NGHIỆP & CẦU BẬP BÊNH (Z = 145 ĐẾN 225)
        // =========================================================================
        SpawnKayKit("kitchentable_sink_large", zone3Group.transform, new Vector3(-4f, 1f, 150f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnKayKit("dishrack_plates", zone3Group.transform, new Vector3(-4f, 5.5f, 150f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnKayKit("kitchentable_B_large", zone3Group.transform, new Vector3(4f, 1f, 162f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnKayKit("plate", zone3Group.transform, new Vector3(4f, 5.5f, 162f), Vector3.zero, new Vector3(6f, 2f, 6f), true, 6);

        // Bồn rửa trơn trượt có hố nước thải (DeadZone rớt nước xà phòng)
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_DishSoap_Pit", new Vector3(0f, 0.5f, 188f), new Vector3(20f, 2f, 30f), null, false);

        // --- CÂU ĐỐ 2 / CHƯỚNG NGẠI VẬT: CẦU BẬP BÊNH THĂNG BẰNG CO-OP (Z = 188) ---
        GameObject seesawPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hoang/Prefabs/Puzzles/SeesawBridgePuzzle.prefab");
        if (seesawPrefab != null)
        {
            GameObject seesawInst = (GameObject)PrefabUtility.InstantiatePrefab(seesawPrefab, zone3Group.transform);
            Undo.RegisterCreatedObjectUndo(seesawInst, "Spawn Seesaw Bridge");
            seesawInst.name = "Seesaw_Dishwashing_Plank";
            seesawInst.transform.position = new Vector3(0f, 3.8f, 188f);
            seesawInst.transform.localScale = new Vector3(1.6f, 1.2f, 1.2f);
        }

        SpawnKayKit("dishrack", zone3Group.transform, new Vector3(0f, 1f, 206f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);

        // VÁCH NGĂN PHÂN KHU BỊT KÍN 100% CỬA SẬP BỒN RỬA (Z = 226)
        GameObject archway2 = CreateChild(zone3Group, "Dishwashing_Grand_Archway");
        BuildGrandPartitionWall(archway2.transform, 226f, woodMat);

        // =========================================================================
        // PHẦN 6: ZONE 4 - KHO LẠNH KHỔNG LỒ & CÂU ĐỐ CRYO VAULT (Z = 228 ĐẾN 310)
        // =========================================================================
        SpawnKayKit("fridge_A_decorated", zone4Group.transform, new Vector3(-6f, 1f, 240f), Vector3.zero, new Vector3(5.5f, 8f, 5.5f), true, 6);
        SpawnKayKit("fridge_B", zone4Group.transform, new Vector3(6f, 1f, 248f), Vector3.zero, new Vector3(5.5f, 8f, 5.5f), true, 6);
        SpawnKayKit("crate_cheese", zone4Group.transform, new Vector3(-2f, 1f, 258f), Vector3.zero, new Vector3(5f, 4.5f, 5f), true, 6);
        SpawnKayKit("crate_steak", zone4Group.transform, new Vector3(2f, 1f, 268f), Vector3.zero, new Vector3(5f, 5f, 5f), true, 6);

        GameObject iceLedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(iceLedge, "Create Ice Ledge");
        iceLedge.name = "Ice_Platform_Hazard";
        iceLedge.transform.SetParent(zone4Group.transform);
        iceLedge.transform.position = new Vector3(0f, 4f, 276f);
        iceLedge.transform.localScale = new Vector3(8f, 0.5f, 10f);
        iceLedge.layer = 6;
        if (iceHazardMat != null) iceLedge.GetComponent<MeshRenderer>().sharedMaterial = iceHazardMat;

        // --- CÂU ĐỐ 3: CỬA BẢO MẬT HẦM ĐÔNG CRYO LOCK (Z = 286 ĐẾN 300) ---
        GameObject puzzle3Root = CreateChild(gameplayGroup, "Kitchen_Puzzle3_CryoVaultLock");
        var puzzle3Ctrl = puzzle3Root.AddComponent<KitchenCoopPuzzleController>();

        KitchenPressurePlate plate3Left = CreatePressurePlate(puzzle3Root.transform, "Plate_Cryo_Left",
            new Vector3(-3.2f, 1.0f, 286f), icePlateIdleMat, icePlateActiveMat, icePlateSolvedMat);
        KitchenPressurePlate plate3Right = CreatePressurePlate(puzzle3Root.transform, "Plate_Cryo_Right",
            new Vector3(3.2f, 1.0f, 286f), icePlateIdleMat, icePlateActiveMat, icePlateSolvedMat);

        GameObject cryoHazardBarrier = CreateHazardFireBarrier(puzzle3Root.transform, "Cryo_Frost_Barrier",
            new Vector3(0f, 1.0f, 292f), iceHazardMat, deadMat, 12f);

        // VÁCH NGĂN PHÂN KHU BỊT KÍN 100% CỬA SẬP CRYO (Z = 300)
        GameObject archway3 = CreateChild(zone4Group, "Cryo_Vault_Grand_Archway");
        KitchenPuzzleGate gate3 = BuildGrandPartitionWallWithGate(archway3.transform, 300f, "Cryo_Gate",
            cryoGateMetalMat, lampRedMat, lampBlueMat, woodMat);
        puzzle3Ctrl.SetupForEditor(new KitchenPressurePlate[] { plate3Left, plate3Right }, gate3, cryoHazardBarrier);

        // =========================================================================
        // PHẦN 7: ZONE 5 - KHO KHÔ CAO TẦNG & THÁP NÔNG SẢN (Z = 310 ĐẾN 420, Y = 1 ĐẾN 25)
        // =========================================================================
        SpawnMedieval("Bag_01", zone5Group.transform, new Vector3(-3f, 1f, 318f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnMedieval("Bag_02", zone5Group.transform, new Vector3(-3f, 4.5f, 318f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", zone5Group.transform, new Vector3(3f, 1f, 328f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnMedieval("Barrel", zone5Group.transform, new Vector3(3f, 5f, 328f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);

        SpawnKayKit("kitchentable_B_large", zone5Group.transform, new Vector3(0f, 1f, 340f), Vector3.zero, new Vector3(6f, 10f, 6f), true, 6);
        SpawnKayKit("crate_potatoes", zone5Group.transform, new Vector3(-3f, 10f, 348f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);
        SpawnKayKit("crate_carrots", zone5Group.transform, new Vector3(3f, 12f, 358f), Vector3.zero, new Vector3(5f, 4f, 5f), true, 6);

        SpawnMedieval("Ladder", zone5Group.transform, new Vector3(-2f, 14f, 368f), new Vector3(15f, 0f, 0f), new Vector3(3f, 4f, 3f), true, 6);
        SpawnKayKit("jar_A_large", zone5Group.transform, new Vector3(0f, 16.5f, 376f), Vector3.zero, new Vector3(5f, 5f, 5f), true, 6);
        SpawnKayKit("jar_B_large", zone5Group.transform, new Vector3(3f, 18.5f, 386f), Vector3.zero, new Vector3(5f, 5f, 5f), true, 6);

        // Cầu xà ngang đi trên cao kết nối qua khu sảnh tiệc (Y = 21.5m, Z = 395 đến 415)
        GameObject catwalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(catwalk, "Create High Catwalk");
        catwalk.name = "Pantry_High_Catwalk";
        catwalk.transform.SetParent(zone5Group.transform);
        catwalk.transform.position = new Vector3(0f, 21.5f, 405f);
        catwalk.transform.localScale = new Vector3(8f, 0.4f, 28f);
        catwalk.layer = 6;
        if (woodMat != null) catwalk.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        // --- CÂU ĐỐ ZONE 5: MẬT MÃ BIỂU TƯỢNG KHO GIA VỊ (SYMBOL CIPHER) (Z = 412, Y = 21.7) ---
        GameObject cipherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hoang/Prefabs/Puzzles/SymbolCipherGatePuzzle.prefab");
        if (cipherPrefab != null)
        {
            GameObject puzzleInst = (GameObject)PrefabUtility.InstantiatePrefab(cipherPrefab, zone5Group.transform);
            Undo.RegisterCreatedObjectUndo(puzzleInst, "Spawn Symbol Cipher Puzzle");
            puzzleInst.name = "Kitchen_Puzzle_Pantry_SymbolCipher";
            puzzleInst.transform.position = new Vector3(0f, 21.7f, 412f);
            puzzleInst.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

            var puzzleComp = puzzleInst.GetComponentInChildren<Hoang.Puzzles.SymbolCipherPuzzle>();
            if (puzzleComp != null)
            {
                var soPuzzle = new SerializedObject(puzzleComp);
                var distProp = soPuzzle.FindProperty("_interactionDistance");
                if (distProp != null) distProp.floatValue = 14f;
                soPuzzle.ApplyModifiedProperties();
            }
            var gateComp = puzzleInst.GetComponentInChildren<Hoang.Puzzles.PuzzleGate>();
            if (gateComp != null)
            {
                var soGate = new SerializedObject(gateComp);
                var offsetProp = soGate.FindProperty("_openOffset");
                if (offsetProp != null) offsetProp.vector3Value = new Vector3(0f, 10f, 0f);
                var speedProp = soGate.FindProperty("_moveSpeed");
                if (speedProp != null) speedProp.floatValue = 5.5f;
                soGate.ApplyModifiedProperties();
            }
        }

        // =========================================================================
        // PHẦN 8: ZONE 6 - SẢNH TIỆC VIP & SÂN VƯỜN NGOÀI TRỜI (WIN AREA) (Z = 420 ĐẾN 510)
        // =========================================================================
        GameObject vipRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(vipRamp, "Create VIP Ramp");
        vipRamp.name = "VIP_Descent_Ramp";
        vipRamp.transform.SetParent(zone6Group.transform);
        vipRamp.transform.position = new Vector3(0f, 10.5f, 428f);
        vipRamp.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
        vipRamp.transform.localScale = new Vector3(6f, 0.3f, 24f);
        vipRamp.layer = 6;
        if (woodMat != null) vipRamp.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        SpawnKayKit("kitchentable_A_large_decorated", zone6Group.transform, new Vector3(0f, 1f, 452f), Vector3.zero, new Vector3(6f, 3.5f, 6f), true, 6);
        SpawnKayKit("food_burger", zone6Group.transform, new Vector3(-1.8f, 4.8f, 452f), Vector3.zero, new Vector3(4.5f, 4.5f, 4.5f), true);
        SpawnKayKit("food_dinner", zone6Group.transform, new Vector3(1.8f, 4.8f, 452f), Vector3.zero, new Vector3(4.5f, 4.5f, 4.5f), true);

        // Khung cửa lớn dẫn ra khu vườn ngoài trời (Z = 480) - Cổng vòm rộng mở thông thoáng, không còn bị chặn đen
        GameObject exitDoorFrame = CreateChild(zone6Group, "Exit_Grand_Doorway");
        SpawnKayKit("pillar_A", exitDoorFrame.transform, new Vector3(-9.5f, 1f, 480f), Vector3.zero, new Vector3(4f, 8f, 4f), true, 6);
        SpawnKayKit("pillar_A", exitDoorFrame.transform, new Vector3(9.5f, 1f, 480f), Vector3.zero, new Vector3(4f, 8f, 4f), true, 6);

        GameObject exitLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(exitLintel, "Create Exit Archway Lintel");
        exitLintel.name = "Exit_Archway_Lintel";
        exitLintel.layer = 6;
        exitLintel.transform.SetParent(exitDoorFrame.transform);
        exitLintel.transform.position = new Vector3(0f, 15f, 480f);
        exitLintel.transform.localScale = new Vector3(20f, 3f, 1.5f);
        if (woodMat != null) exitLintel.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        GameObject exitLight = new GameObject("Exit_Passage_Light");
        exitLight.transform.SetParent(exitDoorFrame.transform);
        exitLight.transform.position = new Vector3(0f, 13f, 480f);
        Light exitLightComp = exitLight.AddComponent<Light>();
        exitLightComp.type = LightType.Point;
        exitLightComp.range = 30f;
        exitLightComp.intensity = 2.5f;
        exitLightComp.color = new Color(1f, 0.95f, 0.8f);

        GameObject thresholdBridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(thresholdBridge, "Create Threshold Bridge");
        thresholdBridge.name = "Door_Threshold_Bridge";
        thresholdBridge.transform.SetParent(zone6Group.transform);
        thresholdBridge.transform.position = new Vector3(0f, 0.5f, 482f);
        thresholdBridge.transform.localScale = new Vector3(16f, 1f, 8f);
        thresholdBridge.layer = 6;
        if (woodMat != null) thresholdBridge.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        // SÂN VƯỜN NGOÀI TRỜI (OUTDOOR SCENIC GARDEN PATIO - Z = 485 ĐẾN 515)
        GameObject gardenGroup = CreateChild(zone6Group, "Outdoor_Garden_Patio");
        GameObject greenPlateau = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(greenPlateau, "Create Green Plateau");
        greenPlateau.name = "Outdoor_GreenHill";
        greenPlateau.layer = 6;
        greenPlateau.transform.SetParent(gardenGroup.transform);
        greenPlateau.transform.position = new Vector3(0f, -0.5f, 500f);
        greenPlateau.transform.localScale = new Vector3(70f, 3f, 45f);
        if (dirtyRoadMat != null) greenPlateau.GetComponent<MeshRenderer>().sharedMaterial = dirtyRoadMat;

        SpawnMedieval("Stone_road", gardenGroup.transform, new Vector3(0f, 1.05f, 492f), Vector3.zero, new Vector3(3f, 1f, 3f), true, 6);
        SpawnMedieval("Stone_road", gardenGroup.transform, new Vector3(0f, 1.05f, 502f), Vector3.zero, new Vector3(3f, 1f, 3f), true, 6);

        SpawnMedieval("Fence", gardenGroup.transform, new Vector3(-12f, 1f, 495f), new Vector3(0f, 90f, 0f), new Vector3(3f, 3f, 3f), true, 6);
        SpawnMedieval("Fence", gardenGroup.transform, new Vector3(12f, 1f, 495f), new Vector3(0f, 90f, 0f), new Vector3(3f, 3f, 3f), true, 6);
        SpawnQuangModel("BigRock2", gardenGroup.transform, new Vector3(-16f, 1f, 502f), Vector3.zero, new Vector3(4f, 4f, 4f), true, 6);
        SpawnQuangModel("BigRock2", gardenGroup.transform, new Vector3(16f, 1f, 505f), Vector3.zero, new Vector3(4.5f, 4.5f, 4.5f), true, 6);
        SpawnQuangNature("tree_pineDefaultA", gardenGroup.transform, new Vector3(-20f, 1f, 508f), Vector3.zero, new Vector3(3f, 3f, 3f), true, 6);
        SpawnQuangNature("tree_pineDefaultA", gardenGroup.transform, new Vector3(20f, 1f, 510f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);

        // ĐỐI TƯỢNG CHIẾN THẮNG (WIN AREA - TẠI Z = 504M)
        GameObject winZone = new GameObject("Win_Game_Zone");
        Undo.RegisterCreatedObjectUndo(winZone, "Create WinZone");
        winZone.transform.SetParent(gardenGroup.transform);
        winZone.transform.position = new Vector3(0f, 1.0f, 504f);
        winZone.tag = "Win_Area";

        BoxCollider winTriggerCol = winZone.AddComponent<BoxCollider>();
        winTriggerCol.isTrigger = true;
        winTriggerCol.center = new Vector3(0f, 2.5f, 0f);
        winTriggerCol.size = new Vector3(14f, 5f, 14f);
        winZone.AddComponent<KitchenWinZoneTrigger>();

        // Bục đài chiến thắng (Podium & Victory Rings)
        GameObject baseDais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDais.name = "Podium_Base";
        baseDais.transform.SetParent(winZone.transform, false);
        baseDais.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        baseDais.transform.localScale = new Vector3(14f, 0.25f, 14f);
        if (dirtyRoadMat != null) baseDais.GetComponent<MeshRenderer>().sharedMaterial = dirtyRoadMat;

        GameObject victoryRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        victoryRing.name = "Victory_Ring";
        victoryRing.transform.SetParent(winZone.transform, false);
        victoryRing.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        victoryRing.transform.localScale = new Vector3(11f, 0.05f, 11f);
        if (winMat != null) victoryRing.GetComponent<MeshRenderer>().sharedMaterial = winMat;

        GameObject innerCrest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerCrest.name = "Inner_Crest";
        innerCrest.transform.SetParent(winZone.transform, false);
        innerCrest.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        innerCrest.transform.localScale = new Vector3(6f, 0.1f, 6f);
        if (winMat != null) innerCrest.GetComponent<MeshRenderer>().sharedMaterial = winMat;

        GameObject flagModel = AssetDatabase.LoadAssetAtPath<GameObject>(QuangModelBase + "Checkpoint_Flag3.fbx");
        if (flagModel != null)
        {
            GameObject flagL = (GameObject)PrefabUtility.InstantiatePrefab(flagModel, winZone.transform);
            flagL.name = "Victory_Flag_L";
            flagL.transform.localPosition = new Vector3(-6.2f, 0.25f, 0f);
            flagL.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            flagL.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            GameObject flagR = (GameObject)PrefabUtility.InstantiatePrefab(flagModel, winZone.transform);
            flagR.name = "Victory_Flag_R";
            flagR.transform.localPosition = new Vector3(6.2f, 0.25f, 0f);
            flagR.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
            flagR.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        }

        GameObject bonfireObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Medieval Environment Pack (35+ Props)/Prefabs/Bonfire.prefab");
        if (bonfireObj != null)
        {
            GameObject fireL = (GameObject)PrefabUtility.InstantiatePrefab(bonfireObj, winZone.transform);
            fireL.name = "Victory_Bonfire_L";
            fireL.transform.localPosition = new Vector3(-7f, 0.1f, -4.5f);
            fireL.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

            GameObject fireR = (GameObject)PrefabUtility.InstantiatePrefab(bonfireObj, winZone.transform);
            fireR.name = "Victory_Bonfire_R";
            fireR.transform.localPosition = new Vector3(7f, 0.1f, -4.5f);
            fireR.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
        }

        SpawnMedieval("Fence", gardenGroup.transform, new Vector3(-10f, 1f, 504f), Vector3.zero, new Vector3(3f, 4f, 3f), true, 6);
        SpawnMedieval("Fence", gardenGroup.transform, new Vector3(10f, 1f, 504f), Vector3.zero, new Vector3(3f, 4f, 3f), true, 6);

        // =========================================================================
        // PHẦN 9: DEAD ZONES TOÀN BẢN ĐỒ
        // =========================================================================
        CreateDeadZone(deadZoneGroup.transform, "DeadZone_GlobalFall",
            new Vector3(0f, -12f, 240f), new Vector3(200f, 4f, 580f), deadMat, false);

        // =========================================================================
        // PHẦN 10: CHECKPOINTS THU NHỎ TỶ LỆ THEO NHÂN VẬT (0.2x) TẠI 6 MỐC TRỌNG YẾU
        // =========================================================================
        GameObject flagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuangModelBase + "Checkpoint_Flag3.fbx");
        if (flagPrefab != null)
        {
            CreateCompactCheckpoint(gameplayGroup.transform, flagPrefab, 1, new Vector3(0f, 1f, 55f), "Checkpoint_1_PrepIslandExit");
            CreateCompactCheckpoint(gameplayGroup.transform, flagPrefab, 2, new Vector3(0f, 1f, 138f), "Checkpoint_2_CooklineExit");
            CreateCompactCheckpoint(gameplayGroup.transform, flagPrefab, 3, new Vector3(0f, 1f, 230f), "Checkpoint_3_DishwashingExit");
            CreateCompactCheckpoint(gameplayGroup.transform, flagPrefab, 4, new Vector3(0f, 1f, 308f), "Checkpoint_4_ColdStorageExit");
            CreateCompactCheckpoint(gameplayGroup.transform, flagPrefab, 5, new Vector3(0f, 22f, 398f), "Checkpoint_5_PantryCatwalk");
            CreateCompactCheckpoint(gameplayGroup.transform, flagPrefab, 6, new Vector3(0f, 1f, 465f), "Checkpoint_6_VIPPatioApproach");
        }

        // =========================================================================
        // PHẦN 11: TIỀN VÀNG THU THẬP (COLLECTIBLE GOLD COINS)
        // =========================================================================
        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuangModelBase + "Gold_Coin_Unity.fbx");
        if (coinPrefab != null)
        {
            Vector3[] coinPositions = new Vector3[]
            {
                new Vector3(-6f, 17.5f, 14f),
                new Vector3(6f, 17.5f, 14f),
                new Vector3(0f, 15f, 32f),
                new Vector3(0f, 7.5f, 78f),
                new Vector3(-1f, 7.5f, 105f),
                new Vector3(0f, 2.5f, 126f),
                new Vector3(-4f, 6.5f, 150f),
                new Vector3(0f, 5.5f, 188f),
                new Vector3(0f, 2.5f, 216f),
                new Vector3(-6f, 9.5f, 240f),
                new Vector3(0f, 5.5f, 276f),
                new Vector3(0f, 2.5f, 292f),
                new Vector3(-3f, 6f, 318f),
                new Vector3(0f, 11.5f, 340f),
                new Vector3(3f, 13.5f, 358f),
                new Vector3(0f, 18f, 376f),
                new Vector3(0f, 23f, 405f),
                new Vector3(0f, 12f, 428f),
                new Vector3(0f, 6f, 452f),
                new Vector3(0f, 2f, 498f)
            };

            for (int i = 0; i < coinPositions.Length; i++)
            {
                CreateCoin(coinGroup.transform, coinPrefab, coinPositions[i], i + 1, goldMat);
            }
        }

        // =========================================================================
        // PHẦN 12: GAMEPLAY: SPAWN CENTER & NEST SETUP
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

                var brokenEggAsset = AssetDatabase.LoadAssetAtPath<GameObject>(QuangModelBase + "Broken_egg2.fbx");
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
        // PHẦN 13: ÁNH SÁNG MÔI TRƯỜNG & CHIẾU SÁNG PHÂN VÙNG (ENVIRONMENT LIGHTING)
        // =========================================================================
        GameObject sunObj = GameObject.Find("Directional Light");
        if (sunObj != null)
        {
            sunObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var sunLight = sunObj.GetComponent<Light>();
            if (sunLight != null)
            {
                sunLight.color = new Color(1f, 0.96f, 0.9f);
                sunLight.intensity = 1.3f;
                sunLight.shadows = LightShadows.Soft;
            }
        }

        CreatePointLight(lightsGroup.transform, "Light_Zone1_PrepIsland", new Vector3(0f, 26f, 15f), new Color(1f, 0.92f, 0.8f), 3.0f, 40f);
        CreatePointLight(lightsGroup.transform, "Light_Zone2_StoveFire", new Vector3(0f, 8f, 85f), new Color(1f, 0.4f, 0.1f), 5.0f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_Zone2_Puzzle1", new Vector3(0f, 12f, 126f), new Color(1f, 0.7f, 0.3f), 4.5f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_Zone3_Dishwashing", new Vector3(0f, 12f, 185f), new Color(0.85f, 0.95f, 1f), 3.5f, 40f);
        CreatePointLight(lightsGroup.transform, "Light_Zone4_ColdVault", new Vector3(0f, 14f, 265f), new Color(0.2f, 0.6f, 1f), 5.0f, 45f);
        CreatePointLight(lightsGroup.transform, "Light_Zone4_Puzzle3", new Vector3(0f, 12f, 292f), new Color(0.3f, 0.8f, 1f), 5.0f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_Zone5_PantryLower", new Vector3(0f, 12f, 345f), new Color(1f, 0.88f, 0.7f), 3.5f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_Zone5_PantryUpper", new Vector3(0f, 28f, 395f), new Color(1f, 0.92f, 0.75f), 4.5f, 45f);
        CreatePointLight(lightsGroup.transform, "Light_Zone6_VIPBanquet", new Vector3(0f, 10f, 455f), new Color(1f, 0.85f, 0.5f), 4.0f, 35f);
        CreatePointLight(lightsGroup.transform, "Light_Zone6_ExitPatio", new Vector3(0f, 12f, 502f), new Color(1f, 0.95f, 0.85f), 4.5f, 35f);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("<color=green><b>Mega Kitchen_Map with full-width walls and rich wall facilities built successfully!</b></color>");
    }

    /// <summary>
    /// Xây dựng vách ngăn phân khu bịt kín 100% từ X = -48 đến +48,
    /// tích hợp Cổng vòm công nghiệp cỡ lớn (rộng 14m, cao 14m) và cửa sập giải đố trượt lên cao.
    /// </summary>
    private static KitchenPuzzleGate BuildGrandPartitionWallWithGate(Transform parent, float zPos, string gateName,
        Material gateMetalMat, Material lampRedMat, Material lampGreenMat, Material beamMat)
    {
        // 1. Vách tường trái bịt kín từ X = -48 đến X = -9.5 (bề ngang 38.5m)
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "Partition_Wall_Left";
        leftWall.layer = 6;
        leftWall.transform.SetParent(parent);
        leftWall.transform.position = new Vector3(-28.75f, 8f, zPos);
        leftWall.transform.localScale = new Vector3(38.5f, 16f, 1.2f);
        ApplyWallTexture(leftWall);

        // 2. Vách tường phải bịt kín từ X = +9.5 đến X = +48 (bề ngang 38.5m)
        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "Partition_Wall_Right";
        rightWall.layer = 6;
        rightWall.transform.SetParent(parent);
        rightWall.transform.position = new Vector3(28.75f, 8f, zPos);
        rightWall.transform.localScale = new Vector3(38.5f, 16f, 1.2f);
        ApplyWallTexture(rightWall);

        // 3. Trụ cổng kiến trúc chịu lực hai bên vòm cửa mở rộng lên 19m (X = -9.5 và X = +9.5)
        SpawnKayKit("pillar_A", parent, new Vector3(-9.5f, 1f, zPos), Vector3.zero, new Vector3(4f, 8f, 4f), true, 6);
        SpawnKayKit("pillar_A", parent, new Vector3(9.5f, 1f, zPos), Vector3.zero, new Vector3(4f, 8f, 4f), true, 6);

        // 4. Xà ngang vòm cửa trên cao mở rộng lên 20m
        GameObject topLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topLintel.name = "Archway_Top_Lintel";
        topLintel.layer = 6;
        topLintel.transform.SetParent(parent);
        topLintel.transform.position = new Vector3(0f, 15f, zPos);
        topLintel.transform.localScale = new Vector3(20f, 3f, 1.5f);
        if (beamMat != null) topLintel.GetComponent<MeshRenderer>().sharedMaterial = beamMat;

        // Đèn chiếu sáng thông lộ cổng vòm, đảm bảo lối đi sáng sủa, không bị tối đen
        GameObject archLight = new GameObject("Archway_Passage_Light");
        archLight.transform.SetParent(parent);
        archLight.transform.position = new Vector3(0f, 13f, zPos);
        Light lightComp = archLight.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.range = 25f;
        lightComp.intensity = 2f;
        lightComp.color = gateName.Contains("Cryo") ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.9f, 0.7f);

        // 5. Cánh cửa sập cơ quan cỡ lớn (Sliding Security Shutter) rộng 13.5m, cao 13.5m
        GameObject gateRoot = new GameObject(gateName);
        gateRoot.transform.SetParent(parent);
        gateRoot.transform.position = new Vector3(0f, 1f, zPos);

        GameObject shutter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shutter.name = "Sliding_Security_Shutter";
        shutter.layer = 6;
        shutter.transform.SetParent(gateRoot.transform);
        shutter.transform.localPosition = new Vector3(0f, 6.8f, 0f);
        shutter.transform.localScale = new Vector3(17.5f, 13.6f, 0.8f);
        if (gateMetalMat != null) shutter.GetComponent<MeshRenderer>().sharedMaterial = gateMetalMat;

        // Gân nan thép ngang (Horizontal Security Slats) tạo điểm nhấn thẩm mỹ cho cửa cuốn an ninh công nghiệp
        Material slatMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Gate_Slat.mat",
            new Color(0.5f, 0.54f, 0.58f, 1f), Color.black, 0.9f, 0.6f);
        for (int i = 0; i < 4; i++)
        {
            GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slat.name = "Shutter_Slat_" + i;
            slat.transform.SetParent(shutter.transform);
            slat.transform.localPosition = new Vector3(0f, -0.35f + i * 0.23f, -0.55f);
            slat.transform.localScale = new Vector3(0.96f, 0.08f, 0.2f);
            var slatCol = slat.GetComponent<Collider>();
            if (slatCol != null) Object.DestroyImmediate(slatCol);
            var slatRend = slat.GetComponent<MeshRenderer>();
            if (slatMat != null) slatRend.sharedMaterial = slatMat;
        }

        // 6. Hai quả cầu đèn báo hiệu trạng thái gắn trên hai trụ cổng
        GameObject lampLeft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampLeft.name = "Gate_Status_Lamp_Left";
        lampLeft.transform.SetParent(gateRoot.transform);
        lampLeft.transform.localPosition = new Vector3(-9.0f, 8f, -0.6f);
        lampLeft.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        var colL = lampLeft.GetComponent<Collider>();
        if (colL != null) Object.DestroyImmediate(colL);
        var rendL = lampLeft.GetComponent<MeshRenderer>();
        if (lampRedMat != null) rendL.sharedMaterial = lampRedMat;

        GameObject lampRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampRight.name = "Gate_Status_Lamp_Right";
        lampRight.transform.SetParent(gateRoot.transform);
        lampRight.transform.localPosition = new Vector3(9.0f, 8f, -0.6f);
        lampRight.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        var colR = lampRight.GetComponent<Collider>();
        if (colR != null) Object.DestroyImmediate(colR);
        var rendR = lampRight.GetComponent<MeshRenderer>();
        if (lampRedMat != null) rendR.sharedMaterial = lampRedMat;

        var gateComp = gateRoot.AddComponent<KitchenPuzzleGate>();
        // Nâng cửa lên cao 16 mét để lộ trọn vẹn vòm thông thủy 13m
        var soGate = new SerializedObject(gateComp);
        soGate.FindProperty("_openOffset").vector3Value = new Vector3(0f, 16f, 0f);
        soGate.FindProperty("_openSpeed").floatValue = 5.5f;
        soGate.ApplyModifiedProperties();

        gateComp.SetupForEditor(shutter.transform, new Renderer[] { rendL, rendR }, lampRedMat, lampGreenMat);
        return gateComp;
    }

    /// <summary>
    /// Xây dựng vách ngăn thông phòng mở (không cửa sập) kín 100% hai bên với vòm thông thủy 14m x 14m.
    /// </summary>
    private static void BuildGrandPartitionWall(Transform parent, float zPos, Material beamMat)
    {
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "Partition_Wall_Left";
        leftWall.layer = 6;
        leftWall.transform.SetParent(parent);
        leftWall.transform.position = new Vector3(-28.75f, 8f, zPos);
        leftWall.transform.localScale = new Vector3(38.5f, 16f, 1.2f);
        ApplyWallTexture(leftWall);

        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "Partition_Wall_Right";
        rightWall.layer = 6;
        rightWall.transform.SetParent(parent);
        rightWall.transform.position = new Vector3(28.75f, 8f, zPos);
        rightWall.transform.localScale = new Vector3(38.5f, 16f, 1.2f);
        ApplyWallTexture(rightWall);

        SpawnKayKit("pillar_A", parent, new Vector3(-9.5f, 1f, zPos), Vector3.zero, new Vector3(4f, 8f, 4f), true, 6);
        SpawnKayKit("pillar_A", parent, new Vector3(9.5f, 1f, zPos), Vector3.zero, new Vector3(4f, 8f, 4f), true, 6);

        GameObject topLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topLintel.name = "Archway_Top_Lintel";
        topLintel.layer = 6;
        topLintel.transform.SetParent(parent);
        topLintel.transform.position = new Vector3(0f, 15f, zPos);
        topLintel.transform.localScale = new Vector3(20f, 3f, 1.5f);
        if (beamMat != null) topLintel.GetComponent<MeshRenderer>().sharedMaterial = beamMat;
    }

    private static void ApplyWallTexture(GameObject wallObj)
    {
        var rend = wallObj.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kitchen_Wall_Mint.mat");
            if (wallMat == null)
            {
                wallMat = GetOrCreateMaterial("Assets/Materials/Kitchen_Wall_Mint.mat",
                    new Color(0.25f, 0.65f, 0.58f, 1f), Color.black, 0f, 0.3f);
            }
            rend.sharedMaterial = wallMat;
        }
    }

    private static KitchenPressurePlate CreatePressurePlate(Transform parent, string name, Vector3 pos,
        Material idleMat, Material activeMat, Material solvedMat)
    {
        GameObject plateRoot = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(plateRoot, "Create " + name);
        plateRoot.transform.SetParent(parent);
        plateRoot.transform.position = pos;

        GameObject baseRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseRim.name = "Plate_Base_Rim";
        baseRim.transform.SetParent(plateRoot.transform);
        baseRim.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        baseRim.transform.localScale = new Vector3(3.2f, 0.05f, 3.2f);
        baseRim.layer = 6;
        if (idleMat != null) baseRim.GetComponent<MeshRenderer>().sharedMaterial = idleMat;

        GameObject movingPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        movingPad.name = "Plate_Moving_Pad";
        movingPad.transform.SetParent(plateRoot.transform);
        movingPad.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        movingPad.transform.localScale = new Vector3(2.6f, 0.08f, 2.6f);
        movingPad.layer = 6;
        var padRend = movingPad.GetComponent<MeshRenderer>();
        if (idleMat != null) padRend.sharedMaterial = idleMat;

        GameObject lightObj = new GameObject("Plate_Status_Light");
        lightObj.transform.SetParent(plateRoot.transform);
        lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var pLight = lightObj.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1f, 0.2f, 0.1f);
        pLight.intensity = 2.5f;
        pLight.range = 5.0f;
        pLight.shadows = LightShadows.None;

        var triggerCol = plateRoot.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(3.2f, 2.0f, 3.2f);
        triggerCol.center = new Vector3(0f, 1.0f, 0f);

        var comp = plateRoot.AddComponent<KitchenPressurePlate>();
        comp.SetupForEditor(movingPad.transform, padRend, pLight, idleMat, activeMat, solvedMat);
        return comp;
    }

    private static GameObject CreateHazardFireBarrier(Transform parent, string name, Vector3 pos, Material fireMat, Material deadMat, float width = 12f)
    {
        GameObject barrierRoot = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(barrierRoot, "Create " + name);
        barrierRoot.transform.SetParent(parent);
        barrierRoot.transform.position = pos;

        int emitterCount = 5;
        float spacing = (width - 2f) / (emitterCount - 1);
        float startX = -(width - 2f) * 0.5f;

        for (int i = 0; i < emitterCount; i++)
        {
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flame.name = $"Flame_Emitter_{i}";
            flame.transform.SetParent(barrierRoot.transform);
            flame.transform.localPosition = new Vector3(startX + i * spacing, 0.4f, 0f);
            flame.transform.localScale = new Vector3(1.6f, 0.8f, 1.6f);
            var col = flame.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var rend = flame.GetComponent<MeshRenderer>();
            if (rend != null && fireMat != null) rend.sharedMaterial = fireMat;
        }

        GameObject fireLightObj = new GameObject("Barrier_Fire_Light");
        fireLightObj.transform.SetParent(barrierRoot.transform);
        fireLightObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var l = fireLightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.28f, 0.05f);
        l.intensity = 7.0f;
        l.range = 16f;

        GameObject deadZoneObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deadZoneObj.name = "DeadZone_Hazard_Trigger";
        deadZoneObj.tag = "Dead";
        deadZoneObj.transform.SetParent(barrierRoot.transform);
        deadZoneObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        deadZoneObj.transform.localScale = new Vector3(width, 2.5f, 2.5f);
        var dzCol = deadZoneObj.GetComponent<BoxCollider>();
        if (dzCol != null) dzCol.isTrigger = true;
        var dzRend = deadZoneObj.GetComponent<MeshRenderer>();
        if (dzRend != null) dzRend.enabled = false;

        return barrierRoot;
    }

    private static void CreateCompactCheckpoint(Transform parent, GameObject flagPrefab, int id, Vector3 pos, string name)
    {
        GameObject flag = (GameObject)PrefabUtility.InstantiatePrefab(flagPrefab, parent);
        Undo.RegisterCreatedObjectUndo(flag, "Create " + name);
        flag.name = name;
        flag.transform.position = pos;
        // Thu nhỏ cột cờ xuống tỉ lệ 0.2x để tương xứng với chiều cao nhân vật (~10-11m)
        flag.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        var cp = flag.GetComponent<OnlineCheckpoint>();
        if (cp == null) cp = flag.AddComponent<OnlineCheckpoint>();
        var idField = typeof(OnlineCheckpoint).GetField("_checkpointId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (idField != null) idField.SetValue(cp, id);

        var col = flag.GetComponent<BoxCollider>();
        if (col == null) col = flag.AddComponent<BoxCollider>();
        // Collider vùng nhận diện rộng ~7m cao 7m (35f * 0.2x) để người chơi dễ dàng kích hoạt khi đi qua
        col.size = new Vector3(35f, 35f, 35f);
        col.center = new Vector3(0f, 15f, 0f);
        col.isTrigger = true;
    }

    private static void CreateCoin(Transform parent, GameObject coinPrefab, Vector3 pos, int id, Material goldMat)
    {
        GameObject coinObj = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, parent);
        Undo.RegisterCreatedObjectUndo(coinObj, "Create Coin " + id);
        coinObj.name = $"Gold_Coin_Unity_{id}";
        coinObj.transform.position = pos;
        coinObj.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        coinObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var netObj = coinObj.GetComponent<NetworkObject>();
        if (netObj == null) netObj = coinObj.AddComponent<NetworkObject>();

        var col = coinObj.GetComponent<Collider>();
        if (col == null)
        {
            var sphereCol = coinObj.AddComponent<SphereCollider>();
            sphereCol.radius = 1.0f;
            sphereCol.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        var coinComp = coinObj.GetComponent<Coin>();
        if (coinComp == null) coinComp = coinObj.AddComponent<Coin>();

        var spinner = coinObj.GetComponent<CoinSpinner>();
        if (spinner == null) spinner = coinObj.AddComponent<CoinSpinner>();
        spinner.rotationSpeed = 120f;
        spinner.rotationAxis = Vector3.forward;

        if (goldMat != null)
        {
            foreach (var rend in coinObj.GetComponentsInChildren<Renderer>())
            {
                rend.sharedMaterial = goldMat;
            }
        }
    }

    private static Material GetOrCreateMaterial(string path, Color color, Color emissionColor, float metallic = 0f, float smoothness = 0.5f)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            string folder = Path.GetDirectoryName(path).Replace("\\", "/");
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

        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (emissionColor != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
        }
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
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

    private static GameObject SpawnKayKit(string modelName, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, bool addCollider = true, int layer = 0)
    {
        return SpawnAsset(KayKitBase + modelName + ".fbx", parent, pos, rot, scale, addCollider, layer);
    }

    private static GameObject SpawnMedieval(string modelName, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, bool addCollider = true, int layer = 0)
    {
        return SpawnAsset(MedievalBase + modelName + ".fbx", parent, pos, rot, scale, addCollider, layer);
    }

    private static GameObject SpawnQuangModel(string modelName, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, bool addCollider = true, int layer = 0)
    {
        return SpawnAsset(QuangModelBase + modelName + ".fbx", parent, pos, rot, scale, addCollider, layer);
    }

    private static GameObject SpawnQuangNature(string modelName, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, bool addCollider = true, int layer = 0)
    {
        return SpawnAsset(QuangNatureBase + modelName + ".fbx", parent, pos, rot, scale, addCollider, layer);
    }

    private static GameObject SpawnAsset(string fullPath, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, bool addCollider, int layer)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing asset: " + fullPath);
            return null;
        }

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(inst, "Spawn " + Path.GetFileNameWithoutExtension(fullPath));
        inst.transform.position = pos;
        inst.transform.rotation = Quaternion.Euler(rot);
        inst.transform.localScale = scale;
        inst.layer = layer;

        // Loại bỏ triệt để các Camera / AudioListener rác xuất khẩu nhầm từ file 3D (Blender)
        foreach (var cam in inst.GetComponentsInChildren<Camera>(true))
        {
            Object.DestroyImmediate(cam.gameObject);
        }
        foreach (var listener in inst.GetComponentsInChildren<AudioListener>(true))
        {
            Object.DestroyImmediate(listener);
        }

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

    [MenuItem("Tools/Apply All Zones Clutter and Wide Gates")]
    public static void ApplyAllZonesEnhancements()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.name != "Kitchen_Map")
        {
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Kitchen_Map.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        // 1. Mở rộng toàn bộ cổng và vòm cửa khắp bản đồ lên 18m
        var puzzle1 = GameObject.Find("Kitchen_Puzzle_PrepIsland_LinkedLights");
        if (puzzle1 != null) ApplyGateWidening(puzzle1);

        WidenArchwayAndGate("Cookline_Grand_Archway", 133f, "Cookline_Gate");
        WidenArchwayOnly("Dishwashing_Grand_Archway", 226f);
        WidenArchwayAndGate("Cryo_Vault_Grand_Archway", 300f, "Cryo_Gate");
        WidenCipherGate();
        WidenExitDoorway();

        // 2. Bố trí rào tàng hình và đồ đạc ngổn ngang 2 bên toàn bộ các khu vực
        var zone1 = GameObject.Find("Zone1_PrepIsland");
        if (zone1 != null)
        {
            BuildZone1InvisibleBarriers(zone1.transform);
            BuildZone1SideClutter(zone1.transform);
        }

        var zone2 = GameObject.Find("Zone2_CooklineStoves");
        if (zone2 != null)
        {
            BuildZone2InvisibleBarriers(zone2.transform);
            BuildZone2SideClutter(zone2.transform);
        }

        var zone3 = GameObject.Find("Zone3_DishwashingSeesaw");
        if (zone3 != null)
        {
            BuildZone3InvisibleBarriers(zone3.transform);
            BuildZone3SideClutter(zone3.transform);
        }

        var zone4 = GameObject.Find("Zone4_ColdStorageVault");
        if (zone4 != null)
        {
            BuildZone4InvisibleBarriers(zone4.transform);
            BuildZone4SideClutter(zone4.transform);
        }

        var zone5 = GameObject.Find("Zone5_HighRisePantry");
        if (zone5 != null)
        {
            BuildZone5InvisibleBarriers(zone5.transform);
            BuildZone5SideClutter(zone5.transform);
        }

        var zone6 = GameObject.Find("Zone6_VIPBanquetAndPatio");
        if (zone6 != null)
        {
            BuildZone6InvisibleBarriers(zone6.transform);
            BuildZone6SideClutter(zone6.transform);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[KitchenMapBuilder] Đã nâng cấp toàn bộ các cổng to 18m và bố trí vật thể ngổn ngang cùng rào tàng hình trên toàn bộ map thành công!");
    }

    [MenuItem("Tools/Apply Zone 1 Clutter and Widen Gate")]
    public static void ApplyZone1Enhancements()
    {
        ApplyAllZonesEnhancements();
    }

    private static void WidenArchwayAndGate(string archName, float zPos, string gateName)
    {
        var arch = GameObject.Find(archName);
        if (arch == null) return;

        var pLeft = arch.transform.Find("Partition_Wall_Left");
        if (pLeft != null)
        {
            pLeft.position = new Vector3(-28.75f, 8f, zPos);
            pLeft.localScale = new Vector3(38.5f, 16f, 1.2f);
        }

        var pRight = arch.transform.Find("Partition_Wall_Right");
        if (pRight != null)
        {
            pRight.position = new Vector3(28.75f, 8f, zPos);
            pRight.localScale = new Vector3(38.5f, 16f, 1.2f);
        }

        var lintel = arch.transform.Find("Archway_Top_Lintel");
        if (lintel != null)
        {
            lintel.position = new Vector3(0f, 15f, zPos);
            lintel.localScale = new Vector3(20f, 3f, 1.5f);
        }

        // Cập nhật vị trí 2 trụ pillar_A
        int pillarIdx = 0;
        for (int i = 0; i < arch.transform.childCount; i++)
        {
            var child = arch.transform.GetChild(i);
            if (child.name.Contains("pillar_A"))
            {
                if (pillarIdx == 0) child.position = new Vector3(-9.5f, 1f, zPos);
                else child.position = new Vector3(9.5f, 1f, zPos);
                pillarIdx++;
            }
        }

        var gate = arch.transform.Find(gateName);
        if (gate != null)
        {
            var shutter = gate.Find("Sliding_Security_Shutter");
            if (shutter != null)
            {
                shutter.localScale = new Vector3(17.5f, 13.6f, 0.8f);
            }

            var lampL = gate.Find("Gate_Status_Lamp_Left");
            if (lampL != null) lampL.localPosition = new Vector3(-9.0f, 8f, -0.6f);

            var lampR = gate.Find("Gate_Status_Lamp_Right");
            if (lampR != null) lampR.localPosition = new Vector3(9.0f, 8f, -0.6f);
        }
    }

    private static void WidenArchwayOnly(string archName, float zPos)
    {
        var arch = GameObject.Find(archName);
        if (arch == null) return;

        var pLeft = arch.transform.Find("Partition_Wall_Left");
        if (pLeft != null)
        {
            pLeft.position = new Vector3(-28.75f, 8f, zPos);
            pLeft.localScale = new Vector3(38.5f, 16f, 1.2f);
        }

        var pRight = arch.transform.Find("Partition_Wall_Right");
        if (pRight != null)
        {
            pRight.position = new Vector3(28.75f, 8f, zPos);
            pRight.localScale = new Vector3(38.5f, 16f, 1.2f);
        }

        var lintel = arch.transform.Find("Archway_Top_Lintel");
        if (lintel != null)
        {
            lintel.position = new Vector3(0f, 15f, zPos);
            lintel.localScale = new Vector3(20f, 3f, 1.5f);
        }

        int pillarIdx = 0;
        for (int i = 0; i < arch.transform.childCount; i++)
        {
            var child = arch.transform.GetChild(i);
            if (child.name.Contains("pillar_A"))
            {
                if (pillarIdx == 0) child.position = new Vector3(-9.5f, 1f, zPos);
                else child.position = new Vector3(9.5f, 1f, zPos);
                pillarIdx++;
            }
        }
    }

    private static void WidenCipherGate()
    {
        var cipherGate = GameObject.Find("CipherGate");
        if (cipherGate == null) return;

        var lPost = cipherGate.transform.Find("LeftPost");
        if (lPost != null) lPost.localPosition = new Vector3(-5.0f, 2.0f, 0f);

        var rPost = cipherGate.transform.Find("RightPost");
        if (rPost != null) rPost.localPosition = new Vector3(5.0f, 2.0f, 0f);

        var mGate = cipherGate.transform.Find("MovingGate");
        if (mGate != null) mGate.localScale = new Vector3(9.3f, 3.6f, 0.45f);

        var lintel = cipherGate.transform.Find("Lintel");
        if (lintel != null) lintel.localScale = new Vector3(10.7f, 0.4f, 0.9f);
    }

    private static void WidenExitDoorway()
    {
        var exitDoor = GameObject.Find("Exit_Grand_Doorway");
        if (exitDoor == null) return;

        int pillarIdx = 0;
        for (int i = 0; i < exitDoor.transform.childCount; i++)
        {
            var child = exitDoor.transform.GetChild(i);
            if (child.name.Contains("pillar_A"))
            {
                if (pillarIdx == 0) child.position = new Vector3(-9.5f, 1f, 480f);
                else child.position = new Vector3(9.5f, 1f, 480f);
                pillarIdx++;
            }
        }

        var lintel = exitDoor.transform.Find("Exit_Archway_Lintel");
        if (lintel != null)
        {
            lintel.position = new Vector3(0f, 15f, 480f);
            lintel.localScale = new Vector3(20f, 3f, 1.5f);
        }
    }

    private static void BuildZone2InvisibleBarriers(Transform zone2Parent)
    {
        var old = GameObject.Find("Zone2_Invisible_Barriers");
        if (old != null) Object.DestroyImmediate(old);

        GameObject barrierContainer = new GameObject("Zone2_Invisible_Barriers");
        Undo.RegisterCreatedObjectUndo(barrierContainer, "Create Zone2 Invisible Barriers");
        barrierContainer.transform.SetParent(zone2Parent);
        barrierContainer.transform.localPosition = Vector3.zero;

        // Hành lang Zone 2 từ Z = 62 đến Z = 133 (dài 71m)
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone2_Left", new Vector3(-9.2f, 4.0f, 97.5f), new Vector3(0.6f, 8.0f, 71.0f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone2_Right", new Vector3(9.2f, 4.0f, 97.5f), new Vector3(0.6f, 8.0f, 71.0f));
    }

    private static void BuildZone2SideClutter(Transform zone2Parent)
    {
        var old = GameObject.Find("Zone2_Side_Clutter");
        if (old != null) Object.DestroyImmediate(old);

        GameObject clutterContainer = new GameObject("Zone2_Side_Clutter");
        Undo.RegisterCreatedObjectUndo(clutterContainer, "Create Zone2 Side Clutter");
        clutterContainer.transform.SetParent(zone2Parent);
        clutterContainer.transform.localPosition = Vector3.zero;

        // Bên trái (X < -11, Z = 65 đến 125): Bếp lò, củi cháy, chảo, nồi, thùng gỗ
        SpawnKayKit("stove_multi", clutterContainer.transform, new Vector3(-15f, 1f, 68f), new Vector3(0f, 30f, 0f), new Vector3(4.5f, 4f, 4.5f), true, 6);
        SpawnKayKit("chair_stool", clutterContainer.transform, new Vector3(-12f, 1f, 74f), new Vector3(85f, -20f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnKayKit("crate_ham", clutterContainer.transform, new Vector3(-17f, 1f, 78f), new Vector3(0f, 45f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-13.5f, 1f, 82f), new Vector3(0f, -15f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-18f, 1.5f, 86f), new Vector3(90f, 50f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Firewood_01", clutterContainer.transform, new Vector3(-13f, 1f, 90f), new Vector3(0f, 75f, 0f), new Vector3(4f, 3f, 4f), true, 6);
        SpawnKayKit("pot_large", clutterContainer.transform, new Vector3(-16f, 1f, 95f), new Vector3(0f, 0f, 30f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnKayKit("crate_potatoes", clutterContainer.transform, new Vector3(-20f, 1f, 98f), new Vector3(15f, 20f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Bag_01", clutterContainer.transform, new Vector3(-13f, 1f, 102f), new Vector3(-15f, 40f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate", clutterContainer.transform, new Vector3(-17f, 1f, 108f), new Vector3(0f, -30f, 0f), new Vector3(4f, 5f, 4f), true, 6);
        SpawnMedieval("Firewood_02", clutterContainer.transform, new Vector3(-13.5f, 1f, 114f), new Vector3(0f, 110f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("cuttingboard", clutterContainer.transform, new Vector3(-18f, 1.1f, 118f), new Vector3(0f, 55f, 0f), new Vector3(5f, 1.5f, 5f), true, 6);
        SpawnKayKit("crate_steak", clutterContainer.transform, new Vector3(-14f, 1f, 124f), new Vector3(0f, 15f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);

        // Bên phải (X > +11, Z = 65 đến 125): Thùng than củi, chảo lửa, nồi hầm, bao tải
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(14f, 1f, 70f), new Vector3(0f, 25f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_buns", clutterContainer.transform, new Vector3(18f, 1f, 72f), new Vector3(0f, -40f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Firewood_01", clutterContainer.transform, new Vector3(13f, 1f, 76f), new Vector3(0f, -80f, 0f), new Vector3(4f, 3f, 4f), true, 6);
        SpawnKayKit("chair_stool", clutterContainer.transform, new Vector3(17f, 1f, 82f), new Vector3(-80f, 30f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnMedieval("Bag_02", clutterContainer.transform, new Vector3(13.5f, 1f, 88f), new Vector3(0f, 65f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(18.5f, 1.5f, 92f), new Vector3(90f, -45f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_tomatoes", clutterContainer.transform, new Vector3(14f, 1f, 96f), new Vector3(20f, 10f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("pot_large", clutterContainer.transform, new Vector3(19f, 1f, 102f), new Vector3(0f, 0f, -25f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnMedieval("Firewood_03", clutterContainer.transform, new Vector3(13f, 1f, 106f), new Vector3(0f, 45f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_carrots", clutterContainer.transform, new Vector3(17f, 1f, 112f), new Vector3(0f, -20f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("crate", clutterContainer.transform, new Vector3(13.5f, 1f, 118f), new Vector3(0f, 35f, 0f), new Vector3(4f, 5f, 4f), true, 6);
        SpawnKayKit("dishrack", clutterContainer.transform, new Vector3(18f, 1f, 122f), new Vector3(0f, -50f, 0f), new Vector3(4.5f, 3f, 4.5f), true, 6);
    }

    private static void BuildZone3InvisibleBarriers(Transform zone3Parent)
    {
        var old = GameObject.Find("Zone3_Invisible_Barriers");
        if (old != null) Object.DestroyImmediate(old);

        GameObject barrierContainer = new GameObject("Zone3_Invisible_Barriers");
        Undo.RegisterCreatedObjectUndo(barrierContainer, "Create Zone3 Invisible Barriers");
        barrierContainer.transform.SetParent(zone3Parent);
        barrierContainer.transform.localPosition = Vector3.zero;

        // Hành lang Zone 3 từ Z = 133 đến Z = 226 (dài 93m)
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone3_Left", new Vector3(-9.2f, 4.0f, 179.5f), new Vector3(0.6f, 8.0f, 93.0f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone3_Right", new Vector3(9.2f, 4.0f, 179.5f), new Vector3(0.6f, 8.0f, 93.0f));
    }

    private static void BuildZone3SideClutter(Transform zone3Parent)
    {
        var old = GameObject.Find("Zone3_Side_Clutter");
        if (old != null) Object.DestroyImmediate(old);

        GameObject clutterContainer = new GameObject("Zone3_Side_Clutter");
        Undo.RegisterCreatedObjectUndo(clutterContainer, "Create Zone3 Side Clutter");
        clutterContainer.transform.SetParent(zone3Parent);
        clutterContainer.transform.localPosition = Vector3.zero;

        // Rửa bát: Chồng đĩa, xô nước, xà phòng, bàn gỗ, thùng phuy
        // Bên trái (X < -11, Z = 140 đến 220)
        SpawnKayKit("dishrack_plates", clutterContainer.transform, new Vector3(-14f, 1f, 142f), new Vector3(0f, 20f, 0f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnMedieval("Bucket_of_water", clutterContainer.transform, new Vector3(-17f, 1f, 148f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-13.5f, 1f, 155f), new Vector3(0f, -30f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("plate", clutterContainer.transform, new Vector3(-18f, 1.1f, 162f), new Vector3(0f, 45f, 0f), new Vector3(6f, 2f, 6f), true, 6);
        SpawnMedieval("Empty_bucket", clutterContainer.transform, new Vector3(-13f, 1f, 170f), new Vector3(70f, 15f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-18.5f, 1.5f, 178f), new Vector3(90f, 35f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("chair_stool", clutterContainer.transform, new Vector3(-14f, 1f, 185f), new Vector3(-80f, 20f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnKayKit("crate", clutterContainer.transform, new Vector3(-17f, 1f, 192f), new Vector3(0f, -40f, 0f), new Vector3(4f, 5.5f, 4f), true, 6);
        SpawnMedieval("Bag_01", clutterContainer.transform, new Vector3(-13f, 1f, 200f), new Vector3(15f, 50f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("dishrack", clutterContainer.transform, new Vector3(-18f, 1f, 208f), new Vector3(0f, 10f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Bucket_of_water", clutterContainer.transform, new Vector3(-13.5f, 1f, 215f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);

        // Bên phải (X > +11, Z = 140 đến 220)
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(14f, 1f, 145f), new Vector3(0f, -15f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("dishrack_plates", clutterContainer.transform, new Vector3(18f, 1f, 150f), new Vector3(0f, 35f, 0f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnMedieval("Empty_bucket", clutterContainer.transform, new Vector3(13.5f, 1f, 158f), new Vector3(-75f, 25f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_cheese", clutterContainer.transform, new Vector3(17f, 1f, 166f), new Vector3(0f, -20f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(14f, 1.5f, 175f), new Vector3(90f, -40f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Bucket_of_water", clutterContainer.transform, new Vector3(18.5f, 1f, 182f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("plate", clutterContainer.transform, new Vector3(13f, 1.1f, 190f), new Vector3(0f, -60f, 0f), new Vector3(6f, 2f, 6f), true, 6);
        SpawnKayKit("chair_stool", clutterContainer.transform, new Vector3(17.5f, 1f, 198f), new Vector3(85f, -10f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnKayKit("crate", clutterContainer.transform, new Vector3(13.5f, 1f, 206f), new Vector3(0f, 45f, 0f), new Vector3(4f, 5.5f, 4f), true, 6);
        SpawnMedieval("Bag_03", clutterContainer.transform, new Vector3(18f, 1f, 214f), new Vector3(0f, -30f, 0f), new Vector3(3.2f, 3.2f, 3.2f), true, 6);
    }

    private static void BuildZone4InvisibleBarriers(Transform zone4Parent)
    {
        var old = GameObject.Find("Zone4_Invisible_Barriers");
        if (old != null) Object.DestroyImmediate(old);

        GameObject barrierContainer = new GameObject("Zone4_Invisible_Barriers");
        Undo.RegisterCreatedObjectUndo(barrierContainer, "Create Zone4 Invisible Barriers");
        barrierContainer.transform.SetParent(zone4Parent);
        barrierContainer.transform.localPosition = Vector3.zero;

        // Hành lang Zone 4 từ Z = 226 đến Z = 300 (dài 74m)
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone4_Left", new Vector3(-9.2f, 4.0f, 263.0f), new Vector3(0.6f, 8.0f, 74.0f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone4_Right", new Vector3(9.2f, 4.0f, 263.0f), new Vector3(0.6f, 8.0f, 74.0f));
    }

    private static void BuildZone4SideClutter(Transform zone4Parent)
    {
        var old = GameObject.Find("Zone4_Side_Clutter");
        if (old != null) Object.DestroyImmediate(old);

        GameObject clutterContainer = new GameObject("Zone4_Side_Clutter");
        Undo.RegisterCreatedObjectUndo(clutterContainer, "Create Zone4 Side Clutter");
        clutterContainer.transform.SetParent(zone4Parent);
        clutterContainer.transform.localPosition = Vector3.zero;

        // Kho đông: Thùng thịt đông, thùng cá, khối băng, thùng gỗ, bao tải lạnh
        // Bên trái (X < -11, Z = 232 đến 296)
        SpawnKayKit("crate_steak", clutterContainer.transform, new Vector3(-14f, 1f, 235f), new Vector3(0f, 30f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-18f, 1f, 240f), new Vector3(0f, -20f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_ham", clutterContainer.transform, new Vector3(-13.5f, 1f, 248f), new Vector3(15f, 45f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Bag_01", clutterContainer.transform, new Vector3(-17.5f, 1f, 255f), new Vector3(-10f, 60f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-14f, 1.5f, 262f), new Vector3(90f, 25f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_cheese", clutterContainer.transform, new Vector3(-18f, 1f, 270f), new Vector3(0f, -35f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Box_01", clutterContainer.transform, new Vector3(-13f, 1f, 278f), new Vector3(0f, 15f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate", clutterContainer.transform, new Vector3(-17f, 1f, 285f), new Vector3(0f, 50f, 0f), new Vector3(4f, 5.5f, 4f), true, 6);
        SpawnKayKit("crate_steak", clutterContainer.transform, new Vector3(-13.5f, 1f, 292f), new Vector3(20f, -15f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);

        // Bên phải (X > +11, Z = 232 đến 296)
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(14f, 1f, 236f), new Vector3(0f, -10f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_steak", clutterContainer.transform, new Vector3(18f, 1f, 242f), new Vector3(0f, 40f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Bag_02", clutterContainer.transform, new Vector3(13.5f, 1f, 250f), new Vector3(0f, -50f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(18.5f, 1.5f, 258f), new Vector3(90f, -35f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_ham", clutterContainer.transform, new Vector3(14f, 1f, 266f), new Vector3(0f, 20f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Box_02", clutterContainer.transform, new Vector3(17.5f, 1f, 274f), new Vector3(0f, -60f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate", clutterContainer.transform, new Vector3(13.5f, 1f, 282f), new Vector3(0f, 35f, 0f), new Vector3(4f, 5.5f, 4f), true, 6);
        SpawnKayKit("crate_potatoes", clutterContainer.transform, new Vector3(18f, 1f, 290f), new Vector3(-15f, 10f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
    }

    private static void BuildZone5InvisibleBarriers(Transform zone5Parent)
    {
        var old = GameObject.Find("Zone5_Invisible_Barriers");
        if (old != null) Object.DestroyImmediate(old);

        GameObject barrierContainer = new GameObject("Zone5_Invisible_Barriers");
        Undo.RegisterCreatedObjectUndo(barrierContainer, "Create Zone5 Invisible Barriers");
        barrierContainer.transform.SetParent(zone5Parent);
        barrierContainer.transform.localPosition = Vector3.zero;

        // Hành lang Zone 5 từ Z = 302 đến Z = 420 (dài 118m), cao 24m để chắn kín cả khi người chơi nhảy trên kệ cao
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone5_Left", new Vector3(-9.2f, 12.0f, 361.0f), new Vector3(0.6f, 24.0f, 118.0f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone5_Right", new Vector3(9.2f, 12.0f, 361.0f), new Vector3(0.6f, 24.0f, 118.0f));

        // Khóa ngang 2 đầu (nối từ mép hành lang ra vách tường ngoài X = ±48m)
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone5_Left_Back", new Vector3(-28.5f, 12.0f, 302.0f), new Vector3(38.0f, 24.0f, 0.6f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone5_Right_Back", new Vector3(28.5f, 12.0f, 302.0f), new Vector3(38.0f, 24.0f, 0.6f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone5_Left_Front", new Vector3(-28.5f, 12.0f, 420.0f), new Vector3(38.0f, 24.0f, 0.6f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone5_Right_Front", new Vector3(28.5f, 12.0f, 420.0f), new Vector3(38.0f, 24.0f, 0.6f));
    }

    private static void BuildZone5SideClutter(Transform zone5Parent)
    {
        var old = GameObject.Find("Zone5_Side_Clutter");
        if (old != null) Object.DestroyImmediate(old);

        GameObject clutterContainer = new GameObject("Zone5_Side_Clutter");
        Undo.RegisterCreatedObjectUndo(clutterContainer, "Create Zone5 Side Clutter");
        clutterContainer.transform.SetParent(zone5Parent);
        clutterContainer.transform.localPosition = Vector3.zero;

        // --- BÊN TRÁI HÀNH LANG (X < -11, Z = 308 đến 415) ---
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-14f, 1f, 310f), new Vector3(0f, 25f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-18f, 1.5f, 314f), new Vector3(90f, 40f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Bag_01", clutterContainer.transform, new Vector3(-13f, 1f, 320f), new Vector3(15f, -30f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_potatoes", clutterContainer.transform, new Vector3(-17f, 1f, 326f), new Vector3(0f, 50f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("crate_carrots", clutterContainer.transform, new Vector3(-13.5f, 1f, 332f), new Vector3(0f, -20f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("chair_stool", clutterContainer.transform, new Vector3(-18f, 1f, 338f), new Vector3(80f, 15f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnMedieval("Box_01", clutterContainer.transform, new Vector3(-14f, 1f, 345f), new Vector3(0f, 35f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-18.5f, 1f, 352f), new Vector3(0f, -15f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Bag_02", clutterContainer.transform, new Vector3(-13.5f, 1f, 358f), new Vector3(-10f, 65f, 0f), new Vector3(3.2f, 3.2f, 3.2f), true, 6);
        SpawnKayKit("crate_onions", clutterContainer.transform, new Vector3(-17f, 1f, 365f), new Vector3(0f, 40f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Firewood_01", clutterContainer.transform, new Vector3(-13f, 1f, 372f), new Vector3(0f, -70f, 0f), new Vector3(4f, 3f, 4f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-18f, 1.5f, 378f), new Vector3(90f, -30f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_buns", clutterContainer.transform, new Vector3(-14f, 1f, 385f), new Vector3(0f, 15f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Bag_03", clutterContainer.transform, new Vector3(-17.5f, 1f, 392f), new Vector3(0f, -45f, 0f), new Vector3(3.2f, 3.2f, 3.2f), true, 6);
        SpawnKayKit("crate_tomatoes", clutterContainer.transform, new Vector3(-13.5f, 1f, 398f), new Vector3(15f, 20f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Box_02", clutterContainer.transform, new Vector3(-18f, 1f, 405f), new Vector3(0f, 60f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Empty_bucket", clutterContainer.transform, new Vector3(-13f, 1f, 412f), new Vector3(75f, -20f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);

        // --- BÊN PHẢI HÀNH LANG (X > +11, Z = 308 đến 415) ---
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(14f, 1f, 312f), new Vector3(0f, -30f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_ham", clutterContainer.transform, new Vector3(18f, 1f, 318f), new Vector3(0f, 20f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Bag_01", clutterContainer.transform, new Vector3(13.5f, 1f, 324f), new Vector3(-15f, 45f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(18.5f, 1.5f, 330f), new Vector3(90f, -45f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_cheese", clutterContainer.transform, new Vector3(14f, 1f, 336f), new Vector3(0f, -35f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnKayKit("chair_stool", clutterContainer.transform, new Vector3(17.5f, 1f, 342f), new Vector3(-85f, 25f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnMedieval("Firewood_01", clutterContainer.transform, new Vector3(13f, 1f, 348f), new Vector3(0f, 85f, 0f), new Vector3(4f, 3f, 4f), true, 6);
        SpawnMedieval("Box_01", clutterContainer.transform, new Vector3(18f, 1f, 355f), new Vector3(0f, -50f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_potatoes", clutterContainer.transform, new Vector3(13.5f, 1f, 362f), new Vector3(20f, 15f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(18f, 1f, 368f), new Vector3(0f, 30f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Bag_02", clutterContainer.transform, new Vector3(14f, 1f, 375f), new Vector3(10f, -60f, 0f), new Vector3(3.2f, 3.2f, 3.2f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(18.5f, 1.5f, 382f), new Vector3(90f, 50f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_steak", clutterContainer.transform, new Vector3(13.5f, 1f, 388f), new Vector3(0f, -25f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Firewood_02", clutterContainer.transform, new Vector3(18f, 1f, 395f), new Vector3(0f, 100f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_carrots", clutterContainer.transform, new Vector3(14f, 1f, 402f), new Vector3(0f, 45f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Bucket_of_water", clutterContainer.transform, new Vector3(17.5f, 1f, 408f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Bag_03", clutterContainer.transform, new Vector3(13.5f, 1f, 414f), new Vector3(0f, -30f, 0f), new Vector3(3.2f, 3.2f, 3.2f), true, 6);
    }

    private static void BuildZone6InvisibleBarriers(Transform zone6Parent)
    {
        var old = GameObject.Find("Zone6_Invisible_Barriers");
        if (old != null) Object.DestroyImmediate(old);

        GameObject barrierContainer = new GameObject("Zone6_Invisible_Barriers");
        Undo.RegisterCreatedObjectUndo(barrierContainer, "Create Zone6 Invisible Barriers");
        barrierContainer.transform.SetParent(zone6Parent);
        barrierContainer.transform.localPosition = Vector3.zero;

        // Hành lang Zone 6 từ Z = 422 đến Z = 480 (dài 58m)
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone6_Left", new Vector3(-9.2f, 4.0f, 451.0f), new Vector3(0.6f, 8.0f, 58.0f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone6_Right", new Vector3(9.2f, 4.0f, 451.0f), new Vector3(0.6f, 8.0f, 58.0f));

        // Khóa ngang 2 đầu cửa ra Z = 480
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone6_Left_End", new Vector3(-28.5f, 4.0f, 480.0f), new Vector3(38.0f, 8.0f, 0.6f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Zone6_Right_End", new Vector3(28.5f, 4.0f, 480.0f), new Vector3(38.0f, 8.0f, 0.6f));
    }

    private static void BuildZone6SideClutter(Transform zone6Parent)
    {
        var old = GameObject.Find("Zone6_Side_Clutter");
        if (old != null) Object.DestroyImmediate(old);

        GameObject clutterContainer = new GameObject("Zone6_Side_Clutter");
        Undo.RegisterCreatedObjectUndo(clutterContainer, "Create Zone6 Side Clutter");
        clutterContainer.transform.SetParent(zone6Parent);
        clutterContainer.transform.localPosition = Vector3.zero;

        // Sảnh tiệc: Bàn tiệc, ghế ngã, thùng rượu, đĩa đồ ăn tiệc
        // Bên trái (X < -11, Z = 426 đến 476)
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-14f, 1f, 428f), new Vector3(0f, 25f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("chair_A", clutterContainer.transform, new Vector3(-17f, 1f, 434f), new Vector3(85f, -30f, 0f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnKayKit("crate_cheese", clutterContainer.transform, new Vector3(-13.5f, 1f, 440f), new Vector3(0f, -40f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-18f, 1.5f, 448f), new Vector3(90f, 45f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("plate", clutterContainer.transform, new Vector3(-13f, 1.1f, 456f), new Vector3(0f, 15f, 0f), new Vector3(6f, 2f, 6f), true, 6);
        SpawnMedieval("Bag_01", clutterContainer.transform, new Vector3(-17.5f, 1f, 464f), new Vector3(15f, -50f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("chair_B", clutterContainer.transform, new Vector3(-14f, 1f, 472f), new Vector3(-75f, 20f, 0f), new Vector3(4f, 4f, 4f), true, 6);

        // Bên phải (X > +11, Z = 426 đến 476)
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(14f, 1f, 430f), new Vector3(0f, -20f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("chair_B", clutterContainer.transform, new Vector3(18f, 1f, 436f), new Vector3(-80f, 35f, 0f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnKayKit("crate_buns", clutterContainer.transform, new Vector3(13.5f, 1f, 444f), new Vector3(0f, 50f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(18.5f, 1.5f, 452f), new Vector3(90f, -30f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("plate", clutterContainer.transform, new Vector3(14f, 1.1f, 460f), new Vector3(0f, -70f, 0f), new Vector3(6f, 2f, 6f), true, 6);
        SpawnMedieval("Bag_02", clutterContainer.transform, new Vector3(18f, 1f, 468f), new Vector3(-10f, 40f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("chair_A", clutterContainer.transform, new Vector3(13.5f, 1f, 474f), new Vector3(80f, -15f, 0f), new Vector3(4f, 4f, 4f), true, 6);
    }

    private static void ApplyGateWidening(GameObject puzzleInst)
    {
        var puzzle = puzzleInst.transform.Find("Puzzle");
        if (puzzle == null) return;

        var gate = puzzle.Find("LightsGate");
        var console = puzzle.Find("LightsConsole");

        if (gate != null)
        {
            var leftPost = gate.Find("LeftPost");
            var rightPost = gate.Find("RightPost");
            var movingGate = gate.Find("MovingGate");
            var lintel = gate.Find("Lintel");

            // Mở rộng khoảng cách giữa 2 trụ lên 18m (world) để cả 2 người chơi cùng lúc đi qua thoải mái
            if (leftPost != null) leftPost.localPosition = new Vector3(-5.0f, 2.0f, 0f);
            if (rightPost != null) rightPost.localPosition = new Vector3(5.0f, 2.0f, 0f);
            if (movingGate != null)
            {
                movingGate.localPosition = new Vector3(0f, 1.8f, 0f);
                movingGate.localScale = new Vector3(9.3f, 3.6f, 0.45f);
            }
            if (lintel != null)
            {
                lintel.localScale = new Vector3(10.7f, 0.4f, 0.9f);
            }
        }

        if (console != null)
        {
            // Bố trí bàn điều khiển đèn sát mép lối đi để người chơi tương tác dễ dàng
            console.localPosition = new Vector3(-4.6f, 0f, 0f);
        }
    }

    private static void BuildZone1InvisibleBarriers(Transform zone1Parent)
    {
        var oldBarriers = GameObject.Find("Zone1_Invisible_Barriers");
        if (oldBarriers != null) Object.DestroyImmediate(oldBarriers);

        GameObject barrierContainer = new GameObject("Zone1_Invisible_Barriers");
        Undo.RegisterCreatedObjectUndo(barrierContainer, "Create Zone1 Invisible Barriers");
        barrierContainer.transform.SetParent(zone1Parent);
        barrierContainer.transform.localPosition = Vector3.zero;

        // 2 Hàng rào tàng hình dọc 2 bên lối đi (Z = 30 đến 62, X = ±9.2)
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Left_Corridor", new Vector3(-9.2f, 4.0f, 46.0f), new Vector3(0.6f, 8.0f, 32.0f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Right_Corridor", new Vector3(9.2f, 4.0f, 46.0f), new Vector3(0.6f, 8.0f, 32.0f));

        // Hàng rào chắn ngang bịt kín 2 đầu sau và trước (ngăn người chơi đi vòng vào khu phế liệu)
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Left_Back", new Vector3(-28.5f, 4.0f, 30.0f), new Vector3(38.0f, 8.0f, 0.6f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Right_Back", new Vector3(28.5f, 4.0f, 30.0f), new Vector3(38.0f, 8.0f, 0.6f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Left_Front", new Vector3(-28.5f, 4.0f, 62.0f), new Vector3(38.0f, 8.0f, 0.6f));
        CreateInvisibleCollider(barrierContainer.transform, "Barrier_Right_Front", new Vector3(28.5f, 4.0f, 62.0f), new Vector3(38.0f, 8.0f, 0.6f));
    }

    private static void CreateInvisibleCollider(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = false;
        go.layer = 6; // Obstacle layer ngăn nhân vật bước qua
    }

    private static void BuildZone1SideClutter(Transform zone1Parent)
    {
        var oldClutter = GameObject.Find("Zone1_Side_Clutter");
        if (oldClutter != null) Object.DestroyImmediate(oldClutter);

        GameObject clutterContainer = new GameObject("Zone1_Side_Clutter");
        Undo.RegisterCreatedObjectUndo(clutterContainer, "Create Zone1 Side Clutter");
        clutterContainer.transform.SetParent(zone1Parent);
        clutterContainer.transform.localPosition = Vector3.zero;

        // --- BÊN TRÁI ĐƯỜNG ĐI (X < -10, Z = 34 đến 58) ---
        SpawnKayKit("crate", clutterContainer.transform, new Vector3(-13.5f, 1.0f, 35.0f), new Vector3(0f, 25f, 0f), new Vector3(4f, 5.5f, 4f), true, 6);
        SpawnKayKit("crate_buns", clutterContainer.transform, new Vector3(-16.0f, 1.0f, 36.5f), new Vector3(0f, -40f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnKayKit("crate_cheese", clutterContainer.transform, new Vector3(-13.0f, 5.5f, 35.0f), new Vector3(0f, 45f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnKayKit("chair_stool", clutterContainer.transform, new Vector3(-11.5f, 1.0f, 39.0f), new Vector3(75f, 20f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-14.5f, 1.0f, 42.0f), new Vector3(0f, 15f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-18.0f, 1.5f, 41.0f), new Vector3(90f, 40f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Bag_01", clutterContainer.transform, new Vector3(-12.5f, 1.0f, 44.5f), new Vector3(15f, -25f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_potatoes", clutterContainer.transform, new Vector3(-21.0f, 1.0f, 38.0f), new Vector3(0f, 60f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("crate_carrots", clutterContainer.transform, new Vector3(-15.5f, 1.0f, 47.5f), new Vector3(0f, -15f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("cuttingboard", clutterContainer.transform, new Vector3(-12.0f, 1.05f, 49.5f), new Vector3(0f, 35f, 0f), new Vector3(5f, 1.5f, 5f), true, 6);
        SpawnKayKit("pot_large", clutterContainer.transform, new Vector3(-18.0f, 1.0f, 47.0f), new Vector3(0f, 0f, 25f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnMedieval("Firewood_01", clutterContainer.transform, new Vector3(-22.0f, 1.0f, 46.0f), new Vector3(0f, 85f, 0f), new Vector3(4f, 3f, 4f), true, 6);
        SpawnMedieval("Bag_02", clutterContainer.transform, new Vector3(-14.0f, 1.0f, 52.5f), new Vector3(0f, 70f, 0f), new Vector3(3.2f, 3.2f, 3.2f), true, 6);
        SpawnKayKit("crate_tomatoes", clutterContainer.transform, new Vector3(-17.5f, 1.0f, 53.5f), new Vector3(15f, 30f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("crate_lettuce", clutterContainer.transform, new Vector3(-23.0f, 1.0f, 52.0f), new Vector3(0f, -50f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(-26.0f, 1.0f, 42.0f), Vector3.zero, new Vector3(3.8f, 3.8f, 3.8f), true, 6);
        SpawnKayKit("dishrack_plates", clutterContainer.transform, new Vector3(-13.5f, 1.0f, 56.5f), new Vector3(0f, 10f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Empty_bucket", clutterContainer.transform, new Vector3(-19.0f, 1.0f, 57.0f), new Vector3(80f, 15f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);

        // --- BÊN PHẢI ĐƯỜNG ĐI (X > +10, Z = 34 đến 58) ---
        SpawnKayKit("crate", clutterContainer.transform, new Vector3(13.5f, 1.0f, 35.5f), new Vector3(0f, -30f, 0f), new Vector3(4f, 5.5f, 4f), true, 6);
        SpawnKayKit("crate_ham", clutterContainer.transform, new Vector3(16.5f, 1.0f, 36.5f), new Vector3(0f, 20f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnKayKit("crate_onions", clutterContainer.transform, new Vector3(13.0f, 5.5f, 35.5f), new Vector3(0f, -15f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnKayKit("chair_stool", clutterContainer.transform, new Vector3(11.5f, 1.0f, 39.5f), new Vector3(-70f, 40f, 0f), new Vector3(3.5f, 2.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(14.5f, 1.0f, 42.5f), new Vector3(0f, -10f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(18.5f, 1.5f, 41.5f), new Vector3(90f, -30f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnMedieval("Bag_01", clutterContainer.transform, new Vector3(12.5f, 1.0f, 45.0f), new Vector3(-10f, 45f, 0f), new Vector3(3.5f, 3.5f, 3.5f), true, 6);
        SpawnKayKit("crate_carrots", clutterContainer.transform, new Vector3(21.5f, 1.0f, 38.5f), new Vector3(0f, -45f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("crate_potatoes", clutterContainer.transform, new Vector3(15.0f, 1.2f, 47.0f), new Vector3(20f, 30f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("pot_large", clutterContainer.transform, new Vector3(19.0f, 1.0f, 46.5f), new Vector3(0f, 0f, -20f), new Vector3(4f, 4f, 4f), true, 6);
        SpawnMedieval("Firewood_01", clutterContainer.transform, new Vector3(13.0f, 1.0f, 50.0f), new Vector3(0f, -65f, 0f), new Vector3(4f, 3f, 4f), true, 6);
        SpawnKayKit("cuttingboard", clutterContainer.transform, new Vector3(22.0f, 1.05f, 47.5f), new Vector3(0f, 80f, 0f), new Vector3(5f, 1.5f, 5f), true, 6);
        SpawnMedieval("Bag_03", clutterContainer.transform, new Vector3(14.5f, 1.0f, 53.0f), new Vector3(0f, -25f, 0f), new Vector3(3.2f, 3.2f, 3.2f), true, 6);
        SpawnKayKit("crate_steak", clutterContainer.transform, new Vector3(18.0f, 1.0f, 52.5f), new Vector3(0f, 55f, 0f), new Vector3(4.5f, 3.5f, 4.5f), true, 6);
        SpawnKayKit("crate_buns", clutterContainer.transform, new Vector3(24.0f, 1.0f, 51.0f), new Vector3(0f, -10f, 0f), new Vector3(4.5f, 2.5f, 4.5f), true, 6);
        SpawnMedieval("Barrel", clutterContainer.transform, new Vector3(26.5f, 1.0f, 43.0f), new Vector3(0f, 15f, 0f), new Vector3(3.8f, 3.8f, 3.8f), true, 6);
        SpawnKayKit("dishrack", clutterContainer.transform, new Vector3(13.5f, 1.0f, 56.5f), new Vector3(0f, -40f, 0f), new Vector3(4f, 3f, 4f), true, 6);
        SpawnMedieval("Bucket_of_water", clutterContainer.transform, new Vector3(18.5f, 1.0f, 57.0f), Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f), true, 6);
    }
}
#endif
