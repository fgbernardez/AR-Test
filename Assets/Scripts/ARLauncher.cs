using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management; // The "Engine Room"
using System.Collections;

public class ARLauncher : MonoBehaviour
{
    public void StartAR()
    {
        StartCoroutine(StartXREngine());
    }

    IEnumerator StartXREngine()
    {
        Debug.Log("🔧 Attempting to hot-wire XR Engine...");

        // 1. Check if an engine is already running (Zombie state) and kill it
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            Debug.Log("⚠️ Found Zombie Engine. Stopping it...");
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            yield return null; // Wait a frame
        }

        // 2. Start the Engine fresh
        Debug.Log("🚀 Starting XR Engine...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("❌ Failed to start XR Engine. Check Project Settings!");
            yield break;
        }

        Debug.Log("✅ Engine Running. Starting Subsystems...");
        XRGeneralSettings.Instance.Manager.StartSubsystems();

        // 3. NOW load the scene
        Debug.Log("📂 Loading AR Scene...");
        SceneManager.LoadScene("AR_Environment");
    }
}