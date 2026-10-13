using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Tạo UI có thể chỉnh trong scene, không sinh giao diện bằng code lúc chạy.</summary>
public static class MenuStartSetup
{
    private const string ScenePath = "Assets/Scenes/Menu_Scene.unity";
    private const string RequestPath = "Docs/MenuStart/setup.request";
    private static TMP_FontAsset _font;
    private static Sprite _panelSprite;

    [InitializeOnLoadMethod]
    private static void CheckRequest()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Move(RequestPath, RequestPath + ".consumed");
            try { Setup(); }
            catch (Exception exception)
            {
                File.WriteAllText("Docs/MenuStart/setup-error.txt", exception.ToString());
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("Tools/Menu/Setup Start Panel")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before setting up the menu.");
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            if (activeScene.isDirty)
                throw new InvalidOperationException("Save the current scene before opening Menu_Scene.");
            activeScene = EditorSceneManager.OpenScene(ScenePath);
        }
        if (activeScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MenuStartController>(true)).Any())
            throw new InvalidOperationException("Menu is already set up; edit the existing references instead.");

        Directory.CreateDirectory("Docs/MenuStart");
        string backup = "Docs/MenuStart/Menu_Scene_BeforeStart_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        EditorSceneManager.SaveScene(activeScene, backup, true);
        var buttons = activeScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true));
        var start = buttons.Single(button => button.GetComponentInChildren<TMP_Text>(true)?.text.Trim().Equals("Start game", StringComparison.OrdinalIgnoreCase) == true);
        var mainRoot = start.transform.parent.gameObject;
        var canvas = start.GetComponentInParent<Canvas>();
        _font = start.GetComponentInChildren<TMP_Text>().font;
        _panelSprite = start.GetComponent<Image>().sprite;

        var roomRoot = new GameObject("RoomSelectionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(roomRoot, "Create Room Selection");
        var roomCanvas = roomRoot.GetComponent<Canvas>();
        roomCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        roomCanvas.sortingOrder = canvas.sortingOrder + 10;
        var scaler = roomRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight = .5f;

        var shade = Image("Backdrop", roomRoot.transform, Vector2.zero, Vector2.zero, new Color(.18f, .10f, .03f, .10f), false);
        Stretch(shade.rectTransform);
        var panel = Image("OnlineGamePanel", roomRoot.transform, Vector2.zero, new Vector2(620, 260), new Color(.56f, .39f, .22f));
        panel.rectTransform.localScale = new Vector3(.68f, .68f, 1f);
        Image("Header", panel.transform, new Vector2(0, 106), new Vector2(616, 44), new Color(.43f, .29f, .16f));
        Label("Title", panel.transform, "Online game", new Vector2(-180, 106), new Vector2(210, 36), 28, TextAlignmentOptions.MidlineLeft);
        var host = Card("HostButton", panel.transform, "Host", "Create a room", -149);
        var join = Card("JoinButton", panel.transform, "Join", "Enter a room code", 149);
        var back = Image("BackButton", roomRoot.transform, new Vector2(0, -120), new Vector2(150, 42), new Color(.43f, .29f, .16f)).gameObject.AddComponent<Button>();
        back.transform.localScale = new Vector3(.8f, .8f, 1f);
        Label("Label", back.transform, "Back", Vector2.zero, new Vector2(140, 38), 25);
        StyleButton(back);

        var controller = Undo.AddComponent<MenuStartController>(canvas.gameObject);
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("_mainMenuRoot").objectReferenceValue = mainRoot;
        serialized.FindProperty("_roomSelectionRoot").objectReferenceValue = roomRoot;
        serialized.FindProperty("_startButton").objectReferenceValue = start;
        serialized.FindProperty("_firstRoomButton").objectReferenceValue = host;
        serialized.FindProperty("_eventSystem").objectReferenceValue = activeScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).Single();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Undo.RecordObject(start, "Bind Start");
        UnityEventTools.AddPersistentListener(start.onClick, controller.OpenRoomSelection);
        UnityEventTools.AddPersistentListener(back.onClick, controller.ShowMainMenu);
        roomRoot.SetActive(false);
        mainRoot.SetActive(true);

        // Đưa menu mới lên đầu nhưng giữ lại các scene cũ phục vụ thử mạng.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
            .Concat(EditorBuildSettings.scenes.Where(scene => scene.path != ScenePath)).ToArray();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();
        Validate(controller, start, back);
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("Menu Start setup complete. Host/Join connection actions are reserved for the next step.");
    }

    private static void Validate(MenuStartController controller, Button start, Button back)
    {
        controller.ShowMainMenu();
        if (controller.IsRoomSelectionOpen) throw new Exception("Initial panel should be hidden.");
        for (int i = 0; i < 3; i++)
        {
            controller.OpenRoomSelection();
            if (!controller.IsRoomSelectionOpen || start.gameObject.activeInHierarchy) throw new Exception("Open failed.");
            controller.ShowMainMenu();
            if (controller.IsRoomSelectionOpen || !start.gameObject.activeInHierarchy) throw new Exception("Back failed.");
        }
        if (start.onClick.GetPersistentMethodName(start.onClick.GetPersistentEventCount() - 1) != nameof(MenuStartController.OpenRoomSelection) ||
            back.onClick.GetPersistentMethodName(0) != nameof(MenuStartController.ShowMainMenu)) throw new Exception("Button binding failed.");
        File.WriteAllText("Docs/MenuStart/validation.txt", "PASS: initial menu, open/back x3, persistent button bindings, Menu_Scene first in build settings.\n");
    }

    private static Button Card(string name, Transform parent, string title, string subtitle, float x)
    {
        var image = Image(name, parent, new Vector2(x, -12), new Vector2(275, 150), new Color(.98f, .80f, .56f));
        var button = image.gameObject.AddComponent<Button>();
        StyleButton(button);
        Label("Title", image.transform, title, new Vector2(0, 19), new Vector2(250, 60), 40);
        Label("Description", image.transform, subtitle, new Vector2(0, -34), new Vector2(250, 36), 20);
        return button;
    }

    private static void StyleButton(Button button)
    {
        button.targetGraphic = button.GetComponent<Image>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1, .94f, .80f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.88f, .72f, .5f);
        button.colors = colors;
    }

    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool rounded = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        if (rounded) { image.sprite = _panelSprite; image.type = UnityEngine.UI.Image.Type.Sliced; }
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return image;
    }

    private static void Label(string name, Transform parent, string content, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = _font; text.text = content; text.fontSize = fontSize;
        text.color = new Color(1, .97f, .84f); text.alignment = alignment; text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
