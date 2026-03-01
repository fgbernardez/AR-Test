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
    public GameObject rectangularPrismPrefab; 
    public GameObject pyramidPrefab; 
    public GameObject conePrefab; 
    public GameObject cylinderPrefab; 
    public GameObject spherePrefab; 

    [Header("UI Controls")]
    public GameObject controlPanel;    
    public TextMeshProUGUI mathText;   
    public Button resetButton;         
    
    // Sliders
    public Slider sliderX; 
    public Slider sliderY; 
    public Slider sliderZ; 

    [Header("AR Components")]
    public GameObject promptText;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager; 

    [Header("Exercise Elements")]
    public GameObject dimensionPrefab;   
    public GameObject volumeLabelPrefab; 
    
    // INTERNAL STATE
    private List<GameObject> activeDimensions = new List<GameObject>();
    private GameObject volumeLabelObj;
    private GameObject spawnedObject; 
    private string selectedShape;
    private bool isPlaced = false; 

    // ANIMATION STATE
    private bool isShowcasing = false;
    private float showcaseTimer = 0f;

    void Start()
    {
        selectedShape = string.IsNullOrEmpty(ButtonScript.selectedShape) ? "Cube" : ButtonScript.selectedShape; 

        if (resetButton != null) resetButton.onClick.AddListener(ResetScene);

        if (ExerciseManager.isExerciseMode) {
            if (controlPanel != null) controlPanel.SetActive(false);
            if (promptText != null) promptText.GetComponent<TextMeshProUGUI>().text = "Touch & Hold to place";
        } else {
            // Setup slider listeners
            if (sliderX != null) sliderX.onValueChanged.AddListener(delegate { StopShowcase(); UpdateDimensions(); });
            if (sliderY != null) sliderY.onValueChanged.AddListener(delegate { StopShowcase(); UpdateDimensions(); });
            if (sliderZ != null) sliderZ.onValueChanged.AddListener(delegate { StopShowcase(); UpdateDimensions(); });
            
            // DYNAMICALLY HIDE SLIDERS
            if (selectedShape == "Sphere" || selectedShape == "Cube") {
                if (sliderY != null) sliderY.gameObject.SetActive(false);
                if (sliderZ != null) sliderZ.gameObject.SetActive(false);
            }
            else if (selectedShape == "Cylinder" || selectedShape == "Cone" || selectedShape == "Pyramid") {
                if (sliderY != null) sliderY.gameObject.SetActive(true);
                if (sliderZ != null) sliderZ.gameObject.SetActive(false);
            }
            else {
                // Rectangular Prism needs all 3
                if (sliderY != null) sliderY.gameObject.SetActive(true);
                if (sliderZ != null) sliderZ.gameObject.SetActive(true);
            }
            
            if (controlPanel != null) controlPanel.SetActive(false);
        }
    }

    void Update()
    {
        // 1. If we are in "Watch Mode", run the animation
        if (isPlaced && isShowcasing && spawnedObject != null)
        {
            AnimateShowcase();
            return; 
        }

        // 2. If not placed yet, handle Drag & Drop
        if (!isPlaced && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchingUI(touch)) return; 
                SpawnGhost(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved && spawnedObject != null)
            {
                MoveGhost(touch.position);
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && spawnedObject != null)
            {
                FinalizePlacement();
            }
        }
    }

    // --- PHASE 1: SPAWN GHOST ---
    void SpawnGhost(Vector2 touchPos)
    {
        Pose hitPose = GetPlanePosition(touchPos);
        if (hitPose == Pose.identity) return; 

        GameObject prefabToUse = cubePrefab; // Default
        switch (selectedShape)
        {
            case "RectangularPrism": prefabToUse = rectangularPrismPrefab; break;
            case "Pyramid": prefabToUse = pyramidPrefab; break;
            case "Cone": prefabToUse = conePrefab; break;
            case "Cylinder": prefabToUse = cylinderPrefab; break;
            case "Sphere": prefabToUse = spherePrefab; break;
        }

        spawnedObject = Instantiate(prefabToUse, hitPose.position, hitPose.rotation);
        
        Vector3 lookPos = new Vector3(Camera.main.transform.position.x, spawnedObject.transform.position.y, Camera.main.transform.position.z);
        spawnedObject.transform.LookAt(lookPos);
    }

    // --- PHASE 2: MOVE GHOST ---
    void MoveGhost(Vector2 touchPos)
    {
        Pose hitPose = GetPlanePosition(touchPos);
        if (hitPose != Pose.identity)
        {
            spawnedObject.transform.position = Vector3.Lerp(spawnedObject.transform.position, hitPose.position, 0.2f);
        }
    }

    // --- PHASE 3: LOCK IT ---
    void FinalizePlacement()
    {
        isPlaced = true;
        TogglePlaneDetection(false);

        if (promptText != null) promptText.SetActive(false);

        if (ExerciseManager.isExerciseMode)
        {
            SetupExerciseVisuals(); 
        }
        else
        {
            if (controlPanel != null) controlPanel.SetActive(true);
            
            // Start the Auto-Showcase animation immediately
            isShowcasing = true;
            showcaseTimer = 0f;
        }
    }

    // --- ANIMATION LOGIC ---
    void AnimateShowcase()
    {
        if (spawnedObject == null) return;

        showcaseTimer += Time.deltaTime;

        // 1. CALCULATE BILLBOARD ROTATION
        Vector3 dirToCamera = Camera.main.transform.position - spawnedObject.transform.position;
        dirToCamera.y = 0; 
        Quaternion textRotation = Quaternion.LookRotation(dirToCamera);
        textRotation *= Quaternion.Euler(0, 180, 0); // Flip to fix mirroring

        // 2. MORPH MATH
        float valX = 0.5f + Mathf.Sin(showcaseTimer * 1.5f) * 0.2f;
        float valY = 0.5f + Mathf.Cos(showcaseTimer * 1.2f) * 0.2f; 
        float valZ = 0.5f + Mathf.Sin(showcaseTimer * 0.8f) * 0.2f;

        // 3. APPLY CORE MATH ENGINE
        ApplyMathAndScale(valX, valY, valZ, true);

        // 4. ROTATE TEXT ONLY
        if (volumeLabelObj != null) 
            volumeLabelObj.transform.rotation = textRotation;

        foreach (var dim in activeDimensions)
        {
            if (dim != null)
            {
                TextMeshPro label = dim.GetComponentInChildren<TextMeshPro>();
                if (label != null) label.transform.rotation = textRotation;
            }
        }

        // 5. SLIDER SYNC
        if (sliderX != null) sliderX.SetValueWithoutNotify(valX);
        if (sliderY != null) sliderY.SetValueWithoutNotify(valY);
        if (sliderZ != null) sliderZ.SetValueWithoutNotify(valZ);
    }

    void StopShowcase()
    {
        if (isShowcasing)
        {
            isShowcasing = false;
        }
    }

    // --- MANUAL CONTROL ---
    public void UpdateDimensions()
    {
        if (spawnedObject == null || isShowcasing) return;
        ApplyMathAndScale(sliderX.value, sliderY.value, sliderZ.value, false);
    }

    // --- THE CORE MATH ENGINE ---
    void ApplyMathAndScale(float x, float y, float z, bool isWatchMode)
    {
        ClearOldLabels();
        float volume = 0f;
        string prefix = isWatchMode ? "<color=yellow>Watch Mode</color>\n" : "";

        if (selectedShape == "Cube")
        {
            spawnedObject.transform.localScale = Vector3.one * x;
            volume = Mathf.Pow(x, 3);
            if (mathText != null) mathText.text = $"{prefix}Side: {x:F2}m\nVolume: {volume:F2} m³";
            CreateVolumeLabel($"Vol: {volume:F2}m³", x);
        }
        else if (selectedShape == "Sphere")
        {
            spawnedObject.transform.localScale = Vector3.one * x;
            float r = x / 2f;
            volume = (4f / 3f) * Mathf.PI * Mathf.Pow(r, 3);
            if (mathText != null) mathText.text = $"{prefix}Radius: {r:F2}m\nVolume: {volume:F2} m³";
            CreateVolumeLabel($"Vol: {volume:F2}m³", x);
        }
        else if (selectedShape == "Cylinder")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            float r = x / 2f;
            volume = Mathf.PI * Mathf.Pow(r, 2) * y;
            if (mathText != null) mathText.text = $"{prefix}Radius: {r:F2}m | Height: {y:F2}m\nVolume: {volume:F2} m³";
            CreateVolumeLabel($"Vol: {volume:F2}m³", (x+y)/2f);
        }
        else if (selectedShape == "Cone")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            float r = x / 2f;
            volume = (1f / 3f) * Mathf.PI * Mathf.Pow(r, 2) * y;
            if (mathText != null) mathText.text = $"{prefix}Radius: {r:F2}m | Height: {y:F2}m\nVolume: {volume:F2} m³";
            CreateVolumeLabel($"Vol: {volume:F2}m³", (x+y)/2f);
        }
        else if (selectedShape == "Pyramid")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            volume = (1f / 3f) * Mathf.Pow(x, 2) * y; 
            if (mathText != null) mathText.text = $"{prefix}Base: {x:F2}m | Height: {y:F2}m\nVolume: {volume:F2} m³";
            CreateVolumeLabel($"Vol: {volume:F2}m³", (x+y)/2f);
        }
        else // Rectangular Prism
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            volume = x * y * z;
            if (mathText != null) mathText.text = $"{prefix}L:{x:F1} | H:{y:F1} | W:{z:F1}\nVolume: {volume:F2} m³";
            CreateVolumeLabel($"Vol: {volume:F2}m³", (x+y+z)/3f);
        }
    }

    // --- HELPERS ---
    Pose GetPlanePosition(Vector2 touchPos)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon)) return hits[0].pose;
        return Pose.identity; 
    }

    void TogglePlaneDetection(bool status)
    {
        if (planeManager != null)
        {
            planeManager.enabled = status; 
            foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(status);
        }
    }

    public void ResetScene()
    {
        StopShowcase();
        if (spawnedObject != null) { Destroy(spawnedObject); spawnedObject = null; }
        ClearOldLabels();
        isPlaced = false; 
        TogglePlaneDetection(true);
        if (promptText != null) promptText.SetActive(true);
        if (controlPanel != null) controlPanel.SetActive(false);
    }

    bool IsTouchingUI(Touch t)
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(t.fingerId);
    }

    void SetupExerciseVisuals()
    {
        ClearOldLabels();
        if (selectedShape == "Sphere")
        {
            spawnedObject.transform.localScale = Vector3.one * 0.6f;
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), Vector3.zero, "r = 0.3m");
            CreateVolumeLabel("Volume ≈ 0.11m³", 0.6f);
        }
        else
        {
            spawnedObject.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
            Vector3 p = new Vector3(-0.5f, 0, -0.5f);
            CreateDimension(p, new Vector3(0.5f, 0, -0.5f), new Vector3(0, -0.05f, -0.05f), "L = 2m");
            CreateDimension(p, new Vector3(-0.5f, 1, -0.5f), new Vector3(-0.05f, 0, -0.05f), "H = ?");
            CreateDimension(p, new Vector3(-0.5f, 0, 0.5f), new Vector3(-0.05f, -0.05f, 0), "W = 2m");
            CreateVolumeLabel("Volume = 12m³", 0.5f);
        }
    }

    void CreateDimension(Vector3 start, Vector3 end, Vector3 offset, string text)
    {
        if (dimensionPrefab == null) return;
        GameObject dim = Instantiate(dimensionPrefab, spawnedObject.transform);
        dim.GetComponent<DimensionBuilder>().Configure(start, end, offset, text);
        activeDimensions.Add(dim);
    }

    void CreateVolumeLabel(string text, float scaleRef)
    {
        if (volumeLabelPrefab == null) return;
        volumeLabelObj = Instantiate(volumeLabelPrefab, spawnedObject.transform);
        volumeLabelObj.transform.localPosition = new Vector3(0, 1.5f, 0); 
        volumeLabelObj.GetComponent<TextMeshPro>().text = text;
        if (scaleRef < 0.1f) scaleRef = 0.1f;
        volumeLabelObj.transform.localScale = Vector3.one * (1f / scaleRef) * 0.05f;
    }

    void ClearOldLabels()
    {
        foreach (var d in activeDimensions) Destroy(d);
        activeDimensions.Clear();
        if (volumeLabelObj != null) Destroy(volumeLabelObj);
    }

    public void ExitAR()
    {
        SceneManager.LoadScene(ExerciseManager.isExerciseMode ? "AR_Exercises" : "AR_Learning");
    }
}