using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem instance;
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private const string CoinKey = "Coin";                         
    public void SaveCoin(int coinAmount)
    {
        PlayerPrefs.SetInt(CoinKey, coinAmount);
        PlayerPrefs.Save();
    }
}
