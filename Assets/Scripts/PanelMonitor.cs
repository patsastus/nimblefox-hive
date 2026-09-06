using UnityEngine;

public class PanelMonitor : MonoBehaviour
{
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public GameObject giveUpButton;
    
    void Start()
    {
        // Make sure GiveUp button is visible at start
        if (giveUpButton != null)
        {
            giveUpButton.SetActive(true);
        }
    }
    
    void Update()
    {
        if (giveUpButton == null) return;
        
        // Check if either panel is active
        bool victoryActive = victoryPanel != null && victoryPanel.activeSelf;
        bool defeatActive = defeatPanel != null && defeatPanel.activeSelf;
        bool panelActive = victoryActive || defeatActive;
        
        // Hide GiveUp button when any panel is active
        if (panelActive && giveUpButton.activeSelf)
        {
            giveUpButton.SetActive(false);
        }
        // Show GiveUp button when no panels are active
        else if (!panelActive && !giveUpButton.activeSelf)
        {
            giveUpButton.SetActive(true);
        }
    }
}