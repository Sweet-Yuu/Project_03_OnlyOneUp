using UnityEngine;
using System.Collections.Generic;
public class PlayerInventory : MonoBehaviour
{
    private List<string> purchasedItem = new List<string>();
    public List<string> PurchasedItem => purchasedItem;
    public static PlayerInventory instance;
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
            Debug.LogError("Không tìm thấy SaveSystem.", this);
            return;
        }

        purchasedItem = SaveSystem.instance.LoadItemIds();
    }
}
