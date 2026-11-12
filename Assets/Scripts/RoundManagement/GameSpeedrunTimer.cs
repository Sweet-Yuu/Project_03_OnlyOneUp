using UnityEngine;
using UnityEngine.SceneManagement;

// Đếm thời gian chơi liên tục từ Kitchen_Map sang MainMap_Online mà không reset giữa chặng.
public sealed class GameSpeedrunTimer : MonoBehaviour
{
    public const string KitchenSceneName = "Kitchen_Map";
    public const string MainMapSceneName = "MainMap_Online";

    private static GameSpeedrunTimer instance;

    private NetworkGameEntryGate entryGate;
    private NetworkNest activeNest;
    private bool hasActiveRun;
    private bool runCompleted;
    private float elapsedSeconds;
    private float nextLookupAt;

    public static GameSpeedrunTimer Instance => instance;
    public float ElapsedSeconds => elapsedSeconds;
    public string CurrentSceneName { get; private set; } = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsGameplayScene(scene.name))
        {
            // Rời khỏi các màn chơi (về Menu/WinOver...) thì đặt lại đồng hồ cho lượt mới.
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
            }
            return;
        }

        EnsureInstance();
        instance.HandleSceneEntered(scene);
    }

    public static bool IsGameplayScene(string sceneName) =>
        sceneName == KitchenSceneName || sceneName == MainMapSceneName;

    public static string FormatTime(float totalSeconds, bool includeCentiseconds)
    {
        float clamped = Mathf.Max(0f, totalSeconds);
        int totalWhole = Mathf.FloorToInt(clamped);
        int hours = totalWhole / 3600;
        int minutes = (totalWhole % 3600) / 60;
        int seconds = totalWhole % 60;

        if (includeCentiseconds)
        {
            int centiseconds = Mathf.FloorToInt((clamped - totalWhole) * 100f);
            return hours > 0
                ? $"{hours:00}:{minutes:00}:{seconds:00}.{centiseconds:00}"
                : $"{minutes:00}:{seconds:00}.{centiseconds:00}";
        }

        return hours > 0
            ? $"{hours:00}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
    }

    public void NotifyRoundWon()
    {
        runCompleted = true;
    }

    public void NotifyRoundRestarted()
    {
        if (!runCompleted) return;
        elapsedSeconds = 0f;
        runCompleted = false;
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        var host = new GameObject("Game Speedrun Timer");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<GameSpeedrunTimer>();
    }

    private void HandleSceneEntered(Scene scene)
    {
        CurrentSceneName = scene.name;
        entryGate = null;
        activeNest = null;
        nextLookupAt = 0f;

        // Chỉ reset khi bắt đầu lượt mới ở Kitchen_Map (hoặc test trực tiếp MainMap_Online khi chưa có lượt).
        if (scene.name == KitchenSceneName || !hasActiveRun)
        {
            elapsedSeconds = 0f;
            hasActiveRun = true;
        }

        runCompleted = false;
        RefreshSceneReferences(scene);
    }

    private void Update()
    {
        if (!hasActiveRun || runCompleted) return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!IsGameplayScene(activeScene.name)) return;
        CurrentSceneName = activeScene.name;

        if ((entryGate == null || activeNest == null) && Time.unscaledTime >= nextLookupAt)
        {
            nextLookupAt = Time.unscaledTime + 0.25f;
            RefreshSceneReferences(activeScene);
        }

        // Chờ màn loading mở hoàn toàn mới bắt đầu tính giờ để tránh tính thời gian đồng bộ mạng.
        if (entryGate != null && !entryGate.IsOpen) return;
        if (activeNest != null && activeNest.HasWon)
        {
            runCompleted = true;
            return;
        }

        if (Time.timeScale > 0f) elapsedSeconds += Time.deltaTime;
    }

    private void RefreshSceneReferences(Scene scene)
    {
        if (entryGate == null)
        {
            foreach (NetworkGameEntryGate gate in FindObjectsByType<NetworkGameEntryGate>(FindObjectsSortMode.None))
            {
                if (gate.gameObject.scene == scene)
                {
                    entryGate = gate;
                    break;
                }
            }
        }

        if (activeNest == null)
        {
            foreach (NetworkNest nest in FindObjectsByType<NetworkNest>(FindObjectsSortMode.None))
            {
                if (nest.gameObject.scene == scene)
                {
                    activeNest = nest;
                    break;
                }
            }
        }
    }
}

