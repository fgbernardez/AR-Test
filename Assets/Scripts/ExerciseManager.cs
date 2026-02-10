using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management; // Essential for the Engine
using TMPro;
using System.Collections;

public class ExerciseManager : MonoBehaviour
{
    // --- 1. THE MISSING VARIABLE ---
    public static bool hasVisualized = false;
    // --- DATA PASSING ---
    public static bool isExerciseMode = false; 
    public static float targetVolume = 0f;     
    public static float fixedLength = 0f;      
    public static float fixedWidth = 0f;
    public static float fixedHeight = 0f;

    [Header("UI References")]
    public TMP_InputField answerInput; 
    public Button submitButton;

    void Start()
    {
        // 2. CHECK: Did we just come back from AR?
        if (hasVisualized)
        {
            // Yes! Unlock the answer box.
            if (answerInput != null) answerInput.interactable = true;
            if (submitButton != null) submitButton.interactable = true;
        }
        else
        {
            // No, we just started. Lock it.
            if (answerInput != null) 
            {
                answerInput.interactable = false;
                answerInput.text = "Visualize first to unlock";
            }
            if (submitButton != null) submitButton.interactable = false;
        }
    }

    // --- BUTTON FUNCTIONS ---
    public void LoadProblem_1()
    {
        // 1. Set the Data
        isExerciseMode = true;
        hasVisualized = false;
        ButtonScript.selectedShape = "Cube"; // Force it to be a Cube

        // Problem: A tank 2m x 2m. Find Height for Volume 12.
        // Since our scale is small (0.2 = 1m), we use 0.4 for 2m visual.
        fixedLength = 0.4f; 
        fixedWidth = 0.4f;
        fixedHeight = 0f;   // 0 = User must solve this one!
        
        targetVolume = 12f; 

        // 2. Start the Engine safely
        StartCoroutine(LaunchARSafe());
    }

    // --- THE "ZOMBIE KILLER" LAUNCHER ---
    IEnumerator LaunchARSafe()
    {
        Debug.Log("🔧 Preparing AR Exercise...");

        // STEP 1: SAFETY CHECK
        // If an engine is already running (even a broken one), kill it.
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            Debug.Log("⚠️ Clearing old XR Engine...");
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            yield return null; // Wait one frame to let it die
        }

        // STEP 2: FRESH START
        Debug.Log("🚀 Starting XR Engine...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("❌ Critical Error: XR Engine failed to start.");
            yield break;
        }

        Debug.Log("✅ Engine Active. Loading Scene...");
        XRGeneralSettings.Instance.Manager.StartSubsystems();

        // STEP 3: GO
        SceneManager.LoadScene("AR_Environment");
    }

    // --- CHECK ANSWER LOGIC ---
    public void SubmitAnswer()
    {
        if (answerInput == null) return;

        // 1. Get the text the user typed
        string userAnswer = answerInput.text;
        
        // 2. Define the correct answer (For Problem 1, Height should be 3)
        // Ideally, you'd store this in a "ProblemData" class, but hardcoding for now is fine.
        string correctAnswer = "3";

        // 3. Check it
        if (userAnswer.Trim() == correctAnswer)
        {
            Debug.Log("CORRECT!");
            // Optional: Change the input field color to Green
            answerInput.image.color = Color.green;
        }
        else
        {
            Debug.Log("WRONG!");
            // Optional: Change the input field color to Red
            answerInput.image.color = Color.red;
            answerInput.text = ""; // Clear it so they can try again
        }
    }

    // --- EXIT LOGIC ---
    public void ExitToMainMenu()
    {
        // 1. Reset all flags so the next session starts fresh
        isExerciseMode = false;
        hasVisualized = false;
        
        // 2. Clear locked values (Optional, but good safety)
        fixedLength = 0f;
        fixedWidth = 0f;
        fixedHeight = 0f;

        // 3. Load the Main Menu (or whatever your starting scene is named)
        SceneManager.LoadScene("MainMenu"); 
    }
    
}