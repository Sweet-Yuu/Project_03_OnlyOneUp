using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoomLobbyView : MonoBehaviour
{
    [SerializeField] private TMP_Text _codeLabel;
    [SerializeField] private TMP_Text _countLabel;
    [SerializeField] private TMP_Text _statusLabel;
    [SerializeField] private TMP_Text[] _names;
    [SerializeField] private Image[] _avatars;
    [SerializeField] private GameObject[] _rows;
    [SerializeField] private Button _copyButton;
    [SerializeField] private Button _backButton;
    [Header("Drag your avatar sprites here")]
    [SerializeField] private Sprite _hostAvatar;
    [SerializeField] private Sprite _clientAvatar;
    [SerializeField] private Button _startGameButton;
    private IRoomRoster _roster;

    public void Bind(IRoomRoster roster)
    {
        if (_roster != null) _roster.Changed -= Refresh;
        _roster = roster;
        _roster.Changed += Refresh;
        Refresh();
    }

    public void SetStatus(string status, bool canLeave)
    {
        _statusLabel.text = status;
        _backButton.interactable = canLeave;
    }

    public void CopyCode()
    {
        if (string.IsNullOrEmpty(_roster?.Code)) return;
        GUIUtility.systemCopyBuffer = _roster.Code;
    }

    private void Refresh()
    {
        _codeLabel.text = "Code: " + (string.IsNullOrEmpty(_roster.Code) ? "..." : _roster.Code);
        _countLabel.text = "Players: " + _roster.Members.Count + "/2";
        _copyButton.interactable = !string.IsNullOrEmpty(_roster.Code);
        for (int i = 0; i < _rows.Length; i++)
        {
            bool visible = i < _roster.Members.Count;
            _rows[i].SetActive(visible);
            if (!visible) continue;
            var member = _roster.Members[i];
            _names[i].richText = false;
            _names[i].text = member.Name;
            _avatars[i].sprite = member.IsHost ? _hostAvatar : _clientAvatar;
            _avatars[i].color = _avatars[i].sprite != null ? Color.white :
                (member.IsHost ? new Color(1, .73f, .28f) : new Color(.65f, .82f, 1));
            _avatars[i].preserveAspect = true;
        }
    }

    public void SetStartGameState(bool isHost, bool canStart)
    {
        _startGameButton.gameObject.SetActive(isHost);
        _startGameButton.interactable = canStart;
    }

    private void OnDestroy()
    {
        if (_roster != null) _roster.Changed -= Refresh;
    }
}
