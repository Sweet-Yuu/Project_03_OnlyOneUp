using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkPlayerEquipment : NetworkBehaviour
{
    [FormerlySerializedAs("shopData")]
    [SerializeField] private ShopData _shopData;
    [FormerlySerializedAs("hatAnchor")]
    [SerializeField] private Transform _hatAnchor;

    private readonly NetworkVariable<FixedString128Bytes> _equippedItemId = new(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private GameObject _currentItem;

    public override void OnNetworkSpawn()
    {
        _equippedItemId.OnValueChanged += HandleEquipmentChanged;
        // Người vào phòng sau cần hiển thị trạng thái đã đồng bộ trước khi đăng ký sự kiện.
        ApplyEquipment(_equippedItemId.Value.ToString());

        if (!IsOwner) return;
        if (SaveSystem.instance == null)
        {
            Debug.LogError("Chưa có SaveSystem để đọc trang bị của người chơi local.", this);
            return;
        }
        SubmitEquipmentRpc(SaveSystem.instance.LoadEquippedItem());
    }

    public override void OnNetworkDespawn()
    {
        _equippedItemId.OnValueChanged -= HandleEquipmentChanged;
        ClearEquipment();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitEquipmentRpc(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            _equippedItemId.Value = default;
            return;
        }
        if (System.Text.Encoding.UTF8.GetByteCount(itemId) > 125 || FindItem(itemId) == null)
        {
            Debug.LogWarning($"ID trang bị không hợp lệ: '{itemId}'.", this);
            return;
        }
        _equippedItemId.Value = new FixedString128Bytes(itemId);
    }

    private void HandleEquipmentChanged(FixedString128Bytes previous, FixedString128Bytes current)
    {
        ApplyEquipment(current.ToString());
    }

    private void ApplyEquipment(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            ClearEquipment();
            return;
        }
        ShopSlot item = FindItem(itemId);
        if (_hatAnchor == null || item == null)
        {
            Debug.LogError($"Thiếu HatAnchor hoặc prefab cho trang bị '{itemId}'.", this);
            return;
        }
        ClearEquipment();
        _currentItem = Instantiate(item.itemPrefab, _hatAnchor, false);
    }

    private ShopSlot FindItem(string itemId)
    {
        if (_shopData == null || _shopData.shopSlots == null) return null;
        foreach (ShopSlot item in _shopData.shopSlots)
        {
            if (item != null && item.ItemID == itemId && item.itemPrefab != null)
                return item;
        }
        return null;
    }

    private void ClearEquipment()
    {
        if (_currentItem == null) return;
        _currentItem.SetActive(false);
        Destroy(_currentItem);
        _currentItem = null;
    }
}
