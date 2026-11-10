using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class Coin : NetworkBehaviour
{
    private ICoinWallet wallet;
    private Collider coinCollider;
    private bool isCollected;
    private float nextRequestTime;

    private void Awake()
    {
        // Cache collider trên chính coin, tránh tham chiếu nhầm collider khác trong scene.
        coinCollider = GetComponent<Collider>();
    }

    public void Initialize(ICoinWallet coinWallet)
    {
        wallet = coinWallet ?? throw new System.ArgumentNullException(nameof(coinWallet));
    }

    private void OnCollisionEnter(Collision collision) => TryCollect(collision.collider);
    private void OnTriggerEnter(Collider other) => TryCollect(other);
    private void OnTriggerStay(Collider other) => TryCollect(other);

    public void TryCollect(Collider playerCollider)
    {
        if (isCollected) return;

        if (IsSpawned)
        {
            NetworkObject player = playerCollider.GetComponentInParent<NetworkObject>();
            if (player == null || !player.IsPlayerObject || !player.IsOwner ||
                Time.unscaledTime < nextRequestTime) return;
            nextRequestTime = Time.unscaledTime + 0.2f;
            RequestCollectRpc();
            return;
        }

        // Không phát thưởng cục bộ trong lúc phiên online đang chờ spawn coin.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;
        if (!playerCollider.CompareTag("Player")) return;

        isCollected = true;
        if (coinCollider != null) coinCollider.enabled = false;
        if (wallet == null) wallet = CurrencyManager.instance;
        wallet?.AddCoin(1);
        Destroy(gameObject);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestCollectRpc(RpcParams rpcParams = default)
    {
        if (isCollected || !NetworkManager.ConnectedClients.TryGetValue(
                rpcParams.Receive.SenderClientId, out NetworkClient client)) return;
        NetworkObject player = client.PlayerObject;
        if (player == null || !player.TryGetComponent(out CharacterController controller)) return;

        // Controller của người chơi từ xa bị tắt trên host, nên tính bounds từ kích thước controller và vị trí đã đồng bộ thay vì controller.bounds.
        Vector3 scale = player.transform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(radius * 2f, controller.height * Mathf.Abs(scale.y));
        Bounds playerBounds = new Bounds(player.transform.TransformPoint(controller.center),
            new Vector3(radius * 2f, height, radius * 2f));
        // Dung sai nhỏ cho độ trễ đồng bộ vị trí khi chạm coin.
        playerBounds.Expand(1f);
        if (!playerBounds.Intersects(coinCollider.bounds)) return;

        isCollected = true;
        if (coinCollider != null) coinCollider.enabled = false;
        RewardClientsRpc();

        // Sử dụng Despawn(false) cho in-scene NetworkObject để không sinh lỗi Netcode cảnh báo phá hủy scene object.
        if (IsSpawned)
        {
            NetworkObject.Despawn(false);
        }
        gameObject.SetActive(false);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void RewardClientsRpc()
    {
        isCollected = true;
        if (coinCollider != null) coinCollider.enabled = false;
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            rend.enabled = false;
        }
        gameObject.SetActive(false);

        if (wallet == null)
        {
            wallet = CurrencyManager.instance;
        }

        if (wallet == null)
        {
            Debug.LogError("Coin: chưa được truyền ICoinWallet và không tìm thấy CurrencyManager.instance.", this);
            return;
        }

        wallet.AddCoin(1);
    }
}
