#if UNITY_EDITOR
using System;
using System.Linq;
using Hoang.Puzzles;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Hoang.Editor
{
    public static class SymbolCipherPuzzleBuilder
    {
        private const string PrefabPath = "Assets/_Hoang/Prefabs/Puzzles/SymbolCipherGatePuzzle.prefab";
        private const string ArtFolder = "Assets/_Hoang/Prefabs/Puzzles/SymbolCipherArt";

        [MenuItem("Tools/Hoang/Create Symbol Cipher Puzzle Prefab")]
        public static void BuildPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                Debug.Log("Symbol cipher prefab đã có; giữ nguyên các chỉnh sửa của bạn.", existing);
                return;
            }
            EnsureFolder("Assets/_Hoang/Prefabs");
            EnsureFolder("Assets/_Hoang/Prefabs/Puzzles");
            EnsureFolder(ArtFolder);
            TMP_FontAsset font = CreateFont();
            Material stone = CreateMaterial("CipherStone", new Color(0.12f, 0.17f, 0.22f));
            Material metal = CreateMaterial("CipherMetal", new Color(0.55f, 0.36f, 0.14f));
            GameObject prefabRoot = new GameObject("SymbolCipherGatePuzzle");
            // NGO tự đăng ký prefab có NetworkObject ở root; wrapper giữ registry chung không bị thay đổi.
            GameObject root = new GameObject("Puzzle");
            root.transform.SetParent(prefabRoot.transform, false);
            try
            {
                root.AddComponent<NetworkObject>();
                SymbolCipherPuzzle puzzle = root.AddComponent<SymbolCipherPuzzle>();

                Transform board = CreateBox("ClueBoard", root.transform, new Vector3(-2.8f, 2.1f, 1.3f),
                    new Vector3(4.7f, 4.2f, 0.3f), stone).transform;
                // Giữ scale của text độc lập với mesh để thay kích thước bảng không làm méo glyph.
                TMP_Text clue = CreateText("ClueText", root.transform, board.localPosition + Vector3.back * 0.17f,
                    new Vector2(4.3f, 3.8f), 3.6f, font);
                clue.text = "GIẢI MÃ CỔNG ĐÁ\n\n" +
                    "<color=#F7C55B>●</color> + <color=#68D5EF>▲</color> = 7\n" +
                    "<color=#68D5EF>▲</color> + <color=#A4DE83>■</color> = 5\n" +
                    "<color=#F7C55B>●</color> + <color=#A4DE83>■</color> = 8\n\n" +
                    "Mỗi ký hiệu là một chữ số 0–9.\n" +
                    "Nhập theo thứ tự: <color=#F7C55B>●</color>  <color=#68D5EF>▲</color>  <color=#A4DE83>■</color>";

                GameObject terminal = new GameObject("CipherConsole");
                terminal.transform.SetParent(root.transform, false);
                terminal.transform.localPosition = new Vector3(1.5f, 0f, 0f);
                CreateBox("ConsoleStem", terminal.transform, new Vector3(0f, 0.65f, 0.5f), new Vector3(0.5f, 1.3f, 0.6f), metal);
                CreateBox("ConsoleBoard", terminal.transform, new Vector3(0f, 1.8f, 0.5f), new Vector3(3.1f, 2.3f, 0.3f), stone);
                TMP_Text display = CreateText("CodeDisplay", terminal.transform, new Vector3(0f, 1.8f, 0.33f),
                    new Vector2(2.9f, 2.1f), 2.8f, font);
                display.text = "BÀN NHẬP MÃ\n\nĐứng gần và nhấn E\nđể nhập 3 chữ số.";
                BoxCollider trigger = terminal.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, 1f, -1f);
                trigger.size = new Vector3(4.5f, 3f, 4f);
                Rigidbody body = terminal.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                SymbolCipherConsole console = terminal.AddComponent<SymbolCipherConsole>();

                GameObject gateRoot = new GameObject("CipherGate");
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
                puzzleData.FindProperty("_clueText").objectReferenceValue = clue;
                puzzleData.FindProperty("_consolePoint").objectReferenceValue = terminal.transform;
                puzzleData.ApplyModifiedPropertiesWithoutUndo();
                var consoleData = new SerializedObject(console);
                consoleData.FindProperty("_puzzle").objectReferenceValue = puzzle;
                consoleData.FindProperty("_displayText").objectReferenceValue = display;
                consoleData.ApplyModifiedPropertiesWithoutUndo();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Không lưu được prefab giải mã.");
                Selection.activeObject = prefab;
                Debug.Log($"Created standalone symbol cipher puzzle: {PrefabPath}", prefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabRoot);
            }
        }

        [MenuItem("Tools/Hoang/Validate Symbol Cipher Puzzle Prefab")]
        public static void ValidatePrefab() => Hoang.Tests.SymbolCipherPrefabValidation.Run();

        public static void BuildAndValidate()
        {
            BuildPrefab();
            ValidatePrefab();
        }

        private static TMP_FontAsset CreateFont()
        {
            string path = ArtFolder + "/CipherVietnamese SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (source == null) throw new InvalidOperationException("Không tìm thấy LiberationSans có sẵn trong dự án.");
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 64, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            if (font == null) throw new InvalidOperationException("Không tạo được font riêng cho puzzle.");
            string characters = new string(Enumerable.Range(32, 95).Select(value => (char)value).ToArray()) +
                "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư●▲■→–" +
                new string(Enumerable.Range(0x1EA0, 90).Select(value => (char)value).ToArray());
            if (!font.TryAddCharacters(characters, out string missing))
                throw new InvalidOperationException("Font puzzle thiếu glyph: " + missing);
            font.name = "CipherVietnamese SDF";
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            font.material.name = "CipherVietnamese Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures)
            {
                atlas.name = "CipherVietnamese Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
            return font;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = ArtFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Không tìm thấy shader URP Lit.");
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

        private static TMP_Text CreateText(string name, Transform parent, Vector3 position, Vector2 size, float fontSize, TMP_FontAsset font)
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
            return text;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
#endif
