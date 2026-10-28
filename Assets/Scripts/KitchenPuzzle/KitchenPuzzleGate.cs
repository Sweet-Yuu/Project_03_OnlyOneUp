using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý cánh cửa sập / rào chắn nhà bếp chặn lối vào kho thực phẩm.
/// Nâng cửa trượt mượt mà lên trần khi giải xong câu đố và đổi màu đèn báo hiệu sang xanh.
/// </summary>
public sealed class KitchenPuzzleGate : MonoBehaviour
{
    [Header("Door Movement")]
    [SerializeField] private Transform _slidingShutter;
    [SerializeField] private Vector3 _openOffset = new Vector3(0f, 10f, 0f);
    [SerializeField] private float _openSpeed = 4f;

    [Header("Status Indicators")]
    [SerializeField] private Renderer[] _statusLamps;
    [SerializeField] private Material _lockedMaterial;
    [SerializeField] private Material _unlockedMaterial;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _openClip;

    private Vector3 _initialLocalPos;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    /// <summary>
    /// Gán tham chiếu tự động từ KitchenMapBuilder.
    /// </summary>
    public void SetupForEditor(Transform shutter, Renderer[] lamps, Material lockedMat, Material unlockedMat)
    {
        _slidingShutter = shutter;
        _statusLamps = lamps;
        _lockedMaterial = lockedMat;
        _unlockedMaterial = unlockedMat;
    }

    private void Awake()
    {
        if (_slidingShutter != null)
        {
            _initialLocalPos = _slidingShutter.localPosition;
        }

        UpdateLampVisuals(false);
    }

    /// <summary>
    /// Kích hoạt mở cửa sập và nâng dần lên trần nhà.
    /// </summary>
    public void OpenGate()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (_audioSource != null && _openClip != null)
        {
            _audioSource.PlayOneShot(_openClip);
        }

        UpdateLampVisuals(true);

        if (_slidingShutter != null)
        {
            StartCoroutine(SlideOpenRoutine());
        }
    }

    private IEnumerator SlideOpenRoutine()
    {
        Vector3 targetPos = _initialLocalPos + _openOffset;
        while (Vector3.Distance(_slidingShutter.localPosition, targetPos) > 0.01f)
        {
            _slidingShutter.localPosition = Vector3.MoveTowards(_slidingShutter.localPosition, targetPos, _openSpeed * Time.deltaTime);
            yield return null;
        }
        _slidingShutter.localPosition = targetPos;
    }

    private void UpdateLampVisuals(bool unlocked)
    {
        if (_statusLamps == null) return;
        Material mat = unlocked ? _unlockedMaterial : _lockedMaterial;
        if (mat == null) return;

        for (int i = 0; i < _statusLamps.Length; i++)
        {
            if (_statusLamps[i] != null)
            {
                _statusLamps[i].sharedMaterial = mat;
            }
        }
    }
}
