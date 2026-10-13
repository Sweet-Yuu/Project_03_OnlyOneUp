using UnityEngine.SceneManagement;
using UnityEngine;
public class UI_Title : MonoBehaviour
{
    public string sceneName = "MainMenu";

    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene(sceneName);
    }
   public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
    public void OnCreditsButtonClicked()
    {
        SceneManager.LoadScene("Credit");
    }
    public void OnMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void OnBackButtonClicked()
    {
        SceneManager.LoadScene(sceneName);
    }
}
