using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Panels to Control")]
    public GameObject[] allPanels;

    // 1. NAVIGATION WIRING
    public void ShowPanel(GameObject panelToShow)
    {
        // hide panels
        foreach (GameObject panel in allPanels)
        {
            if (panel != null) panel.SetActive(false);
        }
        
        //  only show whats needed
        if (panelToShow != null) panelToShow.SetActive(true);
    }

    // 2. AR LAUNCH WIRING
    public void ViewInAR(string shapeName)
    {
        // Tell your existing ButtonScript which shape to spawn
        ButtonScript.selectedShape = shapeName; 
        
        // Load the AR scene (Change "AR_Learning" if your scene name is different!)
        SceneManager.LoadScene("AR_Learning"); 
    }
}