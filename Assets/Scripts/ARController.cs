using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement; 
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic;

public class ARController : MonoBehaviour
{
    [Header("Spawnable Objects")]
    public GameObject cubePrefab;   
    public GameObject spherePrefab; 

    [Header("Exercise Elements")]
    public GameObject labelPrefab; // <-- Drag your new 3D Text Prefab here!

    [Header("UI Controls")]
    public GameObject controlPanel;    // The panel holding sliders
    public TextMeshProUGUI mathText;   // The text showing Volume
    public Slider sliderX; // Length (or Radius)
    public Slider sliderY; // Height
    public Slider sliderZ; // Width

    [Header("AR Components")]
    public GameObject promptText;
    public ARRaycastManager raycastManager;

    [Header("Exercise Elements")]
    public GameObject dimensionPrefab; // <-- Drag your new "DimensionLine" Prefab here!
    public GameObject volumeLabelPrefab; // <-- Keep your OLD text prefab for the Volume label (optional)
    
    // ... inside ARController class ...
    
    // STORAGE FOR LEARNING LABELS
    private GameObject learnLabelL, learnLabelW, learnLabelH, learnLabelVol;

    private GameObject objectToSpawn; 
    private GameObject spawnedObject; 
    private string selectedShape;

    void Start()
    {
        // 1. Determine Shape
        selectedShape = string.IsNullOrEmpty(ButtonScript.selectedShape) ? "Cube" : ButtonScript.selectedShape; 
        objectToSpawn = (selectedShape == "Sphere") ? spherePrefab : cubePrefab;

        // 2. CHECK MODE: Are we Learning or Exercising?
        if (ExerciseManager.isExerciseMode)
        {
            // --- EXERCISE MODE ---
            // Hide the sliders completely. We don't need them.
            if (controlPanel != null) controlPanel.SetActive(false);
            
            // Set prompt text to something helpful
            if (promptText != null) 
                promptText.GetComponent<TextMeshProUGUI>().text = "Tap to place the problem";
        }
        else
        {
            // --- LEARNING MODE ---
            // Setup listeners to run "UpdateDimensions" whenever sliders move
            if (sliderX != null) sliderX.onValueChanged.AddListener(delegate { UpdateDimensions(); });
            if (sliderY != null) sliderY.onValueChanged.AddListener(delegate { UpdateDimensions(); });
            if (sliderZ != null) sliderZ.onValueChanged.AddListener(delegate { UpdateDimensions(); });

            // Hide unrelated sliders for Sphere
            if (selectedShape == "Sphere")
            {
                if (sliderY != null) sliderY.gameObject.SetActive(false);
                if (sliderZ != null) sliderZ.gameObject.SetActive(false);
            }
            
            // Hide control panel until object is spawned
            if (controlPanel != null) controlPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (spawnedObject != null) return; // Stop scanning if object exists

        // --- UNIFIED INPUT CHECK (Mouse or Touch) ---
        if (Input.touchCount > 0 || Input.GetMouseButtonDown(0))
        {
            Vector2 touchPos = (Input.touchCount > 0) ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

            // 1. Raycast against Manual Floor (PC Test)
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(touchPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    SpawnObject(hit.point, Quaternion.identity);
                    return;
                }
            }

            // 2. Raycast against AR Planes (Mobile)
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
            {
                SpawnObject(hits[0].pose.position, hits[0].pose.rotation);
            }
        }
    }

    void SpawnObject(Vector3 position, Quaternion rotation)
    {
        spawnedObject = Instantiate(objectToSpawn, position, rotation);
        
        // Make object face camera
        Vector3 lookPos = new Vector3(Camera.main.transform.position.x, spawnedObject.transform.position.y, Camera.main.transform.position.z);
        spawnedObject.transform.LookAt(lookPos);

        // Hide "Scan Floor" text
        if (promptText != null) promptText.SetActive(false);

        // --- BRANCHING LOGIC ---
        if (ExerciseManager.isExerciseMode)
        {
            SetupExerciseVisuals(); // Run the Blueprint logic
        }
        else
        {
            // Run the Slider logic
            if (controlPanel != null) controlPanel.SetActive(true);
            UpdateDimensions();
        }
    }

    // ---------------------------------------------------------
    // LOGIC A: EXERCISE MODE (Static Blueprint)
    // ---------------------------------------------------------
   void SetupExerciseVisuals()
    {
        // 1. Set Cube Size (2m x 2m x 3m)
        float scaleL = 0.4f; 
        float scaleW = 0.4f;
        float scaleH = 0.6f; 
        spawnedObject.transform.localScale = new Vector3(scaleL, scaleH, scaleW);

        // Define Corners
        Vector3 botLeftFront  = new Vector3(-0.5f, 0, -0.5f);
        Vector3 botRightFront = new Vector3( 0.5f, 0, -0.5f);
        Vector3 topLeftFront  = new Vector3(-0.5f, 1, -0.5f);
        Vector3 botLeftBack   = new Vector3(-0.5f, 0,  0.5f);

        // --- DRAW DIMENSIONS ---
        // I reduced the offsets (0.05f) so the lines hug the cube tighter.

        // L = 2m
        CreateDimension(botLeftFront, botRightFront, new Vector3(0, -0.05f, -0.05f), "L = 2m");

        // H = ?
        CreateDimension(botLeftFront, topLeftFront, new Vector3(-0.05f, 0, -0.05f), "H = ?");

        // W = 2m
        CreateDimension(botLeftFront, botLeftBack, new Vector3(-0.05f, -0.05f, 0), "W = 2m");

        // --- FIX THE VOLUME LABEL ---
        if (volumeLabelPrefab != null)
        {
            GameObject vLabel = Instantiate(volumeLabelPrefab, spawnedObject.transform);
            
            // FIX 1: Move it HIGHER (Y=1.5 is safer than 1.3)
            vLabel.transform.localPosition = new Vector3(0, 1.5f, 0); 
            vLabel.GetComponent<TextMeshPro>().text = "Volume = 12m³";
            
            // FIX 2: FORCE IT TINY
            // We changed 0.2f -> 0.03f. This makes it roughly 7x smaller.
            float parentScale = spawnedObject.transform.localScale.y; 
            vLabel.transform.localScale = Vector3.one * (1f / parentScale) * 0.03f; 
        }
    }

    void CreateDimension(Vector3 start, Vector3 end, Vector3 offset, string text)
    {
        if (dimensionPrefab == null) return;

        // Instantiate inside the cube so it scales/moves with it
        GameObject dimObj = Instantiate(dimensionPrefab, spawnedObject.transform);
        
        // Configure the lines
        DimensionBuilder builder = dimObj.GetComponent<DimensionBuilder>();
        if (builder != null)
        {
            builder.Configure(start, end, offset, text);
        }
    }

    // ---------------------------------------------------------
    // LOGIC B: LEARNING MODE (Sliders & Math)
    // ---------------------------------------------------------
    public void UpdateDimensions()
    {
        if (spawnedObject == null) return;

        float volume = 0f;

        // --- CUBE LOGIC ---
        if (selectedShape == "Cube")
        {
            float length = sliderX.value; // Scale X
            float height = sliderY.value; // Scale Y
            float width  = sliderZ.value; // Scale Z

            // 1. Apply Scale
            spawnedObject.transform.localScale = new Vector3(length, height, width);
            volume = length * width * height;

            // 2. Update UI Text (The 2D Debug Panel)
            if (mathText != null)
            {
                mathText.text = $"L: {length:F2}m  |  H: {height:F2}m  |  W: {width:F2}m\n<b>Volume: {volume:F2} m³</b>";
            }

            // 3. UPDATE 3D VISUALS (The New Feature)
            // Only runs if we are NOT in Exercise Mode (since Exercise has its own setup)
            if (!ExerciseManager.isExerciseMode)
            {
                UpdateLearningVisuals(length, height, width, volume);
            }
        }
        
        // --- SPHERE LOGIC ---
        else if (selectedShape == "Sphere")
        {
            // ... (Keep your sphere logic here) ...
        }
    }
    
    void UpdateLearningVisuals(float l, float h, float w, float vol)
    {
        // DEFINE CORNERS (Same as Exercise Mode)
        Vector3 botLeftFront  = new Vector3(-0.5f, 0, -0.5f);
        Vector3 botRightFront = new Vector3( 0.5f, 0, -0.5f);
        Vector3 topLeftFront  = new Vector3(-0.5f, 1, -0.5f);
        Vector3 botLeftBack   = new Vector3(-0.5f, 0,  0.5f);

        // --- 1. LENGTH LABEL ---
        if (learnLabelL == null) // Does it exist?
        {
            // No? Create it!
            learnLabelL = Instantiate(dimensionPrefab, spawnedObject.transform);
            learnLabelL.GetComponent<DimensionBuilder>().Configure(botLeftFront, botRightFront, new Vector3(0, -0.05f, -0.05f), "");
        }
        // Update the Text
        learnLabelL.GetComponent<DimensionBuilder>().UpdateText($"L = {l:F1}m");

        // --- 2. HEIGHT LABEL ---
        if (learnLabelH == null)
        {
            learnLabelH = Instantiate(dimensionPrefab, spawnedObject.transform);
            learnLabelH.GetComponent<DimensionBuilder>().Configure(botLeftFront, topLeftFront, new Vector3(-0.05f, 0, -0.05f), "");
        }
        learnLabelH.GetComponent<DimensionBuilder>().UpdateText($"H = {h:F1}m");

        // --- 3. WIDTH LABEL ---
        if (learnLabelW == null)
        {
            learnLabelW = Instantiate(dimensionPrefab, spawnedObject.transform);
            learnLabelW.GetComponent<DimensionBuilder>().Configure(botLeftFront, botLeftBack, new Vector3(-0.05f, -0.05f, 0), "");
        }
        learnLabelW.GetComponent<DimensionBuilder>().UpdateText($"W = {w:F1}m");

        // ... inside UpdateLearningVisuals ...

    // --- 4. VOLUME LABEL ---
    if (volumeLabelPrefab != null)
    {
        // A. Create if needed
        if (learnLabelVol == null)
        {
            learnLabelVol = Instantiate(volumeLabelPrefab, spawnedObject.transform);
            learnLabelVol.transform.localPosition = new Vector3(0, 1.5f, 0);
            
            // Ensure it's centered
            TextMeshPro tmp = learnLabelVol.GetComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 6; // Standardize font size
        }
        
        // B. Update Text
        learnLabelVol.GetComponent<TextMeshPro>().text = $"Volume\n{vol:F2} m³";
        
        // C. FIX SCALING (The "Harmonizer")
        // We now use the exact same math as DimensionBuilder so they match perfectly.
        
        float parentX = spawnedObject.transform.localScale.x;
        float parentY = spawnedObject.transform.localScale.y;
        float avgScale = (parentX + parentY) / 2f;
        
        // Safety check to prevent divide by zero
        if (avgScale < 0.001f) avgScale = 0.001f;

        // Match the multiplier from DimensionBuilder (0.05f). 
        // We use 0.06f to make Volume slightly (20%) bigger than dimensions, for emphasis.
        learnLabelVol.transform.localScale = Vector3.one * (1f / avgScale) * 0.06f;
    }
    }
    
    // ---------------------------------------------------------
    // EXIT LOGIC
    // ---------------------------------------------------------
    public void ExitAR()
    {
        if (ExerciseManager.isExerciseMode)
        {
            ExerciseManager.hasVisualized = true; 
            SceneManager.LoadScene("AR_Exercises");
        }
        else
        {
            SceneManager.LoadScene("AR_Learning");
        }
    }
}