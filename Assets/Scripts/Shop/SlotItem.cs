using UnityEngine;

public class SlotItem
{
    private string itemName;
    private int itemPrice;
    private Sprite itemIcon;
    public ShopSlot shopItem;
    
    public void setItem(ShopSlot item)
    {
        shopItem = item;
        itemName = item.itemName;
        itemPrice = item.itemPrice;
        itemIcon = item.itemIcon;
    }
}
