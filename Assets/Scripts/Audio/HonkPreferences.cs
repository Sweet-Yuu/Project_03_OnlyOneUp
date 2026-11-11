using UnityEngine;

// Lưu chỉ số tiếng kêu cuối cùng người chơi chọn trên máy để dùng lại và đồng bộ khi vào trận.
public static class HonkPreferences
{
    public const string Key = "Player.HonkIndex";

    public static bool HasSavedSelection => PlayerPrefs.HasKey(Key);

    public static int LoadIndex(int clipCount)
    {
        if (clipCount <= 0) return 0;
        int saved = PlayerPrefs.GetInt(Key, 0);
        return ((saved % clipCount) + clipCount) % clipCount;
    }

    public static void SaveIndex(int index)
    {
        PlayerPrefs.SetInt(Key, Mathf.Max(0, index));
        PlayerPrefs.Save();
    }
}

