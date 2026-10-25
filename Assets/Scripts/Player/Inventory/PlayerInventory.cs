using UnityEngine;
using System.Collections.Generic;
public class PlayerInventory : MonoBehaviour
{
    private List<string> purchasedItem = new List<string>();
    public List<string> PurchasedItem => purchasedItem;
    public static PlayerInventory instance;
    public string equipedItemId {get; private set;}
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (SaveSystem.instance == null)
        {
            return;
        }

        purchasedItem = SaveSystem.instance.LoadItemIds();

        string savedId = SaveSystem.instance.LoadEquippedItem();

        equipedItemId = purchasedItem.Contains(savedId)
            ? savedId
            : "";
    }

    public void SetEquipedItem(string itemId)
{
    if (string.IsNullOrWhiteSpace(itemId) ||
        !purchasedItem.Contains(itemId))
    {
        return;
    }

    if (SaveSystem.instance == null)
    {
        Debug.LogError("Chưa có SaveSystem để lưu trang bị.");
        return;
    }

    equipedItemId = itemId;
    SaveSystem.instance.SaveEquippedItem(itemId);
}
}
