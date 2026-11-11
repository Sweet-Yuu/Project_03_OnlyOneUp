using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Phát tiếng kêu (Honk) đã lưu của người chơi và đồng bộ cả âm thanh lẫn chữ bay qua mạng cho người chơi còn lại.
public sealed class NetworkPlayerHonk : NetworkBehaviour
{
    [SerializeField] private HonkCatalog catalog;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Min(0.05f)] private float cooldown = 0.25f;

    [Header("Floating Honk Text")]
    [SerializeField] private string honkText = "@aejddd##";
    [SerializeField] private Color honkTextColor = new(1f, 0.92f, 0.35f, 1f);
    [SerializeField] private TMP_FontAsset honkTextFont;
    [SerializeField, Min(1f)] private float popupFontSize = 6f;
    [SerializeField] private float popupHeadOffset = 1.6f;
    [SerializeField] private float popupRightOffset = 1.1f;
    [SerializeField, Min(0.1f)] private float popupRiseDistance = 1.6f;
    [SerializeField] private float popupFlyRightDistance = 1.4f;
    [SerializeField, Min(0.05f)] private float popupRiseDuration = 0.5f;
    [SerializeField, Min(0.05f)] private float popupFadeDuration = 0.3f;

    private readonly NetworkVariable<int> selectedHonkIndex = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private static readonly List<RaycastResult> UiRaycastResults = new();
    private AudioSource audioSource;
    private Transform headAnchor;
    private int localHonkIndex;
    private float nextHonkAt;

    private void Awake()
    {
        if (catalog == null) catalog = HonkCatalog.LoadDefault();
        headAnchor = FindActiveChild(transform, "Head");
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        // Phát 2D để cả hai người chơi luôn nghe rõ dù camera đặt xa nhân vật.
        audioSource.spatialBlend = 0f;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        int count = catalog != null ? catalog.Count : 0;
        localHonkIndex = HonkPreferences.LoadIndex(count);
        SubmitHonkIndexServerRpc(localHonkIndex);
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || !Application.isFocused) return;
        if (Time.timeScale <= 0f || Time.unscaledTime < nextHonkAt) return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame) return;
        if (IsTypingInInputField() || IsPointerOverUI()) return;

        nextHonkAt = Time.unscaledTime + cooldown;
        TriggerHonkEffects(localHonkIndex);
        RequestHonkServerRpc(localHonkIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitHonkIndexServerRpc(int index)
    {
        selectedHonkIndex.Value = ClampHonkIndex(index);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestHonkServerRpc(int index)
    {
        int safeIndex = ClampHonkIndex(index);
        selectedHonkIndex.Value = safeIndex;
        BroadcastHonkClientRpc(safeIndex);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastHonkClientRpc(int index)
    {
        // Máy phát đã kêu và hiện chữ ngay lúc bấm để tránh độ trễ mạng; chỉ phát lại trên máy người chơi còn lại.
        if (IsOwner) return;
        TriggerHonkEffects(index);
    }

    private int ClampHonkIndex(int index)
    {
        int count = catalog != null ? catalog.Count : 0;
        return count > 0 ? Mathf.Clamp(index, 0, count - 1) : 0;
    }

    private void TriggerHonkEffects(int index)
    {
        PlayHonkClip(index);
        SpawnHonkPopup();
    }

    private void PlayHonkClip(int index)
    {
        if (catalog == null) catalog = HonkCatalog.LoadDefault();
        AudioClip clip = catalog != null ? catalog.GetClip(index) : null;
        if (clip == null) return;

        // Ưu tiên phát qua AudioManager để đi đúng AudioMixerGroup SFX và âm lượng cài đặt.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume);
            return;
        }

        if (audioSource != null) audioSource.PlayOneShot(clip, volume);
    }

    private void SpawnHonkPopup()
    {
        // Tính hướng phải theo camera hiện tại để chữ luôn xuất hiện ngoài đầu và bay chếch sang phải màn hình.
        Camera cam = Camera.main;
        Vector3 rightAxis = cam != null
            ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized
            : Vector3.right;
        if (rightAxis.sqrMagnitude < 0.001f) rightAxis = Vector3.right;

        Vector3 headBase = headAnchor != null
            ? headAnchor.position
            : transform.position + Vector3.up * 1.5f;
        Vector3 origin = headBase + Vector3.up * popupHeadOffset + rightAxis * popupRightOffset;
        Vector3 travelOffset = Vector3.up * popupRiseDistance + rightAxis * popupFlyRightDistance;

        HonkPopupText.Spawn(
            origin,
            travelOffset,
            honkText,
            honkTextColor,
            honkTextFont,
            popupFontSize,
            popupRiseDuration,
            popupFadeDuration);
    }

    private static Transform FindActiveChild(Transform root, string targetName)
    {
        foreach (Transform child in root)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            if (child.name == targetName) return child;
            Transform nested = FindActiveChild(child, targetName);
            if (nested != null) return nested;
        }
        return null;
    }

    private static bool IsPointerOverUI()
    {
        // Khi khóa chuột điều khiển gameplay hoặc trỏ vào HUD không tương tác, không chặn nút kêu.
        if (Cursor.lockState == CursorLockMode.Locked || EventSystem.current == null) return false;
        Mouse mouse = Mouse.current;
        if (mouse == null) return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = mouse.position.ReadValue()
        };
        UiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, UiRaycastResults);
        for (int i = 0; i < UiRaycastResults.Count; i++)
        {
            GameObject hit = UiRaycastResults[i].gameObject;
            if (hit == null) continue;
            if (hit.GetComponentInParent<Selectable>() != null || hit.GetComponentInParent<IPointerClickHandler>() != null)
                return true;
        }
        return false;
    }

    private static bool IsTypingInInputField()
    {
        if (EventSystem.current == null) return false;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        return selected != null && selected.TryGetComponent(out TMP_InputField input) && input.isFocused;
    }
}

