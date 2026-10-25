using UnityEngine;

public class ShopPreview : MonoBehaviour
{
    public void PreviewItem(string itemId)
    {
        if(CharacterEquipment.Instance == null) return;
        CharacterEquipment.Instance.EquipItem(itemId);
    }
}
