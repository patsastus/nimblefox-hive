using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene loading

public class MenuUIHandler : MonoBehaviour
{
    // Reference to the popup panel - drag your HowToPlayPopup here in the Inspector
    public GameObject howToPlayPopup;

    // This method will be called when the "Start" button is clicked
    public void StartNewGame()
    {
        // Load the scene at index 1 in your Build Settings.
        // You can also load by name, e.g., SceneManager.LoadScene("Level1");
        SceneManager.LoadScene(1);
    }

    // This method will be called when the "How To Play" button is clicked
    public void ShowHowToPlay()
    {
        // Make the popup appear
        howToPlayPopup.SetActive(true);
    }

    // This method will be called when the "Close" button on the popup is clicked
    public void CloseHowToPlay()
    {
        // Make the popup disappear
        howToPlayPopup.SetActive(false);
    }

    public void QuitGame()
    {
        // Quit the application
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the Editor
        #endif
    }
}