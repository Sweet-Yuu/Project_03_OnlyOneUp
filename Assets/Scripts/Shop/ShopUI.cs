using UnityEngine;

public class ShopUI : MonoBehaviour
{
    public ShopSlot[] shopItems;
    void Start()
    {
        foreach (ShopSlot item in shopItems)
        {
            SlotItem slotItem = new SlotItem();
            slotItem.setItem(item);
        }
    }
}
