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

        [Header("Debug & Quick Testing")]
        [Tooltip("Bật để tự động hoàn thành câu đố và mở cổng ngay lập tức phục vụ test game.")]
        [SerializeField] private bool instantSolve;

        [Header("Pass Through & Sinking On Solved")]
        [Tooltip("Tự động chuyển các collider của bảng gợi ý và bàn nhập mã thành trigger khi giải xong để người chơi đi xuyên qua.")]
        [SerializeField] private bool passThroughOnSolved = true;
        [Tooltip("Tự động hạ bảng gợi ý và bàn nhập mã chìm xuống mặt bàn khi giải xong.")]
        [SerializeField] private bool sinkBoardsOnSolved = true;
        [SerializeField] private Transform clueBoardTransform;
        [SerializeField] private Transform consoleTransform;
        [SerializeField] private float sinkOffset = 5f;
        [SerializeField] private float sinkSpeed = 6f;

        private readonly NetworkVariable<bool> _unlocked = new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private IPuzzleInteractable _gate;
        private SymbolCipherLock _cipher;
        private bool lastInstantSolve;
        private Collider[] boardColliders;
        private Vector3 clueBoardDefaultPos;
        private Vector3 consoleDefaultPos;
        private bool originalPositionsRecorded;

        public bool IsSolved => instantSolve || (IsSpawned ? _unlocked.Value : _cipher != null && _cipher.IsUnlocked);
        public bool IsReady => isActiveAndEnabled && _cipher != null &&
            (!IsNetworkSessionActive || IsSpawned);
        public bool IsNetworkSessionActive => NetworkManager != null && NetworkManager.IsListening;
        public event Action<bool> SubmissionCompleted;
        public event Action<bool> SolvedChanged;

        private void Awake()
        {
            _gate = _gateBehaviour as IPuzzleInteractable;
            InitializePassThrough();
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
            lastInstantSolve = instantSolve;
            InitializePassThrough();
            if (IsReady) ApplySolved(IsSolved);
        }

        private void Update()
        {
            if (instantSolve != lastInstantSolve)
            {
                lastInstantSolve = instantSolve;
                if (IsServer && IsSpawned)
                {
                    _unlocked.Value = instantSolve;
                }
                ApplySolved(IsSolved);
            }

            if (sinkBoardsOnSolved && originalPositionsRecorded)
            {
                UpdateBoardSinking();
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
                    if (IsServer && IsSpawned)
                    {
                        _unlocked.Value = instantSolve;
                    }
                    ApplySolved(IsSolved);
                }
            }
        }
#endif

        public override void OnNetworkSpawn()
        {
            _unlocked.OnValueChanged += HandleUnlocked;
            if (IsServer && _cipher != null && instantSolve)
            {
                _unlocked.Value = true;
            }
            if (_cipher != null && _gate != null) ApplySolved(IsSolved);
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
            if (IsReady && entry >= 0 && entry <= 999)
            {
                bool distanceOk = true;
                if (NetworkManager != null && NetworkManager.ConnectedClients.TryGetValue(sender, out NetworkClient client))
                {
                    NetworkObject player = client.PlayerObject;
                    if (player != null && player.IsSpawned && player.gameObject.activeInHierarchy && _consolePoint != null)
                    {
                        float squaredDistance = (player.transform.position - _consolePoint.position).sqrMagnitude;
                        float maxAllowedDistance = Mathf.Max(_interactionDistance, 35f);
                        if (squaredDistance > maxAllowedDistance * maxAllowedDistance) distanceOk = false;
                    }
                }

                if (distanceOk)
                {
                    accepted = _cipher.TryUnlock(entry.ToString("D3", CultureInfo.InvariantCulture));
                    if (accepted)
                    {
                        _unlocked.Value = true;
                        if (IsServer)
                        {
                            ApplySolved(true);
                        }
                    }
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
            if (_gate != null) _gate.SetActive(solved);
            SolvedChanged?.Invoke(solved);

            InitializePassThrough();
            if (passThroughOnSolved && boardColliders != null)
            {
                foreach (Collider col in boardColliders)
                {
                    if (col != null)
                    {
                        col.isTrigger = solved;
                    }
                }
            }
        }

        private void InitializePassThrough()
        {
            if (originalPositionsRecorded) return;
            originalPositionsRecorded = true;

            if (clueBoardTransform == null)
            {
                clueBoardTransform = transform.Find("Puzzle/ClueBoard");
                if (clueBoardTransform == null) clueBoardTransform = transform.Find("ClueBoard");
            }

            if (consoleTransform == null)
            {
                consoleTransform = transform.Find("Puzzle/CipherConsole");
                if (consoleTransform == null) consoleTransform = transform.Find("CipherConsole");
            }

            if (clueBoardTransform != null) clueBoardDefaultPos = clueBoardTransform.localPosition;
            if (consoleTransform != null) consoleDefaultPos = consoleTransform.localPosition;

            var collidersList = new System.Collections.Generic.List<Collider>();
            if (clueBoardTransform != null)
            {
                foreach (Collider col in clueBoardTransform.GetComponentsInChildren<Collider>(true))
                {
                    if (!col.isTrigger) collidersList.Add(col);
                }
            }
            if (consoleTransform != null)
            {
                foreach (Collider col in consoleTransform.GetComponentsInChildren<Collider>(true))
                {
                    if (!col.isTrigger) collidersList.Add(col);
                }
            }
            boardColliders = collidersList.ToArray();
        }

        private void UpdateBoardSinking()
        {
            bool solved = IsSolved;
            Vector3 clueTarget = solved ? clueBoardDefaultPos - new Vector3(0f, sinkOffset, 0f) : clueBoardDefaultPos;
            Vector3 consoleTarget = solved ? consoleDefaultPos - new Vector3(0f, sinkOffset, 0f) : consoleDefaultPos;

            if (clueBoardTransform != null)
            {
                clueBoardTransform.localPosition = Vector3.MoveTowards(
                    clueBoardTransform.localPosition, clueTarget, sinkSpeed * Time.deltaTime);
            }
            if (consoleTransform != null)
            {
                consoleTransform.localPosition = Vector3.MoveTowards(
                    consoleTransform.localPosition, consoleTarget, sinkSpeed * Time.deltaTime);
            }
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
