using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Hoang.Puzzles
{
    public enum DualPuzzleMode
    {
        HoldBoth,         // Phải đứng giữ cả 2 nút thì cổng mới duy trì mở
        TimedAfterBoth,   // Cả 2 cùng dẫm một lần -> cổng mở trong X giây rồi đóng lại
        PermanentUnlock   // Cả 2 cùng dẫm một lần -> cổng mở vĩnh viễn
    }

    // Điều phối câu đố dẫm nút đôi: hỗ trợ cả chế độ Online Netcode lẫn Offline Test độc lập.
    [SelectionBase]
    public class DualPressurePuzzleCoordinator : NetworkBehaviour
    {
        [Header("Puzzle Components")]
        [SerializeField] private PressurePlate _plateA;
        [SerializeField] private PressurePlate _plateB;
        [SerializeField] private PuzzleGate _gate;

        [Header("Puzzle Rules")]
        [SerializeField] private DualPuzzleMode _mode = DualPuzzleMode.HoldBoth;
        [SerializeField] private float _timedDuration = 7f;

        // Biến mạng đồng bộ trạng thái mở/đóng cổng tới tất cả Client.
        private readonly NetworkVariable<bool> _isGateOpenNet = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Coroutine _timerCoroutine;
        private bool _isSolved;

        private void Awake()
        {
            if (_plateA != null) _plateA.StateChanged += HandlePlateStateChanged;
            if (_plateB != null) _plateB.StateChanged += HandlePlateStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            _isGateOpenNet.OnValueChanged += OnGateStateNetChanged;
            ApplyGateState(_isGateOpenNet.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isGateOpenNet.OnValueChanged -= OnGateStateNetChanged;
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (_plateA != null) _plateA.StateChanged -= HandlePlateStateChanged;
            if (_plateB != null) _plateB.StateChanged -= HandlePlateStateChanged;
        }

        private void HandlePlateStateChanged(bool _)
        {
            // Chỉ Server (hoặc phiên Offline không kết nối mạng) mới có quyền xử lý logic câu đố.
            if (IsSpawned && !IsServer) return;

            EvaluatePuzzleState();
        }

        private void EvaluatePuzzleState()
        {
            if (_isSolved && _mode == DualPuzzleMode.PermanentUnlock) return;

            bool bothPressed = _plateA != null && _plateA.IsPressed &&
                               _plateB != null && _plateB.IsPressed;

            switch (_mode)
            {
                case DualPuzzleMode.HoldBoth:
                    SetGateState(bothPressed);
                    break;

                case DualPuzzleMode.PermanentUnlock:
                    if (bothPressed)
                    {
                        _isSolved = true;
                        SetGateState(true);
                    }
                    break;

                case DualPuzzleMode.TimedAfterBoth:
                    if (bothPressed)
                    {
                        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
                        _timerCoroutine = StartCoroutine(TimedOpenRoutine());
                    }
                    break;
            }
        }

        private IEnumerator TimedOpenRoutine()
        {
            SetGateState(true);
            yield return new WaitForSeconds(_timedDuration);
            SetGateState(false);
            _timerCoroutine = null;
        }

        private void SetGateState(bool open)
        {
            if (IsSpawned && IsServer)
            {
                _isGateOpenNet.Value = open;
            }
            else if (!IsSpawned)
            {
                // Cho phép chạy test độc lập trong Editor mà không cần kết nối mạng Netcode.
                ApplyGateState(open);
            }
        }

        private void OnGateStateNetChanged(bool previous, bool current)
        {
            ApplyGateState(current);
        }

        private void ApplyGateState(bool open)
        {
            if (_gate != null)
            {
                _gate.SetActive(open);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_plateA != null && _plateB != null)
            {
                float dist = Vector3.Distance(_plateA.transform.position, _plateB.transform.position);
                // Cự ly dây nối giữa 2 người chơi khoảng 4m - 5.5m. Nếu vượt quá 5.5m sẽ cảnh báo đỏ.
                Gizmos.color = dist <= 5.5f ? Color.green : Color.red;
                Gizmos.DrawLine(_plateA.transform.position, _plateB.transform.position);
            }

            if (_gate != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _gate.transform.position);
            }
        }
    }
}
