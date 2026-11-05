using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class InGameSettingsShortcut : MonoBehaviour
{
    private SettingPanelController settingsPanel;
    private TutorialPanelController tutorialPanel;
    private PlayerInputActions inputActions;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMap_Online" && scene.name != "Kitchen_Map") return;

        SettingPanelController panel = null;
        TutorialPanelController tutorial = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<InGameSettingsShortcut>(true) != null) return;
            if (panel == null) panel = root.GetComponentInChildren<SettingPanelController>(true);
            if (tutorial == null) tutorial = root.GetComponentInChildren<TutorialPanelController>(true);
        }

        if (panel == null)
        {
            Debug.LogWarning($"Không tìm thấy SettingPanelController trong {scene.name}.");
            return;
        }

        // Panel đang tắt không nhận input; đặt bộ lắng nghe trên object riêng của scene.
        var owner = new GameObject("In-game settings shortcut");
        SceneManager.MoveGameObjectToScene(owner, scene);
        var shortcut = owner.AddComponent<InGameSettingsShortcut>();
        shortcut.settingsPanel = panel;
        shortcut.tutorialPanel = tutorial;
    }

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.BackStep.performed += ToggleSettings;
    }

    private void OnEnable() => inputActions.Player.BackStep.Enable();
    private void OnDisable() => inputActions.Player.BackStep.Disable();

    private void ToggleSettings(InputAction.CallbackContext context)
    {
        if (!Application.isFocused || settingsPanel == null) return;
        // Tutorial đã xử lý Esc để đóng bảng; không mở thêm settings bằng cùng lần nhấn.
        if (tutorialPanel != null && tutorialPanel.isActiveAndEnabled && tutorialPanel.IsOpen) return;

        if (settingsPanel.gameObject.activeSelf)
            settingsPanel.ClosePanel();
        else
        {
            settingsPanel.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnDestroy()
    {
        inputActions.Player.BackStep.performed -= ToggleSettings;
        inputActions.Dispose();
    }
}
