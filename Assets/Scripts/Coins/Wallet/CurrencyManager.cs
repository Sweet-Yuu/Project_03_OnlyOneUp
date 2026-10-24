using UnityEngine;
using System;
public class CurrencyManager : MonoBehaviour, ICoinWallet
{
    public static CurrencyManager instance;
    public int Coin {get; private set;}
    public event Action<int> OnCoinChanged;
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
        Coin = PlayerPrefs.GetInt("Coin", 0);
    }           
    public void AddCoin(int amount)
    {
        Coin += amount;
        OnCoinChanged?.Invoke(Coin);
        SaveSystem.instance.SaveCoin(Coin);
    }

    public void SpendCoin(int amount)
    {
        if(Coin >= amount)
        {
            Coin -= amount;
            OnCoinChanged?.Invoke(Coin);
            SaveSystem.instance.SaveCoin(Coin);
        }
        else
        {
            Debug.LogWarning("Not enough coins to spend.");
        }
    }
}
