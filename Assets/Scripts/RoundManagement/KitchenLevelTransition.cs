using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý việc chuyển tiếp từ Kitchen_Map sang MainMap_Online khi người chơi đưa tổ trứng chạm vào cánh cửa ra khu đất.
/// </summary>
public sealed class KitchenLevelTransition : MonoBehaviour
{
    private const string TargetSceneName = "MainMap_Online";
    [SerializeField, Min(0f)] private float _transitionDelay = 2.5f;

    private NetworkNest _nest;
    private bool _transitionTriggered;

    private IEnumerator Start()
    {
        // Chờ tổ trứng xuất hiện và spawn trong scene
        while (_nest == null)
        {
            foreach (NetworkNest nest in FindObjectsByType<NetworkNest>(FindObjectsSortMode.None))
            {
                if (nest.gameObject.scene == gameObject.scene)
                {
                    _nest = nest;
                    break;
                }
            }
            if (_nest == null) yield return new WaitForSecondsRealtime(0.25f);
        }

        _nest.WinChanged += OnNestWinChanged;
        if (_nest.HasWon) OnNestWinChanged(true);
    }

    private void OnDestroy()
    {
        if (_nest != null) _nest.WinChanged -= OnNestWinChanged;
    }

    private void OnNestWinChanged(bool won)
    {
        if (!won || _transitionTriggered) return;
        _transitionTriggered = true;
        StartCoroutine(LoadNextLevelRoutine());
    }

    private IEnumerator LoadNextLevelRoutine()
    {
        // Chờ hiệu ứng ăn mừng chiến thắng vài giây trước khi chuyển cảnh
        yield return new WaitForSecondsRealtime(_transitionDelay);

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsServer && networkManager.SceneManager != null)
        {
            // Đồng bộ chuyển scene cho tất cả người chơi trong phòng qua Netcode
            networkManager.SceneManager.LoadScene(TargetSceneName, LoadSceneMode.Single);
        }
        else
        {
            // Chạy đơn máy hoặc chế độ test cục bộ
            SceneManager.LoadScene(TargetSceneName);
        }
    }
}
