using UnityEngine;

public class PanelMonitor : MonoBehaviour
{
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public GameObject giveUpButton;
    
    void Update()
    {
        // If either panel is active, hide the GiveUp button
        bool panelActive = (victoryPanel != null && victoryPanel.activeSelf) ||
                          (defeatPanel != null && defeatPanel.activeSelf);
        
        // We want the button to be the OPPOSITE of the panel state
        bool shouldButtonBeActive = !panelActive;
        
        if (giveUpButton != null && giveUpButton.activeSelf != shouldButtonBeActive)
        {
            giveUpButton.SetActive(shouldButtonBeActive);
        }
    }
}