using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SlotItem : MonoBehaviour
{
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Image itemIconImage;
    public Image slotBackground;
    public Image boughtImage;
    public ShopSlot Data{get; private set;}
    
    public void setItem(ShopSlot item)
    {
        Data = item;
        itemNameText.text = item.itemName;
        itemPriceText.text = item.itemPrice.ToString();
        itemIconImage.sprite = item.itemIcon;
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            slotBackground.color = new Color32(118, 191, 228, 255);
        }
        else
        {
            slotBackground.color = new Color32(255, 255, 255, 255); 
        }
    }

    public void BoughtSlot(bool isBought)
    {
        if (isBought)
        {
            slotBackground.color = new Color32(118, 191, 228, 255);
            boughtImage.gameObject.SetActive(true);
        }
        else
        {
            slotBackground.color = new Color32(255, 255, 255, 255);
            boughtImage.gameObject.SetActive(false);
        }
    }
}
