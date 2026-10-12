using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerCoinCollector : MonoBehaviour
{
    private CharacterController _controller;

    private void Awake() => _controller = GetComponent<CharacterController>();

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.TryGetComponent(out Coin coin))
            coin.TryCollect(_controller);
    }
}
