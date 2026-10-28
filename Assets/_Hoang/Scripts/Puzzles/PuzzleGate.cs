using UnityEngine;

namespace Hoang.Puzzles
{
    // Cánh cổng / cầu vật cản: di chuyển trượt mở hoặc nâng lên khi nhận tín hiệu kích hoạt.
    [SelectionBase]
    public class PuzzleGate : MonoBehaviour, IPuzzleInteractable
    {
        [Header("Movement Settings")]
        [Tooltip("Phần thân thực sự di chuyển (nếu để trống sẽ dùng chính GameObject này)")]
        [SerializeField] private Transform _movingPart;
        [Tooltip("Độ lệch tọa độ khi mở (so với vị trí đóng ban đầu)")]
        [SerializeField] private Vector3 _openOffset = new Vector3(0f, 4f, 0f);
        [SerializeField] private float _moveSpeed = 3f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _openClip;
        [SerializeField] private AudioClip _closeClip;

        private Vector3 _closedPosition;
        private Vector3 _openedPosition;
        private bool _isActive;

        public bool IsActive => _isActive;

        private void Awake()
        {
            if (_movingPart == null) _movingPart = transform;
            _closedPosition = _movingPart.localPosition;
            _openedPosition = _closedPosition + _openOffset;
        }

        public void SetActive(bool active)
        {
            if (_isActive == active) return;
            _isActive = active;
            PlaySound(_isActive ? _openClip : _closeClip);
        }

        private void Update()
        {
            if (_movingPart == null) return;
            Vector3 target = _isActive ? _openedPosition : _closedPosition;
            _movingPart.localPosition = Vector3.MoveTowards(_movingPart.localPosition, target, _moveSpeed * Time.deltaTime);
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
            Transform target = _movingPart != null ? _movingPart : transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(target.position + _openOffset, target.lossyScale);
            Gizmos.DrawLine(target.position, target.position + _openOffset);
        }
    }
}
