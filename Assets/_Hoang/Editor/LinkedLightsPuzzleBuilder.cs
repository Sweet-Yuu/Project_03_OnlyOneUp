#if UNITY_EDITOR
using System;
using Hoang.Puzzles;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Hoang.Editor
{
    public static class LinkedLightsPuzzleBuilder
    {
        private const string PrefabPath = "Assets/_Hoang/Prefabs/Puzzles/LinkedLightsGatePuzzle.prefab";
        private const string ArtFolder = "Assets/_Hoang/Prefabs/Puzzles/LinkedLightsArt";
        private const string SharedArt = "Assets/_Hoang/Prefabs/Puzzles/SymbolCipherArt";

        [MenuItem("Tools/Hoang/Create Linked Lights Puzzle Prefab")]
        public static void BuildPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                Debug.Log("Linked lights prefab đã có; giữ nguyên các chỉnh sửa của bạn.", existing);
                return;
            }
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SharedArt + "/CipherVietnamese SDF.asset");
            Material stone = AssetDatabase.LoadAssetAtPath<Material>(SharedArt + "/CipherStone.mat");
            Material metal = AssetDatabase.LoadAssetAtPath<Material>(SharedArt + "/CipherMetal.mat");
            if (font == null || stone == null || metal == null)
                throw new InvalidOperationException("Cần giữ font và vật liệu có sẵn trong _Hoang/Prefabs/Puzzles/SymbolCipherArt.");
            if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder("Assets/_Hoang/Prefabs/Puzzles", "LinkedLightsArt");
            Material lit = CreateLampMaterial("LinkedLampOn", new Color(1f, 0.68f, 0.16f));
            Material dark = CreateLampMaterial("LinkedLampOff", new Color(0.035f, 0.055f, 0.08f));

            GameObject wrapper = new GameObject("LinkedLightsGatePuzzle");
            GameObject root = new GameObject("Puzzle");
            // Root thường tránh cơ chế auto-register của NGO sửa registry chung khi import prefab.
            root.transform.SetParent(wrapper.transform, false);
            try
            {
                root.AddComponent<NetworkObject>();
                LinkedLightsPuzzle puzzle = root.AddComponent<LinkedLightsPuzzle>();
                GameObject terminal = new GameObject("LightsConsole");
                terminal.transform.SetParent(root.transform, false);
                terminal.transform.localPosition = new Vector3(-4.7f, 0f, 0f);
                CreateBox("Board", terminal.transform, new Vector3(0f, 2.5f, 1f), new Vector3(4.5f, 5f, 0.3f), stone);
                CreateText("Title", terminal.transform, new Vector3(0f, 4.6f, 0.82f), new Vector2(4.2f, 0.5f), 3.5f,
                    "ĐÈN LIÊN KẾT", font);
                CreateText("Rules", terminal.transform, new Vector3(0f, 4.1f, 0.82f), new Vector2(4.2f, 0.45f), 1.7f,
                    "Chọn một ô sẽ đảo nó\nvà các ô sát cạnh ngang/dọc.", font);

                var initial = new LinkedLightsBoard(new[] { 1, 2, 4, 5, 9 });
                var lamps = new MeshRenderer[9];
                for (int index = 0; index < 9; index++)
                {
                    int button = index + 1;
                    Vector3 position = new Vector3((index % 3 - 1) * 1.05f, 3.35f - index / 3 * 1.05f, 0.77f);
                    GameObject lamp = CreateBox("Lamp_" + button, terminal.transform, position,
                        new Vector3(0.75f, 0.75f, 0.12f), initial.IsLit(button) ? lit : dark);
                    UnityEngine.Object.DestroyImmediate(lamp.GetComponent<Collider>());
                    lamps[index] = lamp.GetComponent<MeshRenderer>();
                    CreateText("Number_" + button, terminal.transform, position + new Vector3(0f, -0.47f, -0.08f),
                        new Vector2(0.6f, 0.25f), 1.8f, button.ToString(), font);
                }
                TMP_Text status = CreateText("Status", terminal.transform, new Vector3(0f, 0.35f, 0.82f),
                    new Vector2(4.2f, 0.65f), 1.7f, "Tắt hết đèn để mở cổng.\nĐứng gần và nhấn E để chơi.", font);
                BoxCollider trigger = terminal.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, 1f, -1f);
                trigger.size = new Vector3(4.5f, 3f, 4f);
                Rigidbody body = terminal.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                LinkedLightsConsole console = terminal.AddComponent<LinkedLightsConsole>();

                GameObject gateRoot = new GameObject("LightsGate");
                gateRoot.transform.SetParent(root.transform, false);
                gateRoot.transform.localPosition = new Vector3(0f, 0f, 5f);
                Transform slab = CreateBox("MovingGate", gateRoot.transform, new Vector3(0f, 1.8f, 0f),
                    new Vector3(5.8f, 3.6f, 0.45f), metal).transform;
                CreateBox("LeftPost", gateRoot.transform, new Vector3(-3.25f, 2f, 0f), new Vector3(0.7f, 4f, 0.9f), stone);
                CreateBox("RightPost", gateRoot.transform, new Vector3(3.25f, 2f, 0f), new Vector3(0.7f, 4f, 0.9f), stone);
                CreateBox("Lintel", gateRoot.transform, new Vector3(0f, 4.2f, 0f), new Vector3(7.2f, 0.4f, 0.9f), stone);
                PuzzleGate gate = gateRoot.AddComponent<PuzzleGate>();
                var gateData = new SerializedObject(gate);
                gateData.FindProperty("_movingPart").objectReferenceValue = slab;
                gateData.FindProperty("_openOffset").vector3Value = new Vector3(0f, 4f, 0f);
                gateData.ApplyModifiedPropertiesWithoutUndo();

                var puzzleData = new SerializedObject(puzzle);
                puzzleData.FindProperty("_gateBehaviour").objectReferenceValue = gate;
                puzzleData.FindProperty("_consolePoint").objectReferenceValue = terminal.transform;
                puzzleData.ApplyModifiedPropertiesWithoutUndo();
                var consoleData = new SerializedObject(console);
                consoleData.FindProperty("_puzzle").objectReferenceValue = puzzle;
                consoleData.FindProperty("_displayText").objectReferenceValue = status;
                SerializedProperty lampData = consoleData.FindProperty("_lamps");
                lampData.arraySize = 9;
                for (int index = 0; index < 9; index++) lampData.GetArrayElementAtIndex(index).objectReferenceValue = lamps[index];
                consoleData.ApplyModifiedPropertiesWithoutUndo();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Không lưu được prefab đèn liên kết.");
                Selection.activeObject = prefab;
                Debug.Log($"Created standalone linked lights puzzle: {PrefabPath}", prefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
        }

        [MenuItem("Tools/Hoang/Validate Linked Lights Puzzle Prefab")]
        public static void ValidatePrefab() => Hoang.Tests.LinkedLightsPrefabValidation.Run();

        public static void BuildAndValidate()
        {
            BuildPrefab();
            ValidatePrefab();
        }

        private static Material CreateLampMaterial(string name, Color color)
        {
            string path = ArtFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("Không tìm thấy shader URP Unlit.");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector3 position, Vector2 size, float fontSize, string content, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            TMP_Text text = go.GetComponent<TextMeshPro>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = new Color(0.94f, 0.96f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.text = content;
            return text;
        }
    }
}
#endif
