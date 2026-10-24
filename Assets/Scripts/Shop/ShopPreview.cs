using UnityEngine;

public class ShopPreview : MonoBehaviour
{
    private GameObject currentItem;
    [SerializeField] private Transform hatAnchor;
    private ShopData shopData;

    public void Initialize(GameObject prefab)
    {
        if(hatAnchor == null || prefab == null)
        {
            Debug.LogError("Hat anchor or prefab is null.", this);
            return;
        }

        if(currentItem != null)
        {
            currentItem.SetActive(false);
            Destroy(currentItem);
        }
        currentItem = Instantiate(prefab, hatAnchor, false);
    }
}
