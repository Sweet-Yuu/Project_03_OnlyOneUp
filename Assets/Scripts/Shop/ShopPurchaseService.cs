using UnityEngine;
using System.Collections.Generic;
public class ShopPurchaseService : MonoBehaviour
{
    private bool isPurchased = false;
    private bool isBought = false;
    public bool IsBought => isBought;
    public bool PurchaseItem(ShopSlot item)
    {
        isPurchased = CurrencyManager.instance.SpendCoin(item.itemPrice);
        if (isPurchased)
        {
            isBought = true;
            List<string> list = PlayerInventory.instance.PurchasedItem;
            list.Add(item.ItemID);  
            Debug.Log($"Item {item.itemName} purchased successfully.");
            SaveSystem.instance.SaveItemId(list);
        }
        else
        {
            OutOfCoin();
        }
        return isPurchased;
    }

    public void OutOfCoin()
    {
        Debug.Log("Not enough coins to purchase the item.");
    }
}
