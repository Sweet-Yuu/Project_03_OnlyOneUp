using UnityEngine.SceneManagement;

public static class GameResultSession
{
    private const string ResultSceneName = "GameWin&Over";

    public static bool IsGameWin { get; private set; }
    public static string PreviousSceneName { get; private set; } = string.Empty;
    public static bool HasRecordedResult { get; private set; }

    /// <summary>
    /// Lưu trạng thái kết quả (Thắng/Thua) và tên màn chơi vừa kết thúc trước khi mở Scene GameWin&Over
    /// </summary>
    public static void OpenResultScene(bool isWin, string levelSceneName)
    {
        IsGameWin = isWin;
        PreviousSceneName = levelSceneName;
        HasRecordedResult = true;

        SceneManager.LoadScene(ResultSceneName);
    }
}
