using UnityEngine;
using System.Collections.Generic;
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem instance;
    private List<string> purchasedItem = new List<string>();
    public List<string> PurchasedItem => purchasedItem;
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

    [System.Serializable]
    private class PurchasedItemsData
    {
        public List<string> itemIds = new List<string>();
    }

    [System.Serializable]
    private class EquipmentSaveData
    {
        public string equippedItemId;
    }
    private const string EquipmentKey = "PlayerEquipment";

    private const string PurchasedItemsKey = "PurchasedItemIds";

    public void SaveItemId(List<string> itemIds)
    {
        if (itemIds == null)
            return;

        var data = new PurchasedItemsData
        {
            itemIds = new List<string>(itemIds)
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PurchasedItemsKey, json);
        PlayerPrefs.Save();
    }

    public List<string> LoadItemIds()
    {
        string json = PlayerPrefs.GetString(PurchasedItemsKey, "");

        if (string.IsNullOrEmpty(json))
            return new List<string>();

        try
        {
            var data = JsonUtility.FromJson<PurchasedItemsData>(json);
            return data?.itemIds ?? new List<string>();
        }
        catch (System.ArgumentException)
        {
            Debug.LogWarning("Dữ liệu item đã mua không đọc được.");
            return new List<string>();
        }
    }

    public void SaveEquippedItem(string itemId)
    {
        var data = new EquipmentSaveData
        {
            equippedItemId = itemId
        };

        PlayerPrefs.SetString(EquipmentKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public string LoadEquippedItem()
    {
        string json = PlayerPrefs.GetString(EquipmentKey, "");

        if (string.IsNullOrEmpty(json))
            return "";

        try
        {
            var data = JsonUtility.FromJson<EquipmentSaveData>(json);
            return data?.equippedItemId ?? "";
        }
        catch (System.ArgumentException)
        {
            Debug.LogWarning("Không đọc được dữ liệu trang bị.");
            return "";
        }
    }
}
