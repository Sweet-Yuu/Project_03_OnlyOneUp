using UnityEngine;

// Điểm lắp ghép dependency của scene: chỉ component này biết ví tiền cụ thể.
public class CoinWalletInstaller : MonoBehaviour
{
    [SerializeField] private CurrencyManager _currencyManager;
    [SerializeField] private Coin[] _coins;

    private void Awake()
    {
        if (_currencyManager == null)
        {
            Debug.LogError("CoinWalletInstaller: cần gán ví tiền.", this);
            return;
        }

        // [Kitchen_Map]: Tự động tìm coin trong scene nếu danh sách chưa được gán hoặc chứa tham chiếu rỗng
        // (Giúp map mới như Kitchen_Map tự inject ví tiền vào tất cả coin mà không cần kéo thả thủ công trong Inspector)
        if (_coins == null || _coins.Length == 0 || System.Array.Exists(_coins, c => c == null))
        {
            var sceneCoins = new System.Collections.Generic.List<Coin>();
            foreach (Coin coin in FindObjectsByType<Coin>(FindObjectsSortMode.None))
            {
                if (coin != null && coin.gameObject.scene == gameObject.scene)
                    sceneCoins.Add(coin);
            }
            _coins = sceneCoins.ToArray();
        }

        // Inject trong Awake để coin sẵn sàng trước các callback vật lý.
        foreach (Coin coin in _coins)
        {
            if (coin != null) coin.Initialize(_currencyManager);
        }
    }
}
