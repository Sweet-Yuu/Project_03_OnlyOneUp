using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverHandler : MonoBehaviour
{
    [SerializeField] private EggController eggController;
    [SerializeField] private GameObject gameOverPanel;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (eggController != null)
            eggController.OnEggBroken += HandleEggBroken;
    }

    private void OnDestroy()
    {
        if (eggController != null)
            eggController.OnEggBroken -= HandleEggBroken;
    }

    private void HandleEggBroken()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
