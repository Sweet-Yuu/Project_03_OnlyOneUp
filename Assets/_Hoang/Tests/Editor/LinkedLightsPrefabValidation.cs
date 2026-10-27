#if UNITY_EDITOR
using System;
using System.Reflection;
using Hoang.Puzzles;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Hoang.Tests
{
    public static class LinkedLightsPrefabValidation
    {
        public static void Run()
        {
            if (LinkedLightsBoardTests.Main() != 0) throw new Exception("Linked lights rules failed.");
            const string path = "Assets/_Hoang/Prefabs/Puzzles/LinkedLightsGatePuzzle.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab != null, "The standalone linked lights prefab must exist.");
            Check(prefab.GetComponent<NetworkObject>() == null, "The root must avoid automatic shared registry changes.");
            Check(prefab.GetComponentsInChildren<NetworkObject>(true).Length == 1, "Exactly one child NetworkObject is required.");
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script in linked lights prefab.");
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                LinkedLightsPuzzle puzzle = contents.GetComponentInChildren<LinkedLightsPuzzle>(true);
                LinkedLightsConsole console = contents.GetComponentInChildren<LinkedLightsConsole>(true);
                PuzzleGate gate = contents.GetComponentInChildren<PuzzleGate>(true);
                Check(puzzle != null && console != null && gate != null, "Coordinator, console and gate must be present.");
                var puzzleData = new SerializedObject(puzzle);
                var consoleData = new SerializedObject(console);
                Check(puzzleData.FindProperty("_gateBehaviour").objectReferenceValue == gate, "Coordinator must control this gate.");
                Check(puzzleData.FindProperty("_consolePoint").objectReferenceValue == console.transform, "Server interaction point must match console.");
                Check(consoleData.FindProperty("_puzzle").objectReferenceValue == puzzle, "Console must address this puzzle.");
                Check(consoleData.FindProperty("_displayText").objectReferenceValue != null, "Console status must be wired.");
                Check(console.GetComponent<Collider>().isTrigger && console.GetComponent<Rigidbody>().isKinematic,
                    "Console must have a working controller-compatible trigger.");
                SerializedProperty scramble = puzzleData.FindProperty("_scrambleButtons");
                var configuredSeed = new int[scramble.arraySize];
                for (int index = 0; index < configuredSeed.Length; index++) configuredSeed[index] = scramble.GetArrayElementAtIndex(index).intValue;
                new LinkedLightsBoard(configuredSeed);
                SerializedProperty lampData = consoleData.FindProperty("_lamps");
                Check(lampData.arraySize == 9, "Exactly nine lamp renderers must be wired.");
                var lamps = new MeshRenderer[9];
                for (int index = 0; index < 9; index++)
                {
                    lamps[index] = lampData.GetArrayElementAtIndex(index).objectReferenceValue as MeshRenderer;
                    Check(lamps[index] != null, "Each numbered lamp must have a renderer.");
                    Check(lamps[index].name == "Lamp_" + (index + 1), "Lamp wiring must follow the printed numbers.");
                }
                foreach (TMP_Text text in contents.GetComponentsInChildren<TMP_Text>(true))
                {
                    Check(text.font != null, "Every sign must have a font.");
                    text.ForceMeshUpdate(true);
                    Check(text.preferredHeight <= text.rectTransform.rect.height + 0.01f, "Text must fit its board: " + text.name);
                }

                // Dùng seed mẫu trên bản preview để kiểm tra wiring, không phụ thuộc câu đố người dùng đã chỉnh.
                int[] fixture = { 1, 2, 4, 5, 9 };
                scramble.arraySize = fixture.Length;
                for (int index = 0; index < fixture.Length; index++) scramble.GetArrayElementAtIndex(index).intValue = fixture[index];
                puzzleData.ApplyModifiedPropertiesWithoutUndo();
                // Không vào Play Mode hoặc lưu scene; mọi thay đổi preview bị bỏ khi unload.
                InvokeLifecycle(gate, "Awake");
                InvokeLifecycle(puzzle, "Awake");
                InvokeLifecycle(console, "Awake");
                InvokeLifecycle(console, "OnEnable");
                Check(puzzle.IsReady && puzzle.Mask == 351 && !gate.IsActive, "Offline starts at the documented board with a closed gate.");
                Color lit = LampColor(lamps[0]);
                puzzle.RequestPress(1);
                Color dark = LampColor(lamps[0]);
                Check(puzzle.Mask == 340 && dark.r < lit.r, "A real button press must update both state and lamp rendering.");
                puzzle.RequestReset();
                Check(puzzle.Mask == 351 && !gate.IsActive, "Reset must restore the initial unsolved board.");
                foreach (int button in new[] { 1, 2, 4, 5, 9 }) puzzle.RequestPress(button);
                Check(puzzle.IsSolved && gate.IsActive, "Solving the real coordinator must open the wired gate.");
                foreach (MeshRenderer lamp in lamps) Check(LampColor(lamp) == dark, "Every lamp must display off after solving.");
                puzzle.RequestReset();
                puzzle.RequestPress(1);
                Check(puzzle.IsSolved && gate.IsActive, "Later input/reset must preserve the open gate.");
                InvokeLifecycle(console, "OnDisable");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            Debug.Log("PASS: linked lights prefab, nine lamps, offline coordinator/visuals/gate/reset, references and text bounds.");
        }

        private static void InvokeLifecycle(MonoBehaviour component, string method)
        {
            component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
        }

        private static Color LampColor(MeshRenderer lamp)
        {
            var properties = new MaterialPropertyBlock();
            lamp.GetPropertyBlock(properties);
            return properties.GetColor(Shader.PropertyToID("_BaseColor"));
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
#endif
