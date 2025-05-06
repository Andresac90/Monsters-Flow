using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // Load a scene by name
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Quit the game
    public void QuitGame()
    {
        Debug.Log("Quitting Game..."); 
        Application.Quit();
    }
}
