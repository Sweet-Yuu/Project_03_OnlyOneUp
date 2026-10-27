#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Hoang.Puzzles;
using System.IO;

namespace Hoang.Editor
{
    // Công cụ Editor tự động sinh Prefab Cầu bập bênh thăng bằng co-op chuẩn mực.
    public static class SeesawBridgeBuilder
    {
        private const string RequestPath = "Temp/SeesawBridge/build_request.txt";
        private const string ResultPath = "Temp/SeesawBridge/build_result.txt";

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

        [MenuItem("Tools/Hoang/Build Seesaw Bridge Prefab")]
        public static void BuildPrefab()
        {
            string folderPath = "Assets/_Hoang/Prefabs/Puzzles";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string prefabPath = $"{folderPath}/SeesawBridgePuzzle.prefab";

            // Load vật liệu sẵn có trong dự án
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Wood.mat");
            Material steelMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Steel 1.mat");
            Material blackMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Black.mat");
            Material greenMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Full_Green.mat");
            Material redMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastMesh/Materials/Full_Red.mat");

            // 1. Root GameObject
            GameObject root = new GameObject("SeesawBridgePuzzle");
            NetworkObject netObj = root.AddComponent<NetworkObject>();
            SeesawBridge bridgeComp = root.AddComponent<SeesawBridge>();

            // 2. Trụ đỡ cố định ở giữa (Fulcrum Base)
            GameObject fulcrum = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fulcrum.name = "Fulcrum_Base";
            fulcrum.transform.SetParent(root.transform, false);
            fulcrum.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            fulcrum.transform.localScale = new Vector3(3.4f, 0.8f, 1.2f);
            if (steelMat != null) fulcrum.GetComponent<MeshRenderer>().sharedMaterial = steelMat;

            // 3. Bản cầu xoay (Plank Pivot)
            GameObject plankPivot = new GameObject("Plank_Pivot");
            plankPivot.transform.SetParent(root.transform, false);
            plankPivot.transform.localPosition = Vector3.zero;

            // Mặt cầu đi bộ chính
            GameObject plankSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plankSurface.name = "Plank_Surface";
            plankSurface.transform.SetParent(plankPivot.transform, false);
            plankSurface.transform.localPosition = Vector3.zero;
            plankSurface.transform.localScale = new Vector3(3f, 0.35f, 14f);
            if (woodMat != null) plankSurface.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Lan can bên trái
            GameObject railLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railLeft.name = "Rail_Left";
            railLeft.transform.SetParent(plankPivot.transform, false);
            railLeft.transform.localPosition = new Vector3(-1.45f, 0.35f, 0f);
            railLeft.transform.localScale = new Vector3(0.2f, 0.5f, 14f);
            if (blackMat != null) railLeft.GetComponent<MeshRenderer>().sharedMaterial = blackMat;

            // Lan can bên phải
            GameObject railRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railRight.name = "Rail_Right";
            railRight.transform.SetParent(plankPivot.transform, false);
            railRight.transform.localPosition = new Vector3(1.45f, 0.35f, 0f);
            railRight.transform.localScale = new Vector3(0.2f, 0.5f, 14f);
            if (blackMat != null) railRight.GetComponent<MeshRenderer>().sharedMaterial = blackMat;

            // Đèn chỉ báo cảnh báo ở đầu và cuối cầu
            GameObject lampFront = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lampFront.name = "Lamp_Front";
            lampFront.transform.SetParent(plankPivot.transform, false);
            lampFront.transform.localPosition = new Vector3(0f, 0.22f, 6.5f);
            lampFront.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
            Object.DestroyImmediate(lampFront.GetComponent<Collider>());
            MeshRenderer lampFrontRend = lampFront.GetComponent<MeshRenderer>();
            if (greenMat != null) lampFrontRend.sharedMaterial = greenMat;

            GameObject lampBack = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lampBack.name = "Lamp_Back";
            lampBack.transform.SetParent(plankPivot.transform, false);
            lampBack.transform.localPosition = new Vector3(0f, 0.22f, -6.5f);
            lampBack.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
            Object.DestroyImmediate(lampBack.GetComponent<Collider>());
            MeshRenderer lampBackRend = lampBack.GetComponent<MeshRenderer>();
            if (greenMat != null) lampBackRend.sharedMaterial = greenMat;

            // Vùng Trigger nhận diện người chơi trên cầu
            GameObject triggerObj = new GameObject("Trigger_Zone");
            triggerObj.transform.SetParent(plankPivot.transform, false);
            triggerObj.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            BoxCollider triggerBox = triggerObj.AddComponent<BoxCollider>();
            triggerBox.isTrigger = true;
            triggerBox.size = new Vector3(2.8f, 2.5f, 13.8f);

            Rigidbody rb = triggerObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Relay chuyển tiếp va chạm tới SeesawBridge
            SeesawTriggerRelay relay = triggerObj.AddComponent<SeesawTriggerRelay>();
            SerializedObject relaySO = new SerializedObject(relay);
            relaySO.FindProperty("_bridge").objectReferenceValue = bridgeComp;
            relaySO.ApplyModifiedPropertiesWithoutUndo();

            // 4. Thiết lập thuộc tính Inspector bằng SerializedObject
            SerializedObject bridgeSO = new SerializedObject(bridgeComp);
            bridgeSO.FindProperty("_plankTransform").objectReferenceValue = plankPivot.transform;
            bridgeSO.FindProperty("_localTiltAxis").vector3Value = Vector3.right;
            bridgeSO.FindProperty("_maxTiltAngle").floatValue = 20f;
            bridgeSO.FindProperty("_tiltSensitivity").floatValue = 5f;
            bridgeSO.FindProperty("_tiltSpeed").floatValue = 12f;
            bridgeSO.FindProperty("_returnSpeed").floatValue = 8f;
            bridgeSO.FindProperty("_warningAngleThreshold").floatValue = 14f;
            bridgeSO.FindProperty("_safeMaterial").objectReferenceValue = greenMat;
            bridgeSO.FindProperty("_dangerMaterial").objectReferenceValue = redMat;

            SerializedProperty indicatorsProp = bridgeSO.FindProperty("_indicatorRenderers");
            indicatorsProp.arraySize = 2;
            indicatorsProp.GetArrayElementAtIndex(0).objectReferenceValue = lampFrontRend;
            indicatorsProp.GetArrayElementAtIndex(1).objectReferenceValue = lampBackRend;

            bridgeSO.ApplyModifiedPropertiesWithoutUndo();

            // 5. Lưu Prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SeesawBridgeBuilder] Đã tạo thành công Prefab tại: {prefabPath}");
        }
    }
}
#endif
