using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerNameplateSetup
{
    private const string Path = "Assets/Prefabs/NetWorkPlayer.prefab";
    private const string Request = "Temp/NameplateSetup/request.txt";

    [InitializeOnLoadMethod]
    private static void Register() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Setup(); }
        catch (System.Exception error)
        {
            File.WriteAllText("Temp/NameplateSetup/result.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    [MenuItem("Tools/Player/Complete Nameplate")]
    public static void Setup()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool open = stage != null && stage.assetPath == Path;
        var root = open ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(Path);
        try
        {
            var canvasTransform = root.transform.Find("NamePlaceCanvas") as RectTransform;
            if (canvasTransform == null) throw new System.Exception("NamePlaceCanvas not found");
            var background = canvasTransform.Find("Background") as RectTransform;
            var label = background.GetComponentInChildren<TMP_Text>(true);
            Undo.RegisterFullObjectHierarchyUndo(root, "Complete player nameplate");
            canvasTransform.localRotation = Quaternion.identity;
            canvasTransform.localScale = Vector3.one * .025f;
            canvasTransform.sizeDelta = new Vector2(240, 55);
            canvasTransform.localPosition = new Vector3(0, 9.3f, 0);
            canvasTransform.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var raycaster = canvasTransform.GetComponent<GraphicRaycaster>();
            if (raycaster != null) Object.DestroyImmediate(raycaster);
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = background.offsetMax = Vector2.zero;
            var image = background.GetComponent<Image>();
            image.color = new Color(.28f, .15f, .065f, .92f);
            image.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10, 4);
            label.rectTransform.offsetMax = new Vector2(-10, -4);
            label.text = "Player";
            label.richText = false;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14;
            label.fontSizeMax = 32;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1, .94f, .75f);
            var plate = canvasTransform.GetComponent<PlayerNameplate>();
            if (plate == null) plate = canvasTransform.gameObject.AddComponent<PlayerNameplate>();
            var serialized = new SerializedObject(plate);
            serialized.FindProperty("_identity").objectReferenceValue = root.GetComponent<NetworkPlayerIdentity>();
            serialized.FindProperty("_nameText").objectReferenceValue = label;
            serialized.FindProperty("_background").objectReferenceValue = background.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (canvasTransform.GetComponent<NameplateBillboard>() == null)
                canvasTransform.gameObject.AddComponent<NameplateBillboard>();
            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, Path);
            if (open)
            {
                Selection.activeGameObject = canvasTransform.gameObject;
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
            }
            Directory.CreateDirectory("Temp/NameplateSetup");
            File.WriteAllText("Temp/NameplateSetup/result.txt", "PASS: configured and saved nameplate; active prefab stage=" + open);
        }
        finally { if (!open) PrefabUtility.UnloadPrefabContents(root); }
    }
}
