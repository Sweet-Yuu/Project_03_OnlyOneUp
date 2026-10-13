using UnityEngine;
using UnityEngine.SceneManagement;

public class UnitySceneNavigator : IGameSceneNavigator
{
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Tên Scene cần chuyển đến đang bị trống!");
            return;
        }

        // Khôi phục tốc độ thời gian bình thường trước khi chuyển cảnh
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void QuitApplication()
    {
        Time.timeScale = 1f;
        Debug.Log("Quit Game!");
        Application.Quit();
    }
}
