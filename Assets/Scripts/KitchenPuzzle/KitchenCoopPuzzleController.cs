using System.Collections;
using UnityEngine;

/// <summary>
/// Bộ điều phối câu đố phối hợp nhà bếp (Co-op Kitchen Burner Puzzle).
/// Yêu cầu 2 người chơi mang tổ trứng phối hợp giẫm lên 2 bàn đạp cảm ứng bếp nhiệt cùng lúc
/// (hoặc lần lượt trong khoảng thời gian cho phép khi test solo),
/// sau đó dập tắt hàng rào lửa và mở cửa sập vào kho thực phẩm.
/// </summary>
public sealed class KitchenCoopPuzzleController : MonoBehaviour
{
    [Header("Puzzle Components")]
    [SerializeField] private KitchenPressurePlate[] _plates;
    [SerializeField] private KitchenPuzzleGate _gate;

    [Header("Hazard Obstacle")]
    [Tooltip("Hàng rào lửa/bẫy cháy chắn trước cửa sẽ bị vô hiệu hóa khi giải đố thành công")]
    [SerializeField] private GameObject _fireHazardBarrier;

    [Header("Co-op Rules")]
    [Tooltip("Thời gian ân hạn (giây) giữ trạng thái kích hoạt giúp việc chơi solo test hoặc phối hợp mượt mà hơn")]
    [SerializeField] private float _gracePeriod = 4.0f;

    private readonly float[] _lastPressedTimes = new float[2];
    private bool _isSolved;

    public bool IsSolved => _isSolved;

    /// <summary>
    /// Gán tham chiếu tự động từ KitchenMapBuilder.
    /// </summary>
    public void SetupForEditor(KitchenPressurePlate[] plates, KitchenPuzzleGate gate, GameObject fireHazard)
    {
        _plates = plates;
        _gate = gate;
        _fireHazardBarrier = fireHazard;
    }

    private void OnEnable()
    {
        if (_plates != null)
        {
            for (int i = 0; i < _plates.Length; i++)
            {
                if (_plates[i] != null)
                {
                    _plates[i].OnPlateStateChanged += HandlePlateStateChanged;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (_plates != null)
        {
            for (int i = 0; i < _plates.Length; i++)
            {
                if (_plates[i] != null)
                {
                    _plates[i].OnPlateStateChanged -= HandlePlateStateChanged;
                }
            }
        }
    }

    private void HandlePlateStateChanged(KitchenPressurePlate plate, bool isPressed)
    {
        if (_isSolved) return;

        // Cập nhật mốc thời gian kích hoạt của bàn đạp
        for (int i = 0; i < _plates.Length; i++)
        {
            if (_plates[i] == plate && isPressed)
            {
                if (i < _lastPressedTimes.Length)
                {
                    _lastPressedTimes[i] = Time.time;
                }
                break;
            }
        }

        EvaluatePuzzleState();
    }

    private void EvaluatePuzzleState()
    {
        if (_isSolved || _plates == null || _plates.Length < 2) return;

        bool plate0Active = _plates[0] != null && (_plates[0].IsPressed || (Time.time - _lastPressedTimes[0] <= _gracePeriod));
        bool plate1Active = _plates[1] != null && (_plates[1].IsPressed || (Time.time - _lastPressedTimes[1] <= _gracePeriod));

        // Cả 2 bàn đạp đều được kích hoạt đồng thời
        if (plate0Active && plate1Active)
        {
            SolvePuzzle();
        }
    }

    private void SolvePuzzle()
    {
        if (_isSolved) return;
        _isSolved = true;

        // Khóa các bàn đạp ở vị trí thành công
        for (int i = 0; i < _plates.Length; i++)
        {
            if (_plates[i] != null)
            {
                _plates[i].LockSolved();
            }
        }

        // Tắt hàng rào lửa hazard chắn trước cửa
        if (_fireHazardBarrier != null)
        {
            _fireHazardBarrier.SetActive(false);
        }

        // Mở cửa sập vào kho Pantry
        if (_gate != null)
        {
            _gate.OpenGate();
        }

        Debug.Log("[KitchenPuzzle] Câu đố bếp đã giải thành công! Cửa kho đã mở.");
    }
}
