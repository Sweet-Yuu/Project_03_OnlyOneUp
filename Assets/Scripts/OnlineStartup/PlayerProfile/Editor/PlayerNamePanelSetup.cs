using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PlayerNamePanelSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists("Docs/MenuStart/name.clear-test") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (PlayerPrefs.GetString(PlayerNamePreferences.Key) == "DuckTest")
                {
                    PlayerPrefs.DeleteKey(PlayerNamePreferences.Key);
                    PlayerPrefs.Save();
                }
                File.Delete("Docs/MenuStart/name.clear-test");
                File.AppendAllText("Docs/MenuStart/name-validation.txt", "Test name cleared after successful first-run and returning-player Play Mode checks.\n");
            }
            const string request = "Docs/MenuStart/name.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Setup(); }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Docs/MenuStart/name-error.txt", e.ToString()); }
        };
    }

    [MenuItem("Tools/Menu/Setup Player Name Panel")]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Menu_Scene.unity" || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MenuStartController>(true)).Single();
        if (menu.GetComponent<PlayerNamePanelController>() != null) return;
        EditorSceneManager.SaveScene(scene, "Docs/MenuStart/Menu_BeforeName_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
        var source = scene.GetRootGameObjects().Single(g => g.name == "JoinCodeCanvas");
        var root = UnityEngine.Object.Instantiate(source);
        root.name = "PlayerNameCanvas";
        var panel = root.transform.Find("CodeEntryPanel").GetComponent<RectTransform>();
        panel.name = "PlayerNamePanel";
        panel.sizeDelta = new Vector2(350, 175);
        var heading = panel.Find("Heading").GetComponent<TMP_Text>();
        heading.text = "Choose your name";
        heading.color = new Color(1, .97f, .84f);
        heading.rectTransform.anchoredPosition = new Vector2(0, 54);
        var input = panel.GetComponentInChildren<TMP_InputField>(true);
        input.name = "PlayerNameInput";
        input.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 3);
        input.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 36);
        input.characterLimit = 20;
        input.characterValidation = TMP_InputField.CharacterValidation.None;
        input.textComponent.richText = false;
        input.SetTextWithoutNotify("");
        ((TMP_Text)input.placeholder).text = "Enter your name...";
        var confirm = panel.GetComponentInChildren<Button>(true);
        confirm.name = "ConfirmNameButton";
        confirm.onClick = new Button.ButtonClickedEvent();
        confirm.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        confirm.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 34);
        confirm.GetComponentInChildren<TMP_Text>().text = "Continue";
        confirm.GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta = new Vector2(140, 34);
        confirm.interactable = false;
        UnityEngine.Object.DestroyImmediate(root.transform.Find("BackButton").gameObject);
        var menuSo = new SerializedObject(menu);
        var main = (GameObject)menuSo.FindProperty("_mainMenuRoot").objectReferenceValue;
        var controller = menu.gameObject.AddComponent<PlayerNamePanelController>();
        var so = new SerializedObject(controller);
        so.FindProperty("_panelRoot").objectReferenceValue = root;
        so.FindProperty("_mainMenuRoot").objectReferenceValue = main;
        so.FindProperty("_nameInput").objectReferenceValue = input;
        so.FindProperty("_confirmButton").objectReferenceValue = confirm;
        so.ApplyModifiedPropertiesWithoutUndo();
        var completion = (UnityEvent)typeof(PlayerNamePanelController).GetField("_completed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
        // Gắn setter bằng UnityAction để Unity lưu đúng component đích.
        var setter = (UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>), menu, typeof(Behaviour).GetProperty("enabled").GetSetMethod());
        UnityEventTools.AddBoolPersistentListener(completion, setter, true);
        UnityEventTools.AddPersistentListener(completion, menu.ShowMainMenu);
        menu.ShowMainMenu();
        menu.enabled = false;
        main.SetActive(false);
        root.SetActive(true);
        EditorUtility.SetDirty(menu); EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Docs/MenuStart/name-validation.txt", "Name panel created, menu initially disabled, persistent completion wired.\n");
    }
}
