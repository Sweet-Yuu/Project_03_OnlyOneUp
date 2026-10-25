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
        count++;
        if(count >= itemID.Count - 1)
        {
            count = -1;
        }
        string itemId = count == -1 ? "" : itemID[count];
        PlayerInventory.instance.SetEquipedItem(itemId);
        CharacterEquipment.Instance.EquipItem(itemId);
        UIButtonSFX.Instance.PlayClickSound();
    }
}
