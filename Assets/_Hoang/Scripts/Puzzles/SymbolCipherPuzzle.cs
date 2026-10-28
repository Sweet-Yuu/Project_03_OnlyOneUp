using System;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Hoang.Puzzles
{
    [SelectionBase]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class SymbolCipherPuzzle : NetworkBehaviour
    {
        [Header("Clues: Circle + Triangle, Triangle + Square, Circle + Square")]
        [SerializeField, Range(0, 18)] private int _circleTriangleSum = 7;
        [SerializeField, Range(0, 18)] private int _triangleSquareSum = 5;
        [SerializeField, Range(0, 18)] private int _circleSquareSum = 8;

        [Header("Puzzle References")]
        [Tooltip("Component thực thi IPuzzleInteractable, ví dụ PuzzleGate.")]
        [SerializeField] private MonoBehaviour _gateBehaviour;
        [SerializeField] private TMP_Text _clueText;
        [SerializeField] private Transform _consolePoint;
        [SerializeField, Min(0.1f)] private float _interactionDistance = 5f;

        private readonly NetworkVariable<bool> _unlocked = new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private IPuzzleInteractable _gate;
        private SymbolCipherLock _cipher;

        public bool IsSolved => IsSpawned ? _unlocked.Value : _cipher != null && _cipher.IsUnlocked;
        public bool IsReady => isActiveAndEnabled && _cipher != null &&
            (!IsNetworkSessionActive || IsSpawned);
        public bool IsNetworkSessionActive => NetworkManager != null && NetworkManager.IsListening;
        public event Action<bool> SubmissionCompleted;
        public event Action<bool> SolvedChanged;

        private void Awake()
        {
            _gate = _gateBehaviour as IPuzzleInteractable;
            try
            {
                _cipher = new SymbolCipherLock(_circleTriangleSum, _triangleSquareSum, _circleSquareSum);
                if (_gate == null || _clueText == null || _consolePoint == null ||
                    float.IsNaN(_interactionDistance) || float.IsInfinity(_interactionDistance) || _interactionDistance <= 0f)
                    throw new ArgumentException("Thiếu cổng, bảng gợi ý, điểm tương tác hoặc khoảng cách không hợp lệ.");
                RefreshClues();
            }
            catch (ArgumentException error)
            {
                Debug.LogError($"[SymbolCipherPuzzle] {error.Message}", this);
                if (_clueText != null) _clueText.text = "CÂU ĐỐ CHƯA ĐƯỢC CẤU HÌNH ĐÚNG";
                enabled = false;
            }
        }

        private void Start()
        {
            if (IsReady) ApplySolved(IsSolved);
        }

        public override void OnNetworkSpawn()
        {
            _unlocked.OnValueChanged += HandleUnlocked;
            if (_cipher != null && _gate != null) ApplySolved(_unlocked.Value);
        }

        public override void OnNetworkDespawn()
        {
            _unlocked.OnValueChanged -= HandleUnlocked;
        }

        public void SubmitCode(string entry)
        {
            if (!IsReady)
            {
                SubmissionCompleted?.Invoke(false);
                return;
            }
            if (!IsSpawned)
            {
                bool wasSolved = IsSolved;
                bool accepted = _cipher.TryUnlock(entry);
                if (accepted && !wasSolved) ApplySolved(true);
                SubmissionCompleted?.Invoke(accepted);
                return;
            }
            if (entry == null || entry.Length != 3 || entry[0] < '0' || entry[0] > '9' ||
                entry[1] < '0' || entry[1] > '9' || entry[2] < '0' || entry[2] > '9')
            {
                SubmissionCompleted?.Invoke(false);
                return;
            }
            SubmitCodeRpc((entry[0] - '0') * 100 + (entry[1] - '0') * 10 + entry[2] - '0');
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitCodeRpc(int entry, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            bool accepted = false;
            if (IsReady && entry >= 0 && entry <= 999 &&
                NetworkManager.ConnectedClients.TryGetValue(sender, out NetworkClient client))
            {
                NetworkObject player = client.PlayerObject;
                // Kiểm tra lại trên server, không tin việc client tự nhận đang đứng gần bàn nhập.
                if (player != null && player.IsSpawned && player.gameObject.activeInHierarchy &&
                    (player.transform.position - _consolePoint.position).sqrMagnitude <= _interactionDistance * _interactionDistance)
                {
                    accepted = _cipher.TryUnlock(entry.ToString("D3", CultureInfo.InvariantCulture));
                    if (accepted) _unlocked.Value = true;
                }
            }
            SubmissionResultRpc(sender, accepted);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void SubmissionResultRpc(ulong sender, bool accepted)
        {
            if (NetworkManager.LocalClientId == sender) SubmissionCompleted?.Invoke(accepted);
        }

        private void HandleUnlocked(bool previous, bool current) => ApplySolved(current);

        private void ApplySolved(bool solved)
        {
            _gate.SetActive(solved);
            SolvedChanged?.Invoke(solved);
        }

        private void RefreshClues()
        {
            _clueText.text = $"GIẢI MÃ CỔNG ĐÁ\n\n" +
                $"<color=#F7C55B>●</color> + <color=#68D5EF>▲</color> = {_circleTriangleSum}\n" +
                $"<color=#68D5EF>▲</color> + <color=#A4DE83>■</color> = {_triangleSquareSum}\n" +
                $"<color=#F7C55B>●</color> + <color=#A4DE83>■</color> = {_circleSquareSum}\n\n" +
                "Mỗi ký hiệu là một chữ số 0–9.\n" +
                "Nhập theo thứ tự: <color=#F7C55B>●</color>  <color=#68D5EF>▲</color>  <color=#A4DE83>■</color>";
        }
    }
}
