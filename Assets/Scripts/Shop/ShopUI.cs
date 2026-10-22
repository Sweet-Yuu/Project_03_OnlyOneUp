using UnityEngine;
using System;
public class ShopUI : MonoBehaviour
{
    public SlotItem[] shopItems;
    public ShopData shopData;
    void Start()
    {
        for (int i = 0; i < shopItems.Length; i++)
        {
            shopItems[i].setItem(shopData.shopSlots[i]);
        }
    }
}
