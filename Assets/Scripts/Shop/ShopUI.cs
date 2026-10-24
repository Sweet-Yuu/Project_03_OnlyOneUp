using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
public class ShopUI : MonoBehaviour
{
    public SlotItem[] shopItems;
    public ShopData shopData;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image coinIcon;
    [SerializeField] private TextMeshProUGUI alertText;
    [SerializeField] private ShopPreview shopPreview;
    private SlotItem selectedSlot;
    void Start()
    {
         if (shopItems == null || shopData == null ||
            shopData.shopSlots == null ||
            shopItems.Length > shopData.shopSlots.Length)
        {
            Debug.LogError("ShopUI lack data for slots.", this);
            return;
        }
        for (int i = 0; i < shopItems.Length; i++)
        {
            SlotItem slotItem = shopItems[i];
            if (slotItem == null ||
                !slotItem.TryGetComponent<Button>(out var button))
            {
                Debug.LogError($"Slot {i} chưa được gán hoặc thiếu Button.", this);
                continue;
            }
            slotItem.setItem(shopData.shopSlots[i]);
            button.onClick.AddListener(() => SetSelectedSlot(slotItem));
        }
        RefreshSelectionUI();
    }

    void SetSelectedSlot(SlotItem slotItem)
    {
        if(selectedSlot == slotItem)
        {
            return;
        }
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        selectedSlot = slotItem;
        selectedSlot.SetSelected(true);
        RefreshSelectionUI();
        shopPreview.Initialize(slotItem.Data.itemPrefab);
    }

    private void RefreshSelectionUI()
    {
        bool hasSelection = selectedSlot != null;

        priceText.gameObject.SetActive(hasSelection);
        buyButton.gameObject.SetActive(hasSelection);
        coinIcon.gameObject.SetActive(hasSelection);
        alertText.gameObject.SetActive(!hasSelection);

        if (hasSelection)
            priceText.text = selectedSlot.itemPriceText.text;
    }

    private void setUpPrefab(GameObject prefab)
    {
        
    }
}
