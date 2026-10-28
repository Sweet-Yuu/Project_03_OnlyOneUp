using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public sealed class LeaveRoomButton : MonoBehaviour
{
    private Button button;

    private void Awake() => button = GetComponent<Button>();
    private void OnEnable() => button.onClick.AddListener(LeaveRoom);
    private void OnDisable() => button.onClick.RemoveListener(LeaveRoom);

    public void LeaveRoom() => ReturnToMenuTransition.Begin();
}
