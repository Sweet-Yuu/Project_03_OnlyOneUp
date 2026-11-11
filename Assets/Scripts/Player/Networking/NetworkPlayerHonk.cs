using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Phát tiếng kêu (Honk) đã lưu của người chơi và đồng bộ cả âm thanh lẫn chữ bay qua mạng cho người chơi còn lại.
public sealed class NetworkPlayerHonk : NetworkBehaviour
{
    [SerializeField] private HonkCatalog catalog;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Min(0.05f)] private float cooldown = 0.25f;

    [Header("Floating Honk Text")]
    [SerializeField] private string honkText = "HONK!";
    [SerializeField] private Color honkTextColor = new(1f, 0.92f, 0.35f, 1f);
    [SerializeField] private TMP_FontAsset honkTextFont;
    [SerializeField, Min(1f)] private float popupFontSize = 6f;
    [SerializeField] private float popupHeadOffset = 0.8f;
    [SerializeField, Min(0.1f)] private float popupRiseDistance = 1.6f;
    [SerializeField, Min(0.05f)] private float popupRiseDuration = 0.5f;
    [SerializeField, Min(0.05f)] private float popupFadeDuration = 0.3f;

    private readonly NetworkVariable<int> selectedHonkIndex = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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
        if (IsTypingInInputField() || IsPointerOverUI()) return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame) return;

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
        if (clip == null || audioSource == null) return;

        float sfxVolume = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.volume = volume * sfxVolume;
        audioSource.Play();
    }

    private void SpawnHonkPopup()
    {
        Vector3 origin = headAnchor != null
            ? headAnchor.position + Vector3.up * popupHeadOffset
            : transform.position + Vector3.up * (popupHeadOffset + 1.5f);
        HonkPopupText.Spawn(
            origin,
            honkText,
            honkTextColor,
            honkTextFont,
            popupFontSize,
            popupRiseDistance,
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
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsTypingInInputField()
    {
        if (EventSystem.current == null) return false;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        return selected != null && selected.TryGetComponent(out TMP_InputField input) && input.isFocused;
    }
}

