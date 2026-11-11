using System.Collections.Generic;
using UnityEngine;

// Danh sách các file âm thanh tiếng kêu (Honk) dùng chung giữa Menu chọn và đồng bộ khi chơi.
[CreateAssetMenu(fileName = "HonkCatalog", menuName = "Audio/Honk Catalog")]
public sealed class HonkCatalog : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;

    public IReadOnlyList<AudioClip> Clips => clips;
    public int Count => clips != null ? clips.Length : 0;

    public AudioClip GetClip(int index)
    {
        if (clips == null || clips.Length == 0) return null;
        int safeIndex = Mathf.Clamp(index, 0, clips.Length - 1);
        return clips[safeIndex];
    }

    public static HonkCatalog LoadDefault() => Resources.Load<HonkCatalog>("HonkCatalog");
}

