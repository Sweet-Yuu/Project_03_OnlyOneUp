using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hoang.Puzzles
{
    // Cầu bập bênh thăng bằng co-op: tính toán mô-men lực từ vị trí của người chơi
    // so với trục quay ở giữa. Đồng bộ góc xoay an toàn qua NetworkVariable.
    [SelectionBase]
    public class SeesawBridge : NetworkBehaviour, ISeesawPlatform
    {
        [Header("Tilt Configuration")]
        [Tooltip("Phần cầu thực sự xoay (Plank)")]
        [SerializeField] private Transform _plankTransform;
        [Tooltip("Trục xoay cục bộ của cầu (mặc định Vector3.right nếu chiều dài dọc trục Z)")]
        [SerializeField] private Vector3 _localTiltAxis = Vector3.right;
        [Tooltip("Góc nghiêng tối đa cho phép (độ)")]
        [SerializeField, Range(5f, 35f)] private float _maxTiltAngle = 20f;
        [Tooltip("Độ nhạy mô-men lực: khoảng cách người chơi tạo góc nghiêng mục tiêu")]
        [SerializeField, Range(1f, 15f)] private float _tiltSensitivity = 5f;
        [Tooltip("Tốc độ nghiêng chuyển động (độ/giây)")]
        [SerializeField, Range(1f, 30f)] private float _tiltSpeed = 12f;
        [Tooltip("Tốc độ tự thăng bằng về 0 khi không có ai đứng trên cầu")]
        [SerializeField, Range(1f, 30f)] private float _returnSpeed = 8f;

        [Header("Danger Warning Feedback")]
        [Tooltip("Góc bắt đầu cảnh báo nguy hiểm (độ)")]
        [SerializeField] private float _warningAngleThreshold = 14f;
        [SerializeField] private MeshRenderer[] _indicatorRenderers;
        [SerializeField] private Material _safeMaterial;
        [SerializeField] private Material _dangerMaterial;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _creakClip;
        [SerializeField] private float _creakCooldown = 2f;

        // Đồng bộ góc nghiêng qua mạng (Server tính toán, Client nội suy mượt mà)
        private readonly NetworkVariable<float> _netTiltAngle = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly HashSet<Collider> _playersOnBridge = new HashSet<Collider>();
        private float _currentAngle;
        private float _targetAngle;
        private float _lastCreakTime;
        private Quaternion _initialLocalRotation;

        public float CurrentTiltAngle => _currentAngle;
        public float MaxTiltAngle => _maxTiltAngle;
        public bool IsBalanced => Mathf.Abs(_currentAngle) <= 5f;
        public int PlayerCount => _playersOnBridge.Count;

        private void Awake()
        {
            if (_plankTransform == null) _plankTransform = transform;
            _initialLocalRotation = _plankTransform.localRotation;
        }

        public override void OnNetworkSpawn()
        {
            _netTiltAngle.OnValueChanged += OnNetTiltChanged;
            _currentAngle = _netTiltAngle.Value;
            ApplyRotation(_currentAngle);
        }

        public override void OnNetworkDespawn()
        {
            _netTiltAngle.OnValueChanged -= OnNetTiltChanged;
            _playersOnBridge.Clear();
        }

        private void Update()
        {
            CleanInvalidOccupants();

            if (!IsSpawned || IsServer)
            {
                // Server hoặc chế độ Offline độc lập: tự tính góc nghiêng mục tiêu dựa trên mô-men lực
                _targetAngle = CalculateTorqueTargetAngle();

                float speed = _playersOnBridge.Count > 0 ? _tiltSpeed : _returnSpeed;
                _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, speed * Time.deltaTime);

                if (IsSpawned && IsServer)
                {
                    _netTiltAngle.Value = _currentAngle;
                }
            }
            else
            {
                // Client nội suy mượt mà về góc do Server truyền tới
                _currentAngle = Mathf.MoveTowards(_currentAngle, _netTiltAngle.Value, _tiltSpeed * Time.deltaTime);
            }

            ApplyRotation(_currentAngle);
            UpdateWarningVisuals();
            CheckCreakSound();
        }

        private void CleanInvalidOccupants()
        {
            _playersOnBridge.RemoveWhere(col => col == null || !col.enabled || !col.gameObject.activeInHierarchy);
        }

        private float CalculateTorqueTargetAngle()
        {
            if (_playersOnBridge.Count == 0 || _plankTransform == null) return 0f;

            float netTorque = 0f;
            foreach (Collider playerCol in _playersOnBridge)
            {
                if (playerCol == null) continue;

                // Chuyển vị trí người chơi sang hệ tọa độ cục bộ của bản cầu để xác định khoảng cách tới tâm quay
                Vector3 localPos = _plankTransform.InverseTransformPoint(playerCol.bounds.center);

                // Nếu trục xoay là X, cánh tay đòn là khoảng cách theo trục Z
                float leverArm = Vector3.Dot(_localTiltAxis, Vector3.right) > 0.5f ? localPos.z : localPos.x;
                netTorque += leverArm;
            }

            // Góc nghiêng tỷ lệ thuận với tổng mô-men lực của cả hai người chơi
            float wantedAngle = netTorque * _tiltSensitivity;
            return Mathf.Clamp(wantedAngle, -_maxTiltAngle, _maxTiltAngle);
        }

        private void ApplyRotation(float angle)
        {
            if (_plankTransform == null) return;
            Quaternion tiltRot = Quaternion.AngleAxis(angle, _localTiltAxis);
            _plankTransform.localRotation = _initialLocalRotation * tiltRot;
        }

        private void UpdateWarningVisuals()
        {
            if (_indicatorRenderers == null || _indicatorRenderers.Length == 0) return;

            bool isDanger = Mathf.Abs(_currentAngle) >= _warningAngleThreshold;
            Material matToUse = isDanger ? _dangerMaterial : _safeMaterial;

            if (matToUse != null)
            {
                for (int i = 0; i < _indicatorRenderers.Length; i++)
                {
                    if (_indicatorRenderers[i] != null)
                    {
                        _indicatorRenderers[i].sharedMaterial = matToUse;
                    }
                }
            }
        }

        private void CheckCreakSound()
        {
            if (_audioSource == null || _creakClip == null) return;
            if (Mathf.Abs(_currentAngle) >= _warningAngleThreshold && Time.time > _lastCreakTime + _creakCooldown)
            {
                _lastCreakTime = Time.time;
                _audioSource.PlayOneShot(_creakClip);
            }
        }

        public void RegisterPlayer(Collider other)
        {
            if (IsValidPlayer(other))
            {
                _playersOnBridge.Add(other);
            }
        }

        public void UnregisterPlayer(Collider other)
        {
            _playersOnBridge.Remove(other);
        }

        private void OnTriggerEnter(Collider other) => RegisterPlayer(other);

        private void OnTriggerExit(Collider other) => UnregisterPlayer(other);

        private bool IsValidPlayer(Collider col)
        {
            if (col == null) return false;
            if (col.CompareTag("Player")) return true;
            if (col.GetComponentInParent<CharacterController>() != null) return true;

            NetworkObject netObj = col.GetComponentInParent<NetworkObject>();
            return netObj != null && netObj.IsPlayerObject;
        }

        private void OnNetTiltChanged(float previous, float current)
        {
            // Trạng thái đã được Client nội suy trong Update()
        }

        private void OnDrawGizmosSelected()
        {
            Transform t = _plankTransform != null ? _plankTransform : transform;
            Gizmos.color = Color.yellow;
            Vector3 worldAxis = t.TransformDirection(_localTiltAxis);
            Gizmos.DrawRay(t.position - worldAxis * 2.5f, worldAxis * 5f);

            // Cảnh báo đỏ nếu cầu đang bị nghiêng ở mức nguy hiểm
            Gizmos.color = Mathf.Abs(_currentAngle) >= _warningAngleThreshold ? Color.red : Color.green;
            Gizmos.DrawWireSphere(t.position, 0.5f);
        }
    }
}
