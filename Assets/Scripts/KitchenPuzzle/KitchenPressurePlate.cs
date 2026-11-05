using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý bàn đạp cảm ứng bếp nhiệt (Pressure Plate) cho câu đố nhà bếp.
/// Nhận diện nhân vật (Player) hoặc tổ trứng (Nest) bước lên, tạo hiệu ứng ấn xuống và đổi màu phát sáng.
/// </summary>
public sealed class KitchenPressurePlate : MonoBehaviour
{
    public event Action<KitchenPressurePlate, bool> OnPlateStateChanged;

    [Header("Visual Elements")]
    [SerializeField] private Transform _movingPlate;
    [SerializeField] private Renderer _indicatorRenderer;
    [SerializeField] private Light _plateLight;

    [Header("Materials / Colors")]
    [SerializeField] private Material _idleMaterial;
    [SerializeField] private Material _activeMaterial;
    [SerializeField] private Material _solvedMaterial;
    [SerializeField] private Color _idleLightColor = new Color(1f, 0.2f, 0.1f);
    [SerializeField] private Color _activeLightColor = new Color(1f, 0.75f, 0.1f);
    [SerializeField] private Color _solvedLightColor = new Color(0.2f, 1f, 0.3f);

    [Header("Motion Settings")]
    [SerializeField] private float _pressDepth = 0.15f;
    [SerializeField] private float _moveSpeed = 6f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _pressClip;

    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private Vector3 unpressedLocalPos;
    private Vector3 pressedLocalPos;
    private Coroutine motionRoutine;
    private bool isPressed;
    private bool isLocked;

    public bool IsPressed => isPressed || isLocked;
    public bool IsLocked => isLocked;

    /// <summary>
    /// Gán tham chiếu và thông số tự động từ KitchenMapBuilder.
    /// </summary>
    public void SetupForEditor(Transform movingPlate, Renderer indicator, Light light,
        Material idleMat, Material activeMat, Material solvedMat)
    {
        _movingPlate = movingPlate;
        _indicatorRenderer = indicator;
        _plateLight = light;
        _idleMaterial = idleMat;
        _activeMaterial = activeMat;
        _solvedMaterial = solvedMat;
    }

    private void Awake()
    {
        if (_movingPlate != null)
        {
            unpressedLocalPos = _movingPlate.localPosition;
            pressedLocalPos = unpressedLocalPos - new Vector3(0f, _pressDepth, 0f);
        }

        UpdateVisuals(PlateVisualState.Idle);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLocked) return;
        if (!IsValidOccupant(other)) return;

        bool wasEmpty = occupants.Count == 0;
        occupants.Add(other);

        if (wasEmpty)
        {
            SetPressed(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isLocked) return;
        if (!IsValidOccupant(other)) return;

        occupants.Remove(other);

        if (occupants.Count == 0)
        {
            SetPressed(false);
        }
    }

    private bool IsValidOccupant(Collider col)
    {
        if (col == null) return false;
        if (col.CompareTag("Player") || col.gameObject.layer == 7 || col.gameObject.layer == 8 || col.CompareTag("Nest")) return true;
        if (col.GetComponentInParent<CharacterController>() != null) return true;
        var netObj = col.GetComponentInParent<Unity.Netcode.NetworkObject>();
        return netObj != null && netObj.IsPlayerObject;
    }

    private void SetPressed(bool pressed)
    {
        if (isPressed == pressed) return;
        isPressed = pressed;

        if (pressed && _audioSource != null && _pressClip != null)
        {
            _audioSource.PlayOneShot(_pressClip);
        }

        AnimatePosition(pressed ? pressedLocalPos : unpressedLocalPos);
        UpdateVisuals(pressed ? PlateVisualState.Active : PlateVisualState.Idle);

        OnPlateStateChanged?.Invoke(this, pressed);
    }

    /// <summary>
    /// Đồng bộ trạng thái trực quan (chìm xuống / đổi màu) và âm thanh từ mạng.
    /// Giúp Host và Client luôn nhìn thấy đối phương khi bước lên hoặc rời khỏi bàn đạp.
    /// </summary>
    public void SetVisualState(bool pressed, bool playSound)
    {
        if (isLocked) return;

        // Nếu vị trí hiển thị hiện tại đã khớp thì không phát lặp âm thanh hoặc chạy đè Coroutine
        bool wasVisualPressed = _movingPlate != null && Vector3.Distance(_movingPlate.localPosition, pressedLocalPos) < 0.02f;
        if (wasVisualPressed == pressed) return;

        if (playSound && pressed && _audioSource != null && _pressClip != null)
        {
            _audioSource.PlayOneShot(_pressClip);
        }

        AnimatePosition(pressed ? pressedLocalPos : unpressedLocalPos);
        UpdateVisuals(pressed ? PlateVisualState.Active : PlateVisualState.Idle);
    }

    /// <summary>
    /// Khóa vĩnh viễn bàn đạp ở trạng thái đã giải xong (màu xanh lá, chìm xuống cố định).
    /// </summary>
    public void LockSolved()
    {
        isLocked = true;
        isPressed = true;
        occupants.Clear();

        AnimatePosition(pressedLocalPos);
        UpdateVisuals(PlateVisualState.Solved);
    }

    private void AnimatePosition(Vector3 targetLocalPos)
    {
        if (_movingPlate == null) return;
        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = StartCoroutine(MoveRoutine(targetLocalPos));
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        while (Vector3.Distance(_movingPlate.localPosition, target) > 0.001f)
        {
            _movingPlate.localPosition = Vector3.MoveTowards(_movingPlate.localPosition, target, _moveSpeed * Time.deltaTime);
            yield return null;
        }
        _movingPlate.localPosition = target;
    }

    private void UpdateVisuals(PlateVisualState state)
    {
        Material targetMat = state switch
        {
            PlateVisualState.Active => _activeMaterial,
            PlateVisualState.Solved => _solvedMaterial,
            _ => _idleMaterial
        };

        if (_indicatorRenderer != null && targetMat != null)
        {
            _indicatorRenderer.sharedMaterial = targetMat;
        }

        if (_plateLight != null)
        {
            _plateLight.color = state switch
            {
                PlateVisualState.Active => _activeLightColor,
                PlateVisualState.Solved => _solvedLightColor,
                _ => _idleLightColor
            };
        }
    }

    private enum PlateVisualState { Idle, Active, Solved }
}
