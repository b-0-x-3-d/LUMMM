using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    //This script can be used to load any scene
    public void LoadScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
        Debug.Log("New level");
    }

    //Method to reset the current level
    public void ResetLevel() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Reset level");
    }

    //Method to exit the application
    public void ExitApplication() {
        Application.Quit();
        Debug.Log("Exit Application");
    }
}
