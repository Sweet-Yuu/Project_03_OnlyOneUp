using UnityEngine;

public class CharacterEquipment : MonoBehaviour
{
    [SerializeField] private Transform hatAnchor;
    public static CharacterEquipment Instance;
    [SerializeField] private ShopData data;
    private GameObject currentItem;
    public GameObject CurrentItem => currentItem;

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void EquipItem(string itemId)
    {
        if(itemId == "")
        {
            ClearItem();
            return;
        }
        Debug.Log("Chay den day");
        Debug.Log($"ItemId: {itemId}");
        if(data == null || itemId == null)
        {
            return;
        }

        if(currentItem != null)
        {
            currentItem.SetActive(false);
            Destroy(currentItem);
        }
        
        for(int i = 0; i < data.shopSlots.Length; i++)
        {
            string id = data.shopSlots[i].ItemID;
            if(itemId == id)
            {
                currentItem = Instantiate(data.shopSlots[i].itemPrefab, hatAnchor, false);
                break;
            }
        }
    }

    public void ClearItem()
    {
        if(currentItem == null) return;
        currentItem.SetActive(false);
        Destroy(currentItem);
    }

}
