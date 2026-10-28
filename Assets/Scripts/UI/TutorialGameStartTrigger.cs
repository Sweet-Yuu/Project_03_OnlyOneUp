using System.Collections;
using UnityEngine;

/// <summary>
/// Lắng nghe thời điểm trận đấu bắt đầu sau khi cả hai người chơi đã sẵn sàng và mở panel hướng dẫn.
/// </summary>
public sealed class TutorialGameStartTrigger : MonoBehaviour
{
    [SerializeField] private TutorialPanelController _tutorialController;
    [Tooltip("Thời gian chờ tối đa (giây) trước khi tự kích hoạt nếu scene không dùng NetworkGameEntryGate.")]
    [SerializeField, Min(0.1f)] private float _fallbackDelay = 1.5f;

    private IGameEntryGate _entryGate;
    private NetworkNest _nest;
    private bool _hasTriggered;

    private void Awake()
    {
        if (_tutorialController == null)
        {
            _tutorialController = GetComponent<TutorialPanelController>();
        }
    }

    private IEnumerator Start()
    {
        // Chờ tìm kiếm Gate và Nest trong scene
        float searchTimeout = Time.unscaledTime + 5f;
        while (_entryGate == null && _nest == null && Time.unscaledTime < searchTimeout)
        {
            foreach (var gate in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (gate is IGameEntryGate entryGate)
                {
                    _entryGate = entryGate;
                    break;
                }
            }

            _nest = FindFirstObjectByType<NetworkNest>();

            if (_entryGate != null || _nest != null)
            {
                break;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        // Chờ điều kiện bắt đầu game từ cả hai người chơi
        var interval = new WaitForSecondsRealtime(0.1f);
        float startedTime = Time.unscaledTime;

        while (!_hasTriggered)
        {
            // Trường hợp 1: Có NetworkGameEntryGate (màn chơi online 2 người)
            if (_entryGate != null)
            {
                if (_entryGate.IsOpen)
                {
                    TriggerTutorial();
                    yield break;
                }
            }
            // Trường hợp 2: Có NetworkNest nhưng không có Gate riêng
            else if (_nest != null)
            {
                if (_nest.IsPlaying || _nest.RoundStage == NestRoundStage.Playing)
                {
                    TriggerTutorial();
                    yield break;
                }
            }
            // Trường hợp 3: Fallback cho scene test không có Gate hoặc Nest
            else if (Time.unscaledTime - startedTime > _fallbackDelay)
            {
                TriggerTutorial();
                yield break;
            }

            yield return interval;
        }
    }

    private void TriggerTutorial()
    {
        if (_hasTriggered)
        {
            return;
        }

        _hasTriggered = true;

        if (_tutorialController != null)
        {
            _tutorialController.Show();
        }
    }
}
