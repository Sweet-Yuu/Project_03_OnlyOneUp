using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hỗ trợ chuyển tiếp từ Kitchen_Map sang MainMap_Online khi người chơi chọn Chuyển sang màn 2.
/// Không tự động đếm ngược để tôn trọng lựa chọn trên Win Panel của người chơi.
/// </summary>
public sealed class KitchenLevelTransition : MonoBehaviour
{
    private const string TargetSceneName = "MainMap_Online";

    public static void LoadNextLevel()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsServer && networkManager.SceneManager != null)
        {
            // Chuyển scene đồng bộ cho tất cả Client qua Netcode
            networkManager.SceneManager.LoadScene(TargetSceneName, LoadSceneMode.Single);
        }
        else
        {
            // Chạy đơn máy hoặc chế độ test cục bộ
            SceneManager.LoadScene(TargetSceneName);
        }
    }
}
