using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private Button equipButton;
    private List<string> itemID;
    private int count = -1;
    [SerializeField] private ShopData data;
    [SerializeField] private Transform hatAnchor;
    private GameObject currentItem;
    private GameObject newItem;
    void Start()
    {
        equipButton.onClick.AddListener(() => EquipItem());
        itemID = PlayerInventory.instance.PurchasedItem;
    }

    private void OnDestroy()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(EquipItem);
    }
    public void EquipItem()
    {   
        if(count >= itemID.Count - 1)
        {
            count = -1;
        }
        count++;
        string itemId = itemID[count];
        PlayerInventory.instance.SetEquipedItem(itemId);
        InstantiateItem(itemId);
    }
    //Ham khoi tao item
    private void InstantiateItem(string itemId)
    {
        if(itemID == null || itemID.Count == 0)
        {
            return;
        }

        for(int i = 0; i < data.shopSlots.Length; i++)
        {
            if(itemId == data.shopSlots[i].ItemID)
            {
                Debug.Log("Item ID matched: " + itemId);
                newItem = Instantiate(data.shopSlots[i].ItemPrefab, hatAnchor, false);
                break;
            }
        }
        if(currentItem != null)
        {
            currentItem.SetActive(false);
            Destroy(currentItem);
        }
        currentItem = newItem;
    }
}
