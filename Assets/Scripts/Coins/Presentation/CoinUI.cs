using UnityEngine;
using TMPro;
public class CoinUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;

    private void Start()
    {
        CurrencyManager.instance.OnCoinChanged += UpdateCoinUI;
        UpdateCoinUI(CurrencyManager.instance.Coin);
    }
    
    private void OnDestroy()
    {
        CurrencyManager.instance.OnCoinChanged -= UpdateCoinUI;
    }

    void UpdateCoinUI(int coinAmount)
    {
        coinText.text = coinAmount.ToString();
    }
}
