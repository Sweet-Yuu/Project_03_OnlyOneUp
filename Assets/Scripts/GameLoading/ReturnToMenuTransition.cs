using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ReturnToMenuTransition : MonoBehaviour
{
    private const string MenuScene = "Menu_Scene";
    private static bool _running;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => _running = false;

    public static void Begin()
    {
        BeginTransition(false);
    }

    public static void BeginCovered()
    {
        BeginTransition(true);
    }

    private static void BeginTransition(bool alreadyCovered)
    {
        if (_running) return;
        if (!Application.CanStreamedLevelBeLoaded(MenuScene))
        {
            Debug.LogError("Không tìm thấy Menu_Scene trong Build Profiles Scene List.");
            return;
        }
        _running = true;
        var root = new GameObject("Return to menu transition", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        root.AddComponent<ReturnToMenuTransition>().StartCoroutine(Transition(root, alreadyCovered));
    }

    private static IEnumerator Transition(GameObject root, bool alreadyCovered)
    {
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        Color color = new Color(1f, .65f, 0f, 1f);
        foreach (CircularRevealGraphic existing in FindObjectsByType<CircularRevealGraphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            // [Kitchen_Map]: Lấy màu hiệu ứng CircularReveal Graphic từ Kitchen_Map (hoặc MainMap_Online) khi người chơi thoát về Menu
            if (existing.gameObject.scene.name == "MainMap_Online" || existing.gameObject.scene.name == "Kitchen_Map") { color = existing.color; break; }
        color.a = 1f;
        var coverObject = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(CircularRevealGraphic));
        coverObject.transform.SetParent(root.transform, false);
        var cover = coverObject.GetComponent<CircularRevealGraphic>();
        cover.rectTransform.anchorMin = Vector2.zero;
        cover.rectTransform.anchorMax = Vector2.one;
        cover.rectTransform.offsetMin = cover.rectTransform.offsetMax = Vector2.zero;
        cover.color = color;
        cover.raycastTarget = true;
        cover.SetProgress(alreadyCovered ? 0f : 1f);
        yield return null;

        if (!alreadyCovered)
        {
            // Che hết màn hình trước khi ngắt mạng làm biến mất các NetworkObject.
            float closing = 0f;
            while (closing < .35f)
            {
                closing += Time.unscaledDeltaTime;
                cover.SetProgress(1f - Mathf.SmoothStep(0f, 1f, closing / .35f));
                yield return null;
            }
            cover.SetProgress(0f);
        }

        // Ngắt hoàn toàn phiên cũ trước khi menu tạo NetworkManager mới.
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null)
        {
            if (!manager.ShutdownInProgress) manager.Shutdown();
            float deadline = Time.realtimeSinceStartup + 15f;
            while (manager != null && (manager.ShutdownInProgress || manager.IsListening))
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Debug.LogError("Chưa ngắt được phiên mạng. Hãy thử Main Menu lại.");
                    Destroy(root);
                    yield break;
                }
                yield return null;
            }
            if (manager != null) Destroy(manager.gameObject);
            yield return null;
        }
        Time.timeScale = 1f;
        AsyncOperation loading = SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);
        while (!loading.isDone) yield return null;
        yield return null;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        float elapsed = 0f;
        while (elapsed < 1.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            cover.SetProgress(Mathf.SmoothStep(0f, 1f, elapsed / 1.2f));
            yield return null;
        }
        Destroy(root);
    }

    private void OnDestroy() => _running = false;
}
