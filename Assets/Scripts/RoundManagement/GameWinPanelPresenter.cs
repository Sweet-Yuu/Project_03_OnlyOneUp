using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

// Tự nối bảng có sẵn trong scene, kể cả bảng đang tắt; không cần sửa prefab tổ.
public sealed class GameWinPanelPresenter : MonoBehaviour
{
    private GameObject _panel;
    private NetworkNest _nest;
    private Button _menuButton;
    private NetworkManager _manager;
    private bool _won;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMap_Online") return;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "GameWinPanel") continue;
                var owner = new GameObject("Game win panel presenter");
                SceneManager.MoveGameObjectToScene(owner, scene);
                var presenter = owner.AddComponent<GameWinPanelPresenter>();
                presenter._panel = child.gameObject;
                presenter._panel.SetActive(false);
                return;
            }
        Debug.LogWarning("MainMap_Online: không tìm thấy GameWinPanel. Hãy lưu bảng vào scene.");
    }

    private IEnumerator Start()
    {
        foreach (Button button in _panel.GetComponentsInChildren<Button>(true))
            if (button.name == "MenuButton") { _menuButton = button; break; }
        if (_menuButton != null) _menuButton.onClick.AddListener(ReturnToMenuTransition.Begin);
        else Debug.LogWarning("GameWinPanel: không tìm thấy MenuButton.", this);
        _manager = NetworkManager.Singleton;
        if (_manager != null) _manager.OnClientStopped += OnClientStopped;
        // Chỉ tìm trong giai đoạn chờ tổ spawn, không tìm mỗi frame gameplay.
        while (_nest == null)
        {
            foreach (NetworkNest nest in FindObjectsByType<NetworkNest>(FindObjectsSortMode.None))
                if (nest.gameObject.scene == gameObject.scene) { _nest = nest; break; }
            if (_nest == null) yield return new WaitForSecondsRealtime(.25f);
        }
        _nest.WinChanged += Show;
        Show(_nest.HasWon);
    }

    private void Show(bool won)
    {
        _won |= won;
        if (_won && !won) return;
        if (_panel != null) _panel.SetActive(won);
        if (!won) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (_menuButton != null) _menuButton.onClick.RemoveListener(ReturnToMenuTransition.Begin);
        if (_manager != null) _manager.OnClientStopped -= OnClientStopped;
        if (_nest != null) _nest.WinChanged -= Show;
    }

    private void OnClientStopped(bool wasHost)
    {
        if (_won) ReturnToMenuTransition.Begin();
    }
}
