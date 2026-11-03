using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Bộ điều phối câu đố phối hợp nhà bếp (Co-op Kitchen Burner Puzzle).
/// Yêu cầu 2 người chơi mang tổ trứng phối hợp giẫm lên 2 bàn đạp cảm ứng bếp nhiệt cùng lúc
/// (hoặc lần lượt trong khoảng thời gian cho phép khi test solo),
/// sau đó dập tắt hàng rào lửa và mở cửa sập vào kho thực phẩm.
/// Đồng bộ trạng thái giải đố và hình ảnh lún/đổi màu của từng bàn đạp toàn mạng bằng NetworkVariable và ServerRpc.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public sealed class KitchenCoopPuzzleController : NetworkBehaviour
{
    [Header("Puzzle Components")]
    [FormerlySerializedAs("_plates")]
    [SerializeField] private KitchenPressurePlate[] plates;

    [FormerlySerializedAs("_gate")]
    [SerializeField] private KitchenPuzzleGate gate;

    [Header("Hazard Obstacle")]
    [Tooltip("Hàng rào lửa/băng chắn trước cửa sẽ bị vô hiệu hóa khi giải đố thành công")]
    [FormerlySerializedAs("_fireHazardBarrier")]
    [SerializeField] private GameObject fireHazardBarrier;

    [Header("Co-op Rules")]
    [Tooltip("Thời gian ân hạn (giây) giữ trạng thái kích hoạt giúp việc chơi solo test hoặc phối hợp mượt mà hơn")]
    [FormerlySerializedAs("_gracePeriod")]
    [SerializeField] private float gracePeriod = 4.0f;

    // Biến mạng đồng bộ trạng thái giải đố tới toàn bộ Client và Host
    private readonly NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Biến mạng đồng bộ trạng thái giẫm/nhả của từng bàn đạp để cả 2 người chơi nhìn thấy đối phương
    private readonly NetworkVariable<bool> isPlate0ActiveNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<bool> isPlate1ActiveNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly float[] lastPressedTimes = new float[2];
    private bool isSolvedLocal;

    public bool IsSolved => IsSpawned ? isSolvedNet.Value : isSolvedLocal;

    /// <summary>
    /// Gán tham chiếu tự động từ KitchenMapBuilder.
    /// </summary>
    public void SetupForEditor(KitchenPressurePlate[] newPlates, KitchenPuzzleGate newGate, GameObject fireHazard)
    {
        plates = newPlates;
        gate = newGate;
        fireHazardBarrier = fireHazard;
    }

    private void OnEnable()
    {
        if (plates != null)
        {
            for (int i = 0; i < plates.Length; i++)
            {
                if (plates[i] != null)
                {
                    plates[i].OnPlateStateChanged += HandlePlateStateChanged;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (plates != null)
        {
            for (int i = 0; i < plates.Length; i++)
            {
                if (plates[i] != null)
                {
                    plates[i].OnPlateStateChanged -= HandlePlateStateChanged;
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        isSolvedNet.OnValueChanged += HandleNetSolvedChanged;
        isPlate0ActiveNet.OnValueChanged += HandlePlate0ActiveChanged;
        isPlate1ActiveNet.OnValueChanged += HandlePlate1ActiveChanged;

        // Nếu câu đố đã được giải trước khi Client này spawn vào mạng, áp dụng ngay tức thì
        if (isSolvedNet.Value)
        {
            ApplySolvedState(true);
        }
        else
        {
            // Đồng bộ trạng thái trực quan ban đầu nếu đã có ai đứng trên bàn đạp từ trước
            if (plates != null && plates.Length > 0 && plates[0] != null)
            {
                plates[0].SetVisualState(isPlate0ActiveNet.Value, false);
            }
            if (plates != null && plates.Length > 1 && plates[1] != null)
            {
                plates[1].SetVisualState(isPlate1ActiveNet.Value, false);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        isSolvedNet.OnValueChanged -= HandleNetSolvedChanged;
        isPlate0ActiveNet.OnValueChanged -= HandlePlate0ActiveChanged;
        isPlate1ActiveNet.OnValueChanged -= HandlePlate1ActiveChanged;
    }

    private void HandleNetSolvedChanged(bool previous, bool current)
    {
        if (current)
        {
            ApplySolvedState(false);
        }
    }

    private void HandlePlate0ActiveChanged(bool previous, bool current)
    {
        if (plates != null && plates.Length > 0 && plates[0] != null)
        {
            plates[0].SetVisualState(current, playSound: current && !previous);
        }
    }

    private void HandlePlate1ActiveChanged(bool previous, bool current)
    {
        if (plates != null && plates.Length > 1 && plates[1] != null)
        {
            plates[1].SetVisualState(current, playSound: current && !previous);
        }
    }

    private void HandlePlateStateChanged(KitchenPressurePlate plate, bool isPressed)
    {
        if (IsSolved) return;

        int plateIndex = -1;
        if (plates != null)
        {
            for (int i = 0; i < plates.Length; i++)
            {
                if (plates[i] == plate)
                {
                    plateIndex = i;
                    break;
                }
            }
        }

        if (plateIndex < 0) return;

        // Nếu đang ở trong phiên mạng và là Client: gửi RPC báo trạng thái giẫm/nhả lên Server
        if (IsSpawned && !IsServer)
        {
            ReportPlateStateServerRpc(plateIndex, isPressed);
            return;
        }

        // Nếu là Server (Host)
        if (IsSpawned && IsServer)
        {
            SetPlateActiveOnServer(plateIndex, isPressed);
            return;
        }

        // Nếu là phiên offline không kết nối mạng
        if (isPressed && plateIndex < lastPressedTimes.Length)
        {
            lastPressedTimes[plateIndex] = Time.time;
        }

        EvaluatePuzzleState();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportPlateStateServerRpc(int plateIndex, bool isPressed, RpcParams rpcParams = default)
    {
        if (isSolvedNet.Value || plates == null || plateIndex < 0 || plateIndex >= plates.Length) return;

        // Xác thực người chơi gửi RPC có đứng gần bàn đạp không khi ấn (chống gửi lệnh từ xa)
        if (isPressed)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            if (NetworkManager != null && NetworkManager.ConnectedClients.TryGetValue(senderId, out NetworkClient client))
            {
                NetworkObject player = client.PlayerObject;
                if (player != null && plates[plateIndex] != null)
                {
                    float sqrDist = (player.transform.position - plates[plateIndex].transform.position).sqrMagnitude;
                    // Bàn đạp đường kính ~3m, cho phép khoảng cách mở rộng tới 15m để bù trễ mạng
                    if (sqrDist > 15f * 15f) return;
                }
            }
        }

        SetPlateActiveOnServer(plateIndex, isPressed);
    }

    private void SetPlateActiveOnServer(int plateIndex, bool isPressed)
    {
        if (plateIndex == 0)
        {
            isPlate0ActiveNet.Value = isPressed;
        }
        else if (plateIndex == 1)
        {
            isPlate1ActiveNet.Value = isPressed;
        }

        if (isPressed && plateIndex < lastPressedTimes.Length)
        {
            lastPressedTimes[plateIndex] = Time.time;
        }

        EvaluatePuzzleState();
    }

    private void EvaluatePuzzleState()
    {
        if (IsSolved || plates == null || plates.Length < 2) return;
        if (IsSpawned && !IsServer) return;

        // Bàn đạp được tính là đang kích hoạt nếu đang được giẫm đè hoặc trong thời gian ân hạn gracePeriod
        bool plate0Active = (IsSpawned ? isPlate0ActiveNet.Value : (plates[0] != null && plates[0].IsPressed))
                            || (Time.time - lastPressedTimes[0] <= gracePeriod);
        bool plate1Active = (IsSpawned ? isPlate1ActiveNet.Value : (plates[1] != null && plates[1].IsPressed))
                            || (Time.time - lastPressedTimes[1] <= gracePeriod);

        // Cả 2 bàn đạp đều được kích hoạt đồng thời (hoặc trong gracePeriod)
        if (plate0Active && plate1Active)
        {
            if (IsSpawned && IsServer)
            {
                isSolvedNet.Value = true;
            }
            else if (!IsSpawned)
            {
                ApplySolvedState(false);
            }
        }
    }

    private void ApplySolvedState(bool instant)
    {
        if (isSolvedLocal) return;
        isSolvedLocal = true;

        // Khóa các bàn đạp ở vị trí thành công (màu xanh lá)
        if (plates != null)
        {
            for (int i = 0; i < plates.Length; i++)
            {
                if (plates[i] != null)
                {
                    plates[i].LockSolved();
                }
            }
        }

        // Tắt hàng rào lửa / băng chắn trước cửa
        if (fireHazardBarrier != null)
        {
            fireHazardBarrier.SetActive(false);
        }

        // Mở cửa sập
        if (gate != null)
        {
            gate.OpenGate(instant);
        }

        Debug.Log($"[KitchenPuzzle] {gameObject.name}: Đã mở cửa và đồng bộ thành công!");
    }
}
