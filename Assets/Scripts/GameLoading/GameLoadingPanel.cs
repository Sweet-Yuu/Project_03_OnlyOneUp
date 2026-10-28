using TMPro;
using UnityEngine;

public sealed class GameLoadingPanel : MonoBehaviour
{
    [SerializeField] private NetworkGameEntryGate _gate;
    [SerializeField] private CircularRevealGraphic _cover;
    [SerializeField] private TMP_Text _label;

    [SerializeField, TextArea]
    private string _loadingMessage = "Loading game……";

    [SerializeField, TextArea]
    private string _errorMessage =
        "Không thể chuẩn bị trận đấu. Hãy kiểm tra kết nối.";

    [SerializeField, Min(0.1f)]
    private float _revealDuration = 1.2f;

    [SerializeField, Min(10f)]
    private float _timeout = 125f;

    private float _startedAt;
    private float _elapsed;
    private bool _revealing;
    private bool _reported;
    private bool _failed;

    private void Awake()
    {
        _startedAt = Time.unscaledTime;

        _cover.SetProgress(0f);
        _label.text = _loadingMessage;
        _label.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_failed)
            return;

        // [Kitchen_Map]: Tự động tìm NetworkGameEntryGate trong scene nếu chưa được gán sẵn qua Inspector
        // Tránh lỗi văng timeout / màn hình loading bị treo khi load scene mới Kitchen_Map
        if (_gate == null)
        {
            foreach (NetworkGameEntryGate gate in FindObjectsByType<NetworkGameEntryGate>(FindObjectsSortMode.None))
            {
                if (gate != null && gate.gameObject.scene == gameObject.scene)
                {
                    _gate = gate;
                    break;
                }
            }

            if (_gate == null)
            {
                if (Time.unscaledTime - _startedAt > _timeout)
                    ShowError();
                return;
            }
        }

        if (_gate.HasFailed ||
            Time.unscaledTime - _startedAt > _timeout)
        {
            ShowError();
            return;
        }

        if (!_gate.IsSpawned)
            return;

        if (_gate.IsOpen)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!_gate.CanReveal)
            return;

        if (!_revealing)
        {
            _revealing = true;
            _label.gameObject.SetActive(false);
        }

        _elapsed += Time.unscaledDeltaTime;

        float progress = Mathf.Clamp01(
            _elapsed / _revealDuration);

        _cover.SetProgress(
            Mathf.SmoothStep(0f, 1f, progress));

        if (progress >= 1f && !_reported)
        {
            _reported = true;
            _gate.FinishLocalReveal();
        }
    }

    private void ShowError()
    {
        _failed = true;
        _cover.SetProgress(0f);
        _label.text = _errorMessage;
        _label.gameObject.SetActive(true);
    }
}