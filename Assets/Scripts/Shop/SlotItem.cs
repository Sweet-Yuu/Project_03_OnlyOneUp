using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SlotItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image boughtImage;
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

    public void BoughtSlot()
    {
        slotBackground.color = new Color32(130, 130, 130, 255);
        boughtImage.gameObject.SetActive(true);
    }
}
