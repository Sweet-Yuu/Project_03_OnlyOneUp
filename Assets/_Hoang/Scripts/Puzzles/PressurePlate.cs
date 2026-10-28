using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hoang.Puzzles
{
    // Bàn đạp áp lực vật lý: nhận diện người chơi, lún xuống khi dẫm và đổi màu chỉ báo.
    [SelectionBase]
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour, IPressurePlate
    {
        [Header("Visual Feedback")]
        [SerializeField] private Transform _plateMesh;
        [SerializeField] private MeshRenderer _indicatorRenderer;
        [SerializeField] private Material _inactiveMaterial;
        [SerializeField] private Material _activeMaterial;
        [SerializeField] private float _pressDepth = 0.08f;
        [SerializeField] private float _animSpeed = 8f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _pressSfx;
        [SerializeField] private AudioClip _releaseSfx;

        private readonly HashSet<Collider> _occupants = new HashSet<Collider>();
        private Vector3 _unpressedLocalPos;
        private Vector3 _pressedLocalPos;
        private bool _isPressed;

        public bool IsPressed => _isPressed;
        public event Action<bool> StateChanged;

        private void Awake()
        {
            if (_plateMesh != null)
            {
                _unpressedLocalPos = _plateMesh.localPosition;
                _pressedLocalPos = _unpressedLocalPos - new Vector3(0f, _pressDepth, 0f);
            }

            UpdateVisuals(instant: true);
        }

        private void Update()
        {
            // Dọn dẹp collider bị vô hiệu hóa hoặc bị hủy khi đang đứng trong trigger.
            int countBefore = _occupants.Count;
            _occupants.RemoveWhere(col => col == null || !col.enabled || !col.gameObject.activeInHierarchy);
            if (countBefore != _occupants.Count)
            {
                UpdateState();
            }

            // Di chuyển bàn đạp mượt mà giữa vị trí nổi và vị trí lún.
            if (_plateMesh != null)
            {
                Vector3 target = _isPressed ? _pressedLocalPos : _unpressedLocalPos;
                _plateMesh.localPosition = Vector3.Lerp(_plateMesh.localPosition, target, Time.deltaTime * _animSpeed);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsValidPlayer(other))
            {
                _occupants.Add(other);
                UpdateState();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_occupants.Remove(other))
            {
                UpdateState();
            }
        }

        private bool IsValidPlayer(Collider col)
        {
            if (col == null) return false;
            if (col.CompareTag("Player")) return true;
            if (col.GetComponentInParent<CharacterController>() != null) return true;

            // Nhận diện player theo Unity Netcode.
            NetworkObject netObj = col.GetComponentInParent<NetworkObject>();
            return netObj != null && netObj.IsPlayerObject;
        }

        private void UpdateState()
        {
            bool shouldBePressed = _occupants.Count > 0;
            if (_isPressed != shouldBePressed)
            {
                _isPressed = shouldBePressed;
                UpdateVisuals(instant: false);
                PlaySound(_isPressed ? _pressSfx : _releaseSfx);
                StateChanged?.Invoke(_isPressed);
            }
        }

        private void UpdateVisuals(bool instant)
        {
            if (_indicatorRenderer != null)
            {
                Material targetMat = _isPressed ? _activeMaterial : _inactiveMaterial;
                if (targetMat != null)
                {
                    _indicatorRenderer.material = targetMat;
                }
            }

            if (instant && _plateMesh != null)
            {
                _plateMesh.localPosition = _isPressed ? _pressedLocalPos : _unpressedLocalPos;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isPressed ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position + new Vector3(0f, 0.25f, 0f), new Vector3(1.8f, 0.5f, 1.8f));
        }
    }
}
