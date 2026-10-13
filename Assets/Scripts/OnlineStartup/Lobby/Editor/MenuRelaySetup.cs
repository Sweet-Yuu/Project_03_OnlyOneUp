using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuRelaySetup
{
    private static TMP_FontAsset _font;
    private static Sprite _sprite;

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Docs/MenuStart/relay.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Setup(); }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Docs/MenuStart/relay-error.txt", e.ToString()); }
        };
    }

    [MenuItem("Tools/Menu/Setup Relay Lobby")]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Menu_Scene.unity" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Menu_Scene outside Play Mode.");
        var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MenuStartController>(true)).Single();
        if (menu.GetComponent<MenuRelayController>() != null) return;
        EditorSceneManager.SaveScene(scene, "Docs/MenuStart/Menu_BeforeRelay_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
        var rooms = scene.GetRootGameObjects().Single(g => g.name == "RoomSelectionCanvas");
        var joinRoot = scene.GetRootGameObjects().Single(g => g.name == "JoinCodeCanvas");
        var host = rooms.GetComponentsInChildren<Button>(true).Single(b => b.name == "HostButton");
        _font = host.GetComponentInChildren<TMP_Text>(true).font;
        _sprite = host.GetComponent<Image>().sprite;
        var root = new GameObject("RoomLobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        EditorUtility.CopySerialized(rooms.GetComponent<Canvas>(), root.GetComponent<Canvas>());
        EditorUtility.CopySerialized(rooms.GetComponent<CanvasScaler>(), root.GetComponent<CanvasScaler>());
        var panel = Box("LobbyPanel", root.transform, Vector2.zero, new Vector2(365, 290), new Color(.56f, .36f, .17f));
        Box("Header", panel.transform, new Vector2(0, 122), new Vector2(365, 46), new Color(.40f, .25f, .12f));
        var code = Label("RoomCode", panel.transform, "Code: ...", new Vector2(-85, 122), new Vector2(165, 32), 21);
        code.alignment = TextAlignmentOptions.MidlineLeft;
        var count = Label("PlayerCount", panel.transform, "Players: 0/2", new Vector2(111, 122), new Vector2(125, 32), 20);
        var copy = Button("CopyCodeButton", panel.transform, "Copy", new Vector2(20, 122), new Vector2(48, 29), 15);
        var names = new TMP_Text[2]; var avatars = new Image[2]; var rows = new GameObject[2];
        for (int i = 0; i < 2; i++)
        {
            var row = Box("PlayerRow" + i, panel.transform, new Vector2(0, 70 - i * 52), new Vector2(337, 43), new Color(.96f, .73f, .43f));
            rows[i] = row.gameObject;
            avatars[i] = Box("Avatar", row.transform, new Vector2(-144, 0), new Vector2(34, 34), Color.white);
            avatars[i].sprite = null;
            names[i] = Label("PlayerName", row.transform, "", new Vector2(20, 0), new Vector2(275, 35), 21);
            names[i].alignment = TextAlignmentOptions.MidlineLeft;
            names[i].richText = false;
            names[i].enableAutoSizing = true; names[i].fontSizeMin = 13; names[i].fontSizeMax = 21;
            row.gameObject.SetActive(false);
        }
        var status = Label("ConnectionStatus", panel.transform, "", new Vector2(0, -89), new Vector2(333, 85), 15);
        status.enableAutoSizing = true; status.fontSizeMin = 10; status.fontSizeMax = 15;
        var back = Button("BackButton", root.transform, "Back", new Vector2(0, -176), new Vector2(105, 32), 22);
        var view = root.AddComponent<RoomLobbyView>();
        var vs = new SerializedObject(view);
        Set(vs, "_codeLabel", code); Set(vs, "_countLabel", count); Set(vs, "_statusLabel", status);
        Set(vs, "_copyButton", copy); Set(vs, "_backButton", back);
        SetArray(vs, "_names", names); SetArray(vs, "_avatars", avatars); SetArray(vs, "_rows", rows);
        vs.ApplyModifiedPropertiesWithoutUndo();
        var network = new GameObject("MenuNetworkManager");
        var transport = network.AddComponent<UnityTransport>();
        var manager = network.AddComponent<NetworkManager>();
        manager.NetworkConfig.NetworkTransport = transport;
        manager.NetworkConfig.EnableSceneManagement = false;
        manager.NetworkConfig.ConnectionApproval = true;
        manager.NetworkConfig.PlayerPrefab = null;
        manager.NetworkConfig.ProtocolVersion = 1;
        var controller = menu.gameObject.AddComponent<MenuRelayController>();
        var cs = new SerializedObject(controller);
        Set(cs, "_networkManager", manager); Set(cs, "_transport", transport); Set(cs, "_roomSelectionRoot", rooms);
        Set(cs, "_joinRoot", joinRoot); Set(cs, "_lobbyRoot", root); Set(cs, "_view", view);
        cs.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(host.onClick, controller.Host);
        UnityEventTools.AddPersistentListener(copy.onClick, view.CopyCode);
        UnityEventTools.AddPersistentListener(back.onClick, controller.Back);
        var joinController = menu.GetComponent<JoinCodePanelController>();
        var joinEvent = (UnityEvent<string>)typeof(JoinCodePanelController).GetField("_joinRequested", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(joinController);
        UnityEventTools.AddPersistentListener(joinEvent, controller.Join);
        var backEvent = (UnityEvent)typeof(MenuRelayController).GetField("_backRequested", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
        UnityEventTools.AddPersistentListener(backEvent, menu.OpenRoomSelection);
        root.SetActive(false);
        foreach (var obj in new UnityEngine.Object[] { controller, joinController, host, copy, back, view, manager }) EditorUtility.SetDirty(obj);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        File.WriteAllText("Docs/MenuStart/relay-validation.txt", "Scene wired: Host/Join -> Relay, roster view, copy and back. Gameplay spawning disabled in menu.\n");
    }

    private static void Set(SerializedObject so, string name, UnityEngine.Object value) => so.FindProperty(name).objectReferenceValue = value;
    private static void SetArray(SerializedObject so, string name, UnityEngine.Object[] values)
    {
        var p = so.FindProperty(name); p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
    private static Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.layer = 5; go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.sprite = _sprite; image.type = Image.Type.Sliced; image.color = color;
        image.rectTransform.anchoredPosition = position; image.rectTransform.sizeDelta = size; return image;
    }
    private static TMP_Text Label(string name, Transform parent, string content, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.layer = 5; go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = _font; text.text = content; text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center; text.color = new Color(1, .97f, .84f); text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size; return text;
    }
    private static Button Button(string name, Transform parent, string content, Vector2 position, Vector2 size, float fontSize)
    {
        var image = Box(name, parent, position, size, new Color(.68f, .49f, .29f));
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Label("Label", image.transform, content, Vector2.zero, size, fontSize); return button;
    }
}
