using UnityEngine;

public sealed class PlayerNamePreferences : IPlayerNameStore
{
    public const string Key = "Player.DisplayName";
    public string Load() => PlayerPrefs.GetString(Key, string.Empty);
    public void Save(string name)
    {
        PlayerPrefs.SetString(Key, name);
        PlayerPrefs.Save();
    }
}
