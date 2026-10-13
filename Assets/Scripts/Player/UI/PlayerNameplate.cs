using TMPro;
using UnityEngine;

public sealed class PlayerNameplate : MonoBehaviour
{
    [SerializeField] private NetworkPlayerIdentity _identity;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private GameObject _background;

    private void OnEnable()
    {
        if (_identity == null || _nameText == null || _background == null)
        {
            Debug.LogError("Nameplate: cần gán Identity, Name Text và Background.", this);
            return;
        }
        _nameText.richText = false;
        _nameText.raycastTarget = false;
        _identity.NameChanged += Refresh;
        Refresh(_identity.IsSpawned ? _identity.DisplayName : string.Empty);
    }

    private void OnDisable()
    {
        if (_identity != null) _identity.NameChanged -= Refresh;
    }

    private void Refresh(string playerName)
    {
        _nameText.text = playerName;
        // Chỉ ẩn nền con, giữ script hoạt động để nhận tên gửi tới sau khi spawn.
        _background.SetActive(!string.IsNullOrWhiteSpace(playerName));
    }
}
