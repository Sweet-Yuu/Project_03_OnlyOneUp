using UnityEngine;
using System;
[Serializable]
public class ShopSlot
{
    public string itemName;
    public int itemPrice;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public GameObject ItemPrefab => itemPrefab;
    [SerializeField] private string itemID;
    public string ItemID => itemID;
}
