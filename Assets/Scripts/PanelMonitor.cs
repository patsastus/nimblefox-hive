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
        
        if (giveUpButton != null && giveUpButton.activeSelf != panelActive)
        {
            giveUpButton.SetActive(!panelActive);
        }
    }
}