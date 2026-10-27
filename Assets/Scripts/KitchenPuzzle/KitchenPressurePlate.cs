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

    private readonly HashSet<Collider> _occupants = new HashSet<Collider>();
    private Vector3 _unpressedLocalPos;
    private Vector3 _pressedLocalPos;
    private Coroutine _motionRoutine;
    private bool _isPressed;
    private bool _isLocked;

    public bool IsPressed => _isPressed || _isLocked;
    public bool IsLocked => _isLocked;

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
            _unpressedLocalPos = _movingPlate.localPosition;
            _pressedLocalPos = _unpressedLocalPos - new Vector3(0f, _pressDepth, 0f);
        }

        UpdateVisuals(PlateVisualState.Idle);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isLocked) return;
        if (!IsValidOccupant(other)) return;

        bool wasEmpty = _occupants.Count == 0;
        _occupants.Add(other);

        if (wasEmpty)
        {
            SetPressed(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isLocked) return;
        if (!IsValidOccupant(other)) return;

        _occupants.Remove(other);

        if (_occupants.Count == 0)
        {
            SetPressed(false);
        }
    }

    private bool IsValidOccupant(Collider col)
    {
        // Chấp nhận người chơi (Tag Player / Layer 7) hoặc tổ trứng (Layer 8 / Tag Nest)
        return col.CompareTag("Player") || col.gameObject.layer == 7 || col.gameObject.layer == 8 || col.CompareTag("Nest");
    }

    private void SetPressed(bool pressed)
    {
        if (_isPressed == pressed) return;
        _isPressed = pressed;

        if (pressed && _audioSource != null && _pressClip != null)
        {
            _audioSource.PlayOneShot(_pressClip);
        }

        AnimatePosition(pressed ? _pressedLocalPos : _unpressedLocalPos);
        UpdateVisuals(pressed ? PlateVisualState.Active : PlateVisualState.Idle);

        OnPlateStateChanged?.Invoke(this, pressed);
    }

    /// <summary>
    /// Khóa vĩnh viễn bàn đạp ở trạng thái đã giải xong (màu xanh lá, chìm xuống cố định).
    /// </summary>
    public void LockSolved()
    {
        _isLocked = true;
        _isPressed = true;
        _occupants.Clear();

        AnimatePosition(_pressedLocalPos);
        UpdateVisuals(PlateVisualState.Solved);
    }

    private void AnimatePosition(Vector3 targetLocalPos)
    {
        if (_movingPlate == null) return;
        if (_motionRoutine != null) StopCoroutine(_motionRoutine);
        _motionRoutine = StartCoroutine(MoveRoutine(targetLocalPos));
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
