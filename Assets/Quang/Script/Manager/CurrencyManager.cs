using UnityEngine;
using System;
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager instance;
    public int Coin;
    public GameObject coinPrefab;
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
    }           
    public void AddCoin(int amount)
    {
        Coin += amount;
        OnCoinChanged?.Invoke(Coin);
        SaveSystem.instance.SaveCoin(Coin);
    }
}
