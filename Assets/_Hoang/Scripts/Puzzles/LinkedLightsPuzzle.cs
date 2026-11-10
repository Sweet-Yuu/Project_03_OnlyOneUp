using System;
using Unity.Netcode;
using UnityEngine;

namespace Hoang.Puzzles
{
    [SelectionBase]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class LinkedLightsPuzzle : NetworkBehaviour
    {
        [Tooltip("Chuỗi nút 1–9 dùng tạo bố cục từ bảng tắt. Không phải danh sách đèn bật.")]
        [SerializeField] private int[] _scrambleButtons = { 1, 2, 4, 5, 9 };
        [Tooltip("Component thực thi IPuzzleInteractable, ví dụ PuzzleGate.")]
        [SerializeField] private MonoBehaviour _gateBehaviour;
        [SerializeField] private Transform _consolePoint;
        [SerializeField, Min(0.1f)] private float _interactionDistance = 5f;

        [Header("Debug & Quick Testing")]
        [Tooltip("Bật để tự động hoàn thành câu đố và mở cổng ngay lập tức phục vụ test game.")]
        [SerializeField] private bool instantSolve;

        // -1 phân biệt trạng thái chưa nhận dữ liệu mạng với bảng đã giải (0).
        private readonly NetworkVariable<int> _netMask = new NetworkVariable<int>(-1,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private LinkedLightsBoard _board;
        private IPuzzleInteractable _gate;
        private bool lastInstantSolve;

        public int Mask => instantSolve ? 0 : (IsSpawned ? _netMask.Value : _board != null ? _board.Mask : -1);
        public bool IsSolved => instantSolve || Mask == 0;
        public bool IsNetworkSessionActive => NetworkManager != null && NetworkManager.IsListening;
        public bool IsReady => isActiveAndEnabled && _board != null && _gate != null &&
            (instantSolve || (Mask >= 0 && Mask <= 511)) && (!IsNetworkSessionActive || IsSpawned);
        public event Action<int> StateChanged;

        private void Awake()
        {
            _gate = _gateBehaviour as IPuzzleInteractable;
            try
            {
                _board = new LinkedLightsBoard(_scrambleButtons);
                if (_gate == null || _consolePoint == null || float.IsNaN(_interactionDistance) ||
                    float.IsInfinity(_interactionDistance) || _interactionDistance <= 0f)
                    throw new ArgumentException("Cần gán cổng, điểm tương tác và khoảng cách hợp lệ.");
            }
            catch (ArgumentException error)
            {
                Debug.LogError($"[LinkedLightsPuzzle] {error.Message}", this);
                enabled = false;
            }
        }

        private void Start()
        {
            lastInstantSolve = instantSolve;
            ApplyState();
        }

        private void Update()
        {
            if (instantSolve != lastInstantSolve)
            {
                lastInstantSolve = instantSolve;
                SyncInstantSolve(instantSolve);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _gate != null)
            {
                if (instantSolve != lastInstantSolve)
                {
                    lastInstantSolve = instantSolve;
                    SyncInstantSolve(instantSolve);
                }
            }
        }
#endif

        public override void OnNetworkSpawn()
        {
            _netMask.OnValueChanged += HandleMaskChanged;
            if (IsServer && _board != null)
            {
                _netMask.Value = instantSolve ? 0 : _board.Mask;
            }
            else if (!IsServer && instantSolve)
            {
                SetInstantSolveRpc(true);
            }
            ApplyState();
        }

        private void SyncInstantSolve(bool solved)
        {
            if (IsSpawned)
            {
                if (IsServer)
                {
                    _netMask.Value = solved ? 0 : (_board != null ? _board.Mask : 0);
                    ApplyState();
                }
                else
                {
                    // Client gửi RPC lên Server để đồng bộ trạng thái mở cổng tới Host và tất cả người chơi
                    SetInstantSolveRpc(solved);
                    ApplyState();
                }
            }
            else
            {
                ApplyState();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetInstantSolveRpc(bool solved)
        {
            _netMask.Value = solved ? 0 : (_board != null ? _board.Mask : 0);
            ApplyState();
        }

        public override void OnNetworkDespawn()
        {
            _netMask.OnValueChanged -= HandleMaskChanged;
            StateChanged?.Invoke(-1);
        }

        public void RequestPress(int button)
        {
            if (button < 1 || button > 9) return;
            SubmitCommand(button);
        }

        public void RequestReset() => SubmitCommand(0);

        private void SubmitCommand(int command)
        {
            if (!IsReady || IsSolved) return;
            if (IsSpawned) ChangeBoardRpc(command);
            else if (ChangeBoard(command)) ApplyState();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ChangeBoardRpc(int command, RpcParams rpcParams = default)
        {
            if (!IsReady || IsSolved || command < 0 || command > 9) return;
            if (NetworkManager != null && NetworkManager.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out NetworkClient client))
            {
                NetworkObject player = client.PlayerObject;
                if (player != null && player.IsSpawned && player.gameObject.activeInHierarchy && _consolePoint != null)
                {
                    float squaredDistance = (player.transform.position - _consolePoint.position).sqrMagnitude;
                    float maxAllowedDistance = Mathf.Max(_interactionDistance, 35f);
                    if (squaredDistance > maxAllowedDistance * maxAllowedDistance) return;
                }
            }

            if (ChangeBoard(command))
            {
                _netMask.Value = _board.Mask;
                if (IsServer)
                {
                    ApplyState();
                }
            }
        }

        private bool ChangeBoard(int command) => command == 0 ? _board.TryReset() : _board.TryPress(command);

        private void HandleMaskChanged(int previous, int current) => ApplyState();

        private void ApplyState()
        {
            if (!IsReady) return;
            _gate.SetActive(IsSolved);
            StateChanged?.Invoke(Mask);
        }
    }
}
