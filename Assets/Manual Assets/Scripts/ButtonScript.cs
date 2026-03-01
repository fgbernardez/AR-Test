using UnityEngine;
using UnityEngine.SceneManagement; 

public class ButtonScript : MonoBehaviour
{
    [Header("Textbook Pages (Only needed in Textbook Scene)")]
    public GameObject pageCube;
    public GameObject pageSphere;

    // --- SHARED MEMORY ---
    public static string selectedShape = "Cube"; 

    // --- MAIN MENU FUNCTIONS ---
    public void StartGame()
    {
        // Use this button on the Main Menu to go to the Textbook
        SceneManager.LoadScene("AR_Learning");
    }

    public void QuitApp()
    {
        Application.Quit();
        Debug.Log("App Quitting...");
    }

    // --- TEXTBOOK FUNCTIONS ---
    void Start()
    {
        // This runs automatically every time the scene loads.
        // It checks: "Do I have pages assigned?" If yes, reset to the Cube.
        if (pageCube != null && pageSphere != null)
        {
            GoToCube(); // Force the app to start on the Cube page
        }
    }
    
    public void GoToSphere()
    {
        // We add this check so it doesn't crash if we use it in the wrong scene
        if (pageCube != null) pageCube.SetActive(false);
        if (pageSphere != null) pageSphere.SetActive(true);
        
        selectedShape = "Sphere";
    }

    public void GoToCube()
    {
        if (pageSphere != null) pageSphere.SetActive(false);
        if (pageCube != null) pageCube.SetActive(true);
        
        selectedShape = "Cube";
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // --- AR FUNCTIONS ---
    public void LaunchAR()
    {
        SceneManager.LoadScene("AR_Environment"); 
    }

    public void LaunchExercise()
    {
        SceneManager.LoadScene("AR_Exercises"); 
    }
}