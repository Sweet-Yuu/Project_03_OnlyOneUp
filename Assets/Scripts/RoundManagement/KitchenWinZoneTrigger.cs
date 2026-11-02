using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Kích hoạt trạng thái chiến thắng Màn 1 khi người chơi hoặc tổ trứng bước vào khu vực bãi đất trống cuối bản đồ.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class KitchenWinZoneTrigger : MonoBehaviour
{
    private NetworkNest nestCache;
    private bool winTriggered;

    private void Awake()
    {
        // Đảm bảo collider là trigger để người chơi và tổ trứng có thể đi vào
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (winTriggered) return;
        if (!IsEligibleWinner(other)) return;

        // Chỉ Server/Host có quyền xác thực chiến thắng và thay đổi NetworkVariable
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && !networkManager.IsServer) return;

        TriggerVictory();
    }

    private void TriggerVictory()
    {
        if (winTriggered) return;

        if (nestCache == null)
        {
            foreach (NetworkNest nest in FindObjectsByType<NetworkNest>(FindObjectsSortMode.None))
            {
                if (nest.gameObject.scene == gameObject.scene)
                {
                    nestCache = nest;
                    break;
                }
            }
        }

        if (nestCache != null)
        {
            winTriggered = true;
            Debug.Log("[KitchenWinZone] Người chơi / Tổ trứng đã tiến vào bục chiến thắng! Kích hoạt kết thúc Màn 1.");
            nestCache.TriggerWin();
        }
    }

    private bool IsEligibleWinner(Collider other)
    {
        if (other == null) return false;

        // Kiểm tra nếu là tổ trứng hoặc phần thân của tổ trứng
        if (other.GetComponentInParent<NetworkNest>() != null) return true;

        // Kiểm tra nếu là người chơi (qua component danh tính hoặc chuyển động mạng)
        if (other.GetComponentInParent<NetworkPlayerIdentity>() != null) return true;
        if (other.GetComponentInParent<NetworkTestMovement>() != null) return true;

        // Kiểm tra tag quy ước
        if (other.CompareTag("Player")) return true;

        return false;
    }
}

