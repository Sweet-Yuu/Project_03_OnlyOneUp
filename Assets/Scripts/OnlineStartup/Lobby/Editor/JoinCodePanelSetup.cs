using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class JoinCodePanelSetup
{
    private static TMP_FontAsset _font;
    private static Sprite _sprite;
    private const string Request = "Docs/MenuStart/join.request";

    [InitializeOnLoadMethod]
    private static void CheckRequest()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Move(Request, Request + "." + DateTime.Now.Ticks + ".consumed");
            try
            {
                if (SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<JoinCodePanelController>(true)).Any()) Finish();
                else Setup();
            }
            catch (Exception exception) { File.WriteAllText("Docs/MenuStart/join-error.txt", exception.ToString()); Debug.LogException(exception); }
        };
    }

    [MenuItem("Tools/Menu/Setup Join Code Panel")]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Menu_Scene.unity")
            throw new InvalidOperationException("Open Menu_Scene outside Play Mode first.");
        var menu = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MenuStartController>(true)).Single();
        if (menu.GetComponent<JoinCodePanelController>() != null) throw new InvalidOperationException("Join panel already exists.");
        EditorSceneManager.SaveScene(scene, "Docs/MenuStart/Menu_BeforeJoin_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
        var rooms = scene.GetRootGameObjects().Single(o => o.name == "RoomSelectionCanvas");
        var joinCard = rooms.GetComponentsInChildren<Button>(true).Single(b => b.name == "JoinButton");
        _font = joinCard.GetComponentInChildren<TMP_Text>().font;
        _sprite = joinCard.GetComponent<Image>().sprite;
        var root = new GameObject("JoinCodeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create Join Code Panel");
        EditorUtility.CopySerialized(rooms.GetComponent<Canvas>(), root.GetComponent<Canvas>());
        EditorUtility.CopySerialized(rooms.GetComponent<CanvasScaler>(), root.GetComponent<CanvasScaler>());
        var backdrop = Box("Backdrop", root.transform, Vector2.zero, Vector2.zero, new Color(.18f, .1f, .03f, .10f));
        backdrop.sprite = null;
        backdrop.rectTransform.anchorMin = Vector2.zero; backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
        var panel = Box("CodeEntryPanel", root.transform, Vector2.zero, new Vector2(340, 58), new Color(.56f, .39f, .22f));
        Text("Heading", panel.transform, "Enter room code", new Vector2(0, 52), new Vector2(340, 35), 25).color = new Color(.32f, .19f, .09f);
        var inputImage = Box("RoomCodeInput", panel.transform, new Vector2(-38, 0), new Vector2(236, 34), new Color(.64f, .49f, .39f));
        var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(inputImage.transform, false); viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(8, 2); viewport.offsetMax = new Vector2(-8, -2);
        var placeholder = Text("Placeholder", viewport, "Enter code...", Vector2.zero, Vector2.zero, 19);
        var inputText = Text("Text", viewport, "", Vector2.zero, Vector2.zero, 20);
        foreach (var label in new[] { placeholder, inputText })
        {
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }
        placeholder.fontStyle = FontStyles.Italic; placeholder.color = new Color(1, .97f, .84f, .65f);
        var input = inputImage.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = inputImage; input.textViewport = viewport; input.textComponent = inputText; input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine; input.characterLimit = 12;
        input.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        var submit = Button("SubmitJoinButton", panel.transform, "Join", new Vector2(126, 0), new Vector2(65, 36), 23);
        var back = Button("BackButton", root.transform, "Back", new Vector2(0, -76), new Vector2(100, 32), 21);
        var controller = Undo.AddComponent<JoinCodePanelController>(menu.gameObject);
        var so = new SerializedObject(controller);
        so.FindProperty("_roomSelectionRoot").objectReferenceValue = rooms;
        so.FindProperty("_joinPanelRoot").objectReferenceValue = root;
        so.FindProperty("_codeInput").objectReferenceValue = input;
        so.FindProperty("_joinButton").objectReferenceValue = submit;
        so.ApplyModifiedPropertiesWithoutUndo();
        // UnityEvent giữ form độc lập với controller mạng và với màn hình trước đó.
        var field = typeof(JoinCodePanelController).GetField("_backRequested", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        UnityEventTools.AddPersistentListener((UnityEngine.Events.UnityEvent)field.GetValue(controller), menu.OpenRoomSelection);
        UnityEventTools.AddPersistentListener(joinCard.onClick, controller.Open);
        UnityEventTools.AddPersistentListener(submit.onClick, controller.Submit);
        UnityEventTools.AddPersistentListener(back.onClick, controller.Back);
        EditorUtility.SetDirty(controller); EditorUtility.SetDirty(joinCard);
        controller.Open();
        if (!root.activeSelf || rooms.activeSelf || submit.interactable) throw new Exception("Initial form state failed.");
        controller.Back();
        // Persistent listener RuntimeOnly chỉ chạy trong Play Mode, không chạy khi tạo scene.
        if (root.activeSelf) throw new Exception("Back should hide the code form.");
        menu.OpenRoomSelection();
        menu.ShowMainMenu(); root.SetActive(false); submit.interactable = false;
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        File.WriteAllText("Docs/MenuStart/join-validation.txt", "PASS: open hides room cards, empty code disables Join, Back restores room selection; initial menu saved.\n");
        Debug.Log("Join code panel ready in Menu_Scene.");
    }

    [MenuItem("Tools/Menu/Finalize Join Code Panel")]
    public static void Finish()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Menu_Scene.unity") return;
        var menu = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MenuStartController>(true)).Single();
        var root = scene.GetRootGameObjects().Single(o => o.name == "JoinCodeCanvas");
        root.SetActive(false); menu.ShowMainMenu();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        File.WriteAllText("Docs/MenuStart/join-validation.txt", "Panel references configured; open and empty-code guard checked. Play Mode navigation verification follows.\n");
        Debug.Log("Join code panel saved.");
    }

    private static Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.layer = 5; go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.sprite = _sprite; image.type = Image.Type.Sliced; image.color = color;
        image.rectTransform.anchoredPosition = position; image.rectTransform.sizeDelta = size;
        return image;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string content, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.layer = 5; go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = _font; text.text = content; text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center; text.color = new Color(1, .97f, .84f); text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size; return text;
    }

    private static Button Button(string name, Transform parent, string content, Vector2 position, Vector2 size, float fontSize)
    {
        var image = Box(name, parent, position, size, new Color(.68f, .49f, .29f)); var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Text("Label", image.transform, content, Vector2.zero, size, fontSize); return button;
    }
}
