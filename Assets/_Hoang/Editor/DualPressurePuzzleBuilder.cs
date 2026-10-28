#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Hoang.Puzzles;
using System.IO;

namespace Hoang.Editor
{
    // Công cụ Editor tự động sinh Prefab câu đố Bàn đạp áp lực đôi chuẩn mực.
    public static class DualPressurePuzzleBuilder
    {
        private const string RequestPath = "Temp/DualPressurePuzzle/build_request.txt";
        private const string ResultPath = "Temp/DualPressurePuzzle/build_result.txt";

        [InitializeOnLoadMethod]
        private static void Register() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (!File.Exists(RequestPath) || EditorApplication.isCompiling ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;

            File.Delete(RequestPath);
            try
            {
                BuildPrefab();
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

        [MenuItem("Tools/Hoang/Build Dual Pressure Gate Prefab")]
        public static void BuildPrefab()
        {
            string folderPath = "Assets/_Hoang/Prefabs/Puzzles";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string prefabPath = $"{folderPath}/DualPressureGatePuzzle.prefab";

            // Load vật liệu sẵn có trong dự án
            Material redMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Full_Red.mat");
            Material greenMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Full_Green.mat");
            Material blackMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Black.mat");
            Material steelMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Steel 1.mat");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Wood.mat");

            // 1. Root GameObject
            GameObject root = new GameObject("DualPressureGatePuzzle");
            NetworkObject netObj = root.AddComponent<NetworkObject>();
            DualPressurePuzzleCoordinator coordinator = root.AddComponent<DualPressurePuzzleCoordinator>();

            // 2. Tạo Plate A & Plate B
            // Khoảng cách 4m (-2m và +2m) khớp với cự ly cầm tổ 2 người chơi
            GameObject plateAObj = CreatePlate("Plate_A", new Vector3(-2f, 0f, 0f), root.transform, redMat, greenMat, blackMat);
            GameObject plateBObj = CreatePlate("Plate_B", new Vector3(2f, 0f, 0f), root.transform, redMat, greenMat, blackMat);

            PressurePlate plateAComp = plateAObj.GetComponent<PressurePlate>();
            PressurePlate plateBComp = plateBObj.GetComponent<PressurePlate>();

            // 3. Tạo Gate (Cổng chắn)
            GameObject gateObj = CreateGate("Gate", new Vector3(0f, 0f, 4f), root.transform, steelMat ?? blackMat, woodMat ?? steelMat);
            PuzzleGate gateComp = gateObj.GetComponent<PuzzleGate>();

            // 4. Thiết lập liên kết Inspector cho Coordinator bằng SerializedObject
            SerializedObject coordSO = new SerializedObject(coordinator);
            coordSO.FindProperty("_plateA").objectReferenceValue = plateAComp;
            coordSO.FindProperty("_plateB").objectReferenceValue = plateBComp;
            coordSO.FindProperty("_gate").objectReferenceValue = gateComp;
            coordSO.FindProperty("_mode").enumValueIndex = (int)DualPuzzleMode.HoldBoth;
            coordSO.FindProperty("_timedDuration").floatValue = 7f;
            coordSO.ApplyModifiedPropertiesWithoutUndo();

            // 5. Lưu prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DualPressurePuzzleBuilder] Đã tạo thành công Prefab tại: {prefabPath}");
        }

        private static GameObject CreatePlate(string name, Vector3 localPos, Transform parent,
            Material redMat, Material greenMat, Material baseMat)
        {
            GameObject plateRoot = new GameObject(name);
            plateRoot.transform.SetParent(parent, false);
            plateRoot.transform.localPosition = localPos;

            // Trigger nhận diện người chơi
            BoxCollider trigger = plateRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.8f, 0.5f, 1.8f);
            trigger.center = new Vector3(0f, 0.25f, 0f);

            Rigidbody rb = plateRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            PressurePlate plateComp = plateRoot.AddComponent<PressurePlate>();

            // Phần đế cố định
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(plateRoot.transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            baseObj.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
            Object.DestroyImmediate(baseObj.GetComponent<Collider>());
            if (baseMat != null) baseObj.GetComponent<MeshRenderer>().sharedMaterial = baseMat;

            // Phần đệm lún (Pad)
            GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padObj.name = "Pad";
            padObj.transform.SetParent(plateRoot.transform, false);
            padObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            padObj.transform.localScale = new Vector3(1.4f, 0.06f, 1.4f);
            Object.DestroyImmediate(padObj.GetComponent<Collider>());
            MeshRenderer padRenderer = padObj.GetComponent<MeshRenderer>();
            if (redMat != null) padRenderer.sharedMaterial = redMat;

            // Thiết lập SerializedProperty cho PressurePlate
            SerializedObject plateSO = new SerializedObject(plateComp);
            plateSO.FindProperty("_plateMesh").objectReferenceValue = padObj.transform;
            plateSO.FindProperty("_indicatorRenderer").objectReferenceValue = padRenderer;
            plateSO.FindProperty("_inactiveMaterial").objectReferenceValue = redMat;
            plateSO.FindProperty("_activeMaterial").objectReferenceValue = greenMat;
            plateSO.FindProperty("_pressDepth").floatValue = 0.05f;
            plateSO.FindProperty("_animSpeed").floatValue = 10f;
            plateSO.ApplyModifiedPropertiesWithoutUndo();

            return plateRoot;
        }

        private static GameObject CreateGate(string name, Vector3 localPos, Transform parent,
            Material frameMat, Material doorMat)
        {
            GameObject gateRoot = new GameObject(name);
            gateRoot.transform.SetParent(parent, false);
            gateRoot.transform.localPosition = localPos;

            PuzzleGate gateComp = gateRoot.AddComponent<PuzzleGate>();

            // Trụ trái
            GameObject leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPillar.name = "Pillar_Left";
            leftPillar.transform.SetParent(gateRoot.transform, false);
            leftPillar.transform.localPosition = new Vector3(-2.1f, 1.75f, 0f);
            leftPillar.transform.localScale = new Vector3(0.5f, 3.5f, 0.5f);
            if (frameMat != null) leftPillar.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

            // Trụ phải
            GameObject rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPillar.name = "Pillar_Right";
            rightPillar.transform.SetParent(gateRoot.transform, false);
            rightPillar.transform.localPosition = new Vector3(2.1f, 1.75f, 0f);
            rightPillar.transform.localScale = new Vector3(0.5f, 3.5f, 0.5f);
            if (frameMat != null) rightPillar.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

            // Xà ngang trên
            GameObject topBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBeam.name = "Beam_Top";
            topBeam.transform.SetParent(gateRoot.transform, false);
            topBeam.transform.localPosition = new Vector3(0f, 3.6f, 0f);
            topBeam.transform.localScale = new Vector3(4.7f, 0.4f, 0.6f);
            if (frameMat != null) topBeam.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

            // Cánh cổng di chuyển (nâng lên trên khi mở)
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door_Moving";
            door.transform.SetParent(gateRoot.transform, false);
            door.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            door.transform.localScale = new Vector3(3.7f, 3.3f, 0.25f);
            if (doorMat != null) door.GetComponent<MeshRenderer>().sharedMaterial = doorMat;

            // Thiết lập SerializedProperty cho PuzzleGate
            SerializedObject gateSO = new SerializedObject(gateComp);
            gateSO.FindProperty("_movingPart").objectReferenceValue = door.transform;
            gateSO.FindProperty("_openOffset").vector3Value = new Vector3(0f, 3.4f, 0f);
            gateSO.FindProperty("_moveSpeed").floatValue = 3.5f;
            gateSO.ApplyModifiedPropertiesWithoutUndo();

            return gateRoot;
        }
    }
}
#endif
