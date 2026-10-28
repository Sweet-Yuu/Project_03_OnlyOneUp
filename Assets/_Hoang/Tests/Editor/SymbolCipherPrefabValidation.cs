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
    public static class SymbolCipherPrefabValidation
    {
        public static void Run()
        {
            if (SymbolCipherLockTests.Main() != 0) throw new Exception("Cipher rules failed.");
            MethodInfo resultRpc = typeof(SymbolCipherPuzzle).GetMethod("SubmissionResultRpc", BindingFlags.Instance | BindingFlags.NonPublic);
            RpcAttribute resultPolicy = resultRpc.GetCustomAttribute<RpcAttribute>();
            Check(resultPolicy.InvokePermission == RpcInvokePermission.Server, "Only the server may send submission results.");
            string path = "Assets/_Hoang/Prefabs/Puzzles/SymbolCipherGatePuzzle.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab != null, "The standalone symbol cipher prefab must exist.");
            Check(prefab.GetComponent<NetworkObject>() == null, "The wrapper must avoid automatic shared network registry changes.");
            Check(prefab.GetComponentInChildren<NetworkObject>(true) != null, "The scene-placed puzzle needs a child NetworkObject.");
            SymbolCipherPuzzle puzzle = prefab.GetComponentInChildren<SymbolCipherPuzzle>(true);
            Check(puzzle != null, "The prefab needs the symbol cipher coordinator.");
            var serialized = new SerializedObject(puzzle);
            Check(serialized.FindProperty("_gateBehaviour").objectReferenceValue is IPuzzleInteractable,
                "The lock must control a puzzle interactable.");
            Check(serialized.FindProperty("_clueText").objectReferenceValue != null, "Clue board must be wired.");
            Check(serialized.FindProperty("_consolePoint").objectReferenceValue != null, "Server interaction point must be wired.");
            Check(serialized.FindProperty("_circleTriangleSum").intValue == 7 &&
                serialized.FindProperty("_triangleSquareSum").intValue == 5 &&
                serialized.FindProperty("_circleSquareSum").intValue == 8, "Default clues must match the tested solution.");
            SymbolCipherConsole console = prefab.GetComponentInChildren<SymbolCipherConsole>(true);
            Check(console != null, "Console must be present.");
            var consoleData = new SerializedObject(console);
            Check(consoleData.FindProperty("_puzzle").objectReferenceValue == puzzle, "Console must address this puzzle.");
            Check(consoleData.FindProperty("_displayText").objectReferenceValue != null, "Console display must be wired.");
            Check(console.GetComponent<Collider>().isTrigger, "The interaction region must be a trigger.");
            Check(console.GetComponent<Rigidbody>().isKinematic, "The trigger must work with controller and rigidbody players.");
            foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                Check(text.font != null && text.rectTransform.rect.width > 0f && text.rectTransform.rect.height > 0f,
                    "All signs need a font and nonzero bounds.");
                foreach (char character in "●▲■→–ĐđẢảẾếẬậỜờỐốỮữ")
                    Check(text.font.HasCharacter(character), "The local font must contain puzzle and Vietnamese glyphs.");
            }
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (TMP_Text text in contents.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.ForceMeshUpdate(true);
                    Check(text.preferredHeight <= text.rectTransform.rect.height + 0.01f,
                        "Sign text must fit vertically inside its board: " + text.name);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "No missing scripts allowed.");
            Debug.Log("PASS: symbol cipher prefab, gate/console references, trigger, default clues, fonts and script references.");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
#endif
