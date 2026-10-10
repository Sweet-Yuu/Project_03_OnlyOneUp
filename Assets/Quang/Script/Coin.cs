using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class Coin : NetworkBehaviour
{
    private ICoinWallet _wallet;
    private Collider _collider;
    private bool _collected;
    private float _nextRequestTime;

    private void Awake()
    {
        // Cache collider trên chính coin, tránh tham chiếu nhầm coin khác trong scene.
        _collider = GetComponent<Collider>();
    }

    public void Initialize(ICoinWallet wallet)
    {
        _wallet = wallet ?? throw new System.ArgumentNullException(nameof(wallet));
    }

    private void OnCollisionEnter(Collision collision) => TryCollect(collision.collider);
    private void OnTriggerEnter(Collider other) => TryCollect(other);
    private void OnTriggerStay(Collider other) => TryCollect(other);

    public void TryCollect(Collider playerCollider)
    {
        if (_collected) return;
        if (IsSpawned)
        {
            NetworkObject player = playerCollider.GetComponentInParent<NetworkObject>();
            if (player == null || !player.IsPlayerObject || !player.IsOwner ||
                Time.unscaledTime < _nextRequestTime) return;
            _nextRequestTime = Time.unscaledTime + 0.2f;
            RequestCollectRpc();
            return;
        }

        // Không phát thưởng cục bộ trong lúc phiên online đang chờ spawn coin.
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsListening) return;
        if (!playerCollider.CompareTag("Player") || _wallet == null) return;
        _collected = true;
        _wallet.AddCoin(1);
        Destroy(gameObject);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestCollectRpc(RpcParams rpcParams = default)
    {
        if (_collected || !NetworkManager.ConnectedClients.TryGetValue(
                rpcParams.Receive.SenderClientId, out NetworkClient client)) return;
        NetworkObject player = client.PlayerObject;
        if (player == null || !player.TryGetComponent(out CharacterController controller)) return;

        // Controller của người chơi từ xa bị tắt trên host, nên tính bounds từ
        // kích thước controller và vị trí đã đồng bộ thay vì controller.bounds.
        Vector3 scale = player.transform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(radius * 2f, controller.height * Mathf.Abs(scale.y));
        Bounds playerBounds = new Bounds(player.transform.TransformPoint(controller.center),
            new Vector3(radius * 2f, height, radius * 2f));
        // Dung sai nhỏ cho độ trễ đồng bộ vị trí khi chạm coin.
        playerBounds.Expand(1f);
        if (!playerBounds.Intersects(_collider.bounds)) return;

        _collected = true;
        RewardClientsRpc();
        NetworkObject.Despawn();
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void RewardClientsRpc()
    {
        if (_wallet == null)
        {
            Debug.LogError("Coin: chưa được truyền ICoinWallet để nhận thưởng.", this);
            return;
        }
        _wallet.AddCoin(1);
    }
}
