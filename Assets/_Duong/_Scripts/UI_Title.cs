using UnityEngine.SceneManagement;
using UnityEngine;
public class UI_Title : MonoBehaviour
{
    public string sceneName = "MainMenu";
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene(sceneName);
    }
   
}
