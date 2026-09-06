using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIHandler : MonoBehaviour
{
    // Reference to the popup panel - drag your HowToPlayPopup here in the Inspector
    public GameObject howToPlayPopup;
    
    // Reference to the logo - drag your logo GameObject here in the Inspector
    public GameObject logo;

    // Reference to the GiveUp button
    public GameObject giveUpButton;

    // This method will be called when the "Start" button is clicked
    public void StartNewGame()
    {
        // Load the scene at index 1 in your Build Settings.
        SceneManager.LoadScene("FirstLightWords");
    }

    // This method will be called when the "How To Play" button is clicked
    public void ShowHowToPlay()
    {
        // Show the popup
        howToPlayPopup.SetActive(true);
        
        // Hide the logo
        if (logo != null)
        {
            logo.SetActive(false);
        }
    }

    // This method will be called when the "Close" button on the popup is clicked
    public void CloseHowToPlay()
    {
        // Hide the popup
        howToPlayPopup.SetActive(false);
        
        // Show the logo again
        if (logo != null)
        {
            logo.SetActive(true);
        }
    }

    public void QuitGame()
    {
        // Quit the application
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the Editor
        #endif
    }
    
    // NEW: Give Up method
    public void GiveUp()
    {
        // Find the SimpleWordChooser in the scene
        SimpleWordChooser wordChooser = FindObjectOfType<SimpleWordChooser>();
        
        if (wordChooser != null)
        {
            // Trigger defeat using reflection (since EndGame is private)
            var method = wordChooser.GetType().GetMethod("EndGame", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(wordChooser, new object[] { false });
            }
        }
        else
        {
            Debug.LogWarning("SimpleWordChooser not found!");
        }
        
        // Hide the GiveUp button
        if (giveUpButton != null)
        {
            giveUpButton.SetActive(false);
        }
    }
}