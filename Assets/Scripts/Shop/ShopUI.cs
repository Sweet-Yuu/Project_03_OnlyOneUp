using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class ShopUI : MonoBehaviour
{
    public SlotItem[] shopItems;
    public ShopData shopData;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI alertText;
    [SerializeField] private ShopPreview shopPreview;
    [SerializeField] private ShopPurchaseService shopPurchaseService;
    private SlotItem selectedSlot;
    void Start()
    {
        buyButton.onClick.AddListener(OnBuyButtonClicked);
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
                continue;
            }
            slotItem.setItem(shopData.shopSlots[i]);
            button.onClick.AddListener(() => SetSelectedSlot(slotItem));
        }
        RefreshSelectionUI();
        checkItem(PlayerInventory.instance.PurchasedItem);
    }
    //Ham set slot duoc chon
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
        shopPreview.PreviewItem(slotItem.Data.ItemID);
        UIButtonSFX.Instance.PlayClickSound();
    }
    // Ham check item da mua hay chua
    void checkItem(List<string> purchaseItem)
    {
        for(int i = 0; i < purchaseItem.Count; i++)
        {
            for(int j = 0; j < shopItems.Length; j++)
            {
                if(purchaseItem[i] == shopItems[j].Data.ItemID)
                {
                    SetBoughtSlot(shopItems[j]);
                }
            }
        }
    }

    void SetBoughtSlot(SlotItem slotItem)
    {
        if(slotItem == null) return;
        slotItem.BoughtSlot();
    }

    void OnBuyButtonClicked()
    {
        Debug.Log("Buy button clicked.");
        if (selectedSlot == null)
        {
            Debug.LogWarning("No slot selected to buy.", this);
            return;
        }
        bool isPurchased = shopPurchaseService.PurchaseItem(selectedSlot.Data);
        Debug.Log($"Purchase result: {isPurchased} for item {selectedSlot.Data.itemName}");
        if (isPurchased)
        {
            SetBoughtSlot(selectedSlot);
        }
    }

    private void RefreshSelectionUI()
    {
        bool hasSelection = selectedSlot != null;
        buyButton.gameObject.SetActive(hasSelection);
        alertText.gameObject.SetActive(!hasSelection);
    }

}
