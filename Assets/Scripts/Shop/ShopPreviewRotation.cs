using UnityEngine;
using UnityEngine.EventSystems;

public class ShopPreviewRotation : MonoBehaviour, IDragHandler
{
    [SerializeField] private Transform _rotationRoot;
    [SerializeField] private float _degreesPerPixel = 0.4f;

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left ||
            _rotationRoot == null)
        {
            return;
        }

        float angle = -eventData.delta.x * _degreesPerPixel;

        _rotationRoot.Rotate(Vector3.up, angle, Space.World);
    }
}