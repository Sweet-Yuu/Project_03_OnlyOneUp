using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SlotItem : MonoBehaviour
{
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Image itemIconImage;
    private ShopSlot shopSlot;
    
    public void setItem(ShopSlot item)
    {
        shopSlot = item;
        itemNameText.text = item.itemName;
        itemPriceText.text = item.itemPrice.ToString();
        itemIconImage.sprite = item.itemIcon;
    }
}
