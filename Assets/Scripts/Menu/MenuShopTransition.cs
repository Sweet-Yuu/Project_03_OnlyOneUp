using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Serialization;

public sealed class MenuShopTransition : MonoBehaviour
{
    [FormerlySerializedAs("_menuCamera")]
    [SerializeField] private Camera menuCamera;
    [FormerlySerializedAs("_character")]
    [SerializeField] private Transform character;
    [FormerlySerializedAs("_mainMenu")]
    [SerializeField] private CanvasGroup mainMenu;
    [FormerlySerializedAs("_shop")]
    [SerializeField] private CanvasGroup shop;
    [FormerlySerializedAs("_itemPanel")]
    [SerializeField] private RectTransform itemPanel;
    [FormerlySerializedAs("_shopButton")]
    [SerializeField] private Button shopButton;
    [FormerlySerializedAs("_backButton")]
    [SerializeField] private Button backButton;
    [FormerlySerializedAs("_duration")]
    [SerializeField, Min(0.1f)] private float duration = 0.65f;
    [FormerlySerializedAs("_characterViewport")]
    [SerializeField] private Vector2 characterViewport = new(0.13f, 0.53f);
    [FormerlySerializedAs("_characterScreenHeight")]
    [SerializeField, Range(0.2f, 0.8f)] private float characterScreenHeight = 0.58f;

    private Vector3 menuPosition;
    private Vector3 shopPosition;
    private float menuSize;
    private float shopSize;
    private Vector2 panelPosition;
    private float slideDistance;
    private float progress;
    private bool isOpen;
    private bool initialized;
    private Coroutine transition;

    private void Awake()
    {
        if (menuCamera == null || character == null || mainMenu == null ||
            shop == null || itemPanel == null || shopButton == null || backButton == null)
        {
            Debug.LogError("MenuShopTransition chưa được gán đủ tham chiếu.", this);
            enabled = false;
            return;
        }

        panelPosition = itemPanel.anchoredPosition;
        shop.gameObject.SetActive(false);
        shopButton.onClick.AddListener(OpenShop);
        backButton.onClick.AddListener(CloseShop);
        initialized = true;
    }

    private void Update()
    {
        if (isOpen && ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)))
            CloseShop();
    }

    public void OpenShop()
    {
        UIButtonSFX.Instance.PlayClickSound();
        if (!initialized || isOpen || transition != null) return;

        menuPosition = menuCamera.transform.position;
        menuSize = menuCamera.orthographicSize;
        if (!CalculateShopCamera()) return;

        isOpen = true;
        shop.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        slideDistance = ((RectTransform)itemPanel.parent).rect.width;
        BeginTransition(true);
    }

    public void CloseShop()
    {
        UIButtonSFX.Instance.PlayClickSound();
        if (!initialized || !isOpen) return;
        isOpen = false;
        mainMenu.gameObject.SetActive(true);
        BeginTransition(false);
        string item = PlayerInventory.instance.equipedItemId;
        CharacterEquipment.Instance.EquipItem(item);
    }

    private void BeginTransition(bool opening)
    {
        if (transition != null) StopCoroutine(transition);
        mainMenu.interactable = false;
        mainMenu.blocksRaycasts = false;
        // Chặn click xuyên qua shop trong lúc bảng đang di chuyển.
        shop.interactable = false;
        shop.blocksRaycasts = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        transition = StartCoroutine(Animate(opening));
    }

    private IEnumerator Animate(bool opening)
    {
        float target = opening ? 1f : 0f;
        ApplyProgress();
        while (!Mathf.Approximately(progress, target))
        {
            // UI vẫn chuyển động khi game đang tạm dừng hoặc timeScale bằng 0.
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / Mathf.Max(0.1f, duration));
            ApplyProgress();
            yield return null;
        }

        mainMenu.gameObject.SetActive(!opening);
        shop.gameObject.SetActive(opening);
        mainMenu.interactable = !opening;
        mainMenu.blocksRaycasts = !opening;
        shop.interactable = opening;
        shop.blocksRaycasts = opening;
        transition = null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(opening ? backButton.gameObject : shopButton.gameObject);
    }

    private void ApplyProgress()
    {
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        menuCamera.transform.position = Vector3.Lerp(menuPosition, shopPosition, eased);
        if (menuCamera.orthographic)
            menuCamera.orthographicSize = Mathf.Lerp(menuSize, shopSize, eased);
        itemPanel.anchoredPosition = panelPosition + Vector2.right * (slideDistance * (1f - eased));
        mainMenu.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.4f));
        shop.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - 0.15f) / 0.85f));
    }

    private bool CalculateShopCamera()
    {
        // Chỉ đo thân nhân vật có rig, tránh mũ hoặc hiệu ứng làm đổi khung hình khi mở shop.
        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("Không tìm thấy model có rig trong nhân vật menu.", this);
            return false;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (SkinnedMeshRenderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Transform cameraTransform = menuCamera.transform;
        Vector3 up = cameraTransform.up;
        Vector3 right = cameraTransform.right;
        Vector3 extents = bounds.extents;
        float height = 2f * (Mathf.Abs(up.x) * extents.x + Mathf.Abs(up.y) * extents.y + Mathf.Abs(up.z) * extents.z);
        shopSize = Mathf.Max(0.1f, height / (2f * characterScreenHeight));
        float distance = menuCamera.orthographic
            ? Vector3.Dot(bounds.center - menuPosition, cameraTransform.forward)
            : shopSize / Mathf.Tan(menuCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        distance = Mathf.Max(distance, bounds.extents.magnitude + menuCamera.nearClipPlane);
        float halfHeight = menuCamera.orthographic ? shopSize : distance * Mathf.Tan(menuCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        shopPosition = bounds.center - cameraTransform.forward * distance
            - right * ((characterViewport.x - 0.5f) * 2f * halfHeight * menuCamera.aspect)
            - up * ((characterViewport.y - 0.5f) * 2f * halfHeight);
        return true;
    }

    private void OnDisable()
    {
        if (!initialized || (!isOpen && transition == null)) return;
        if (transition != null) StopCoroutine(transition);
        transition = null;
        isOpen = false;
        progress = 0f;

        // Khi scene bị dỡ, camera hoặc UI có thể đã bị hủy trước component này.
        // Chỉ khôi phục giao diện khi các đối tượng vẫn còn tồn tại.
        if (menuCamera == null || itemPanel == null || mainMenu == null || shop == null)
            return;

        ApplyProgress();
        mainMenu.gameObject.SetActive(true);
        mainMenu.interactable = true;
        mainMenu.blocksRaycasts = true;
        shop.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (shopButton != null) shopButton.onClick.RemoveListener(OpenShop);
        if (backButton != null) backButton.onClick.RemoveListener(CloseShop);
    }
}
