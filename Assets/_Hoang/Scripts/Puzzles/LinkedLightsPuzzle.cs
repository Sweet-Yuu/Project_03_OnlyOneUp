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

        // -1 phân biệt trạng thái chưa nhận dữ liệu mạng với bảng đã giải (0).
        private readonly NetworkVariable<int> _netMask = new NetworkVariable<int>(-1,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private LinkedLightsBoard _board;
        private IPuzzleInteractable _gate;

        public int Mask => IsSpawned ? _netMask.Value : _board != null ? _board.Mask : -1;
        public bool IsSolved => Mask == 0;
        public bool IsNetworkSessionActive => NetworkManager != null && NetworkManager.IsListening;
        public bool IsReady => isActiveAndEnabled && _board != null && _gate != null &&
            Mask >= 0 && Mask <= 511 && (!IsNetworkSessionActive || IsSpawned);
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

        private void Start() => ApplyState();

        public override void OnNetworkSpawn()
        {
            _netMask.OnValueChanged += HandleMaskChanged;
            if (IsServer && _board != null) _netMask.Value = _board.Mask;
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
            if (!NetworkManager.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out NetworkClient client)) return;
            NetworkObject player = client.PlayerObject;
            // Vị trí được server kiểm chứng để client ở xa không tự gửi lệnh bấm/reset.
            if (player == null || !player.IsSpawned || !player.gameObject.activeInHierarchy) return;
            float squaredDistance = (player.transform.position - _consolePoint.position).sqrMagnitude;
            // Chỉ nhận khoảng cách thỏa điều kiện; NaN cũng phải bị loại thay vì lọt qua phép so sánh >.
            if (!(squaredDistance <= _interactionDistance * _interactionDistance)) return;
            if (ChangeBoard(command)) _netMask.Value = _board.Mask;
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
