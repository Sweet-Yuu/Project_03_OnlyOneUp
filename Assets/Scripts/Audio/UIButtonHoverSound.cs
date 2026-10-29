using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Phát âm thanh khi người dùng rê chuột vào button (PointerEnter).
/// Hỗ trợ gọi qua UIButtonSFX.Instance hoặc fallback tự phát clip được gán riêng.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UIButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioClip hoverClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Chỉ phát âm thanh khi button đang có thể tương tác (interactable)
        if (selectable != null && !selectable.interactable) return;

        PlayHover();
    }

    private void PlayHover()
    {
        // 1. Ưu tiên phát clip riêng nếu có gán
        if (hoverClip != null)
        {
            if (UIButtonSFX.Instance != null && UIButtonSFX.Instance.audio != null)
            {
                UIButtonSFX.Instance.audio.PlayOneShot(hoverClip, volume);
                return;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(hoverClip, volume);
                return;
            }
        }

        // 2. Sử dụng clip mặc định cấu hình trên UIButtonSFX
        if (UIButtonSFX.Instance != null)
        {
            UIButtonSFX.Instance.PlayHoverSound();
        }
    }
}

