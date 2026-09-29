using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static bool isGameWin = false;
    public static string previousSceneName = "";

    [Header("UI Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameWinPanel;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameWinPanel != null) gameWinPanel.SetActive(false);

        if (isGameWin)
        {
            if (gameWinPanel != null) gameWinPanel.SetActive(true);
        }
        else
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }
    }
    public void RetryLevel()
    {
        Time.timeScale = 1f;
        
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            SceneManager.LoadScene(previousSceneName);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy tên Scene màn chơi trước đó!");
        }
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); 
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }
    ///UIManager.TriggerGameOver(SceneManager.GetActiveScene().name);
    public static void TriggerGameOver(string levelName)
    {
        isGameWin = false;
        previousSceneName = levelName;
        SceneManager.LoadScene("GameOver&Win"); 
    }
    ///UIManager.TriggerGameWin(SceneManager.GetActiveScene().name);
    public static void TriggerGameWin(string levelName)
    {
        isGameWin = true;
        previousSceneName = levelName;
        SceneManager.LoadScene("GameOver&Win"); 
    }
}
