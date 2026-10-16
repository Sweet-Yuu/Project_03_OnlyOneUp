using UnityEngine;

// Điểm lắp ghép dependency của scene: chỉ component này biết ví tiền cụ thể.
public class CoinWalletInstaller : MonoBehaviour
{
    [SerializeField] private CurrencyManager _currencyManager;
    [SerializeField] private Coin[] _coins;

    private void Awake()
    {
        if (_currencyManager == null || _coins == null)
        {
            Debug.LogError("CoinWalletInstaller: cần gán ví tiền và danh sách coin.", this);
            return;
        }

        // Inject trong Awake để coin sẵn sàng trước các callback vật lý.
        foreach (Coin coin in _coins)
        {
            if (coin == null)
            {
                Debug.LogError("CoinWalletInstaller: danh sách có coin chưa được gán.", this);
                continue;
            }
            coin.Initialize(_currencyManager);
        }
    }
}
