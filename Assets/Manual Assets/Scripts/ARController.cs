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
        // 1. IF THE OBJECT IS PLACED...
        if (isPlaced && spawnedObject != null)
        {
            // A. First, run the animation (which destroys and recreates labels)
            if (isShowcasing)
            {
                AnimateShowcase();
            }

            // B. THEN, tell the surviving (or brand new) labels to face the camera
            BillboardLabels();

            // C. Stop running code here so we don't accidentally trigger the placement logic below
            return; 
        }

        // 2. IF THE OBJECT IS NOT PLACED YET (Drag and Drop Logic)
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

    // --- NEW DEDICATED CAMERA-FACING FUNCTION ---
    void BillboardLabels()
    {
        if (spawnedObject == null) return;

        Vector3 dirToCamera = Camera.main.transform.position - spawnedObject.transform.position;
        dirToCamera.y = 0; 
        Quaternion textRotation = Quaternion.LookRotation(dirToCamera);
        textRotation *= Quaternion.Euler(0, 180, 0); 

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
    }

    // --- ANIMATION LOGIC (Cleaned up!) ---
    void AnimateShowcase()
    {
        if (spawnedObject == null) return;

        showcaseTimer += Time.deltaTime;

        float valX = 0.5f + Mathf.Sin(showcaseTimer * 1.5f) * 0.2f;
        float valY = 0.5f + Mathf.Cos(showcaseTimer * 1.2f) * 0.2f; 
        float valZ = 0.5f + Mathf.Sin(showcaseTimer * 0.8f) * 0.2f;

        ApplyMathAndScale(valX, valY, valZ, true);

        if (sliderX != null) sliderX.SetValueWithoutNotify(valX);
        if (sliderY != null) sliderY.SetValueWithoutNotify(valY);
        if (sliderZ != null) sliderZ.SetValueWithoutNotify(valZ);
    }

    // --- UPDATED VOLUME LABEL FUNCTION (2.5x Bigger) ---
    void CreateVolumeLabel(string text, float scaleRef, Vector3 localPos)
    {
        if (volumeLabelPrefab == null) return;
        volumeLabelObj = Instantiate(volumeLabelPrefab, spawnedObject.transform);
        
        volumeLabelObj.transform.localPosition = localPos; 
        
        volumeLabelObj.GetComponent<TextMeshPro>().text = text;
        if (scaleRef < 0.1f) scaleRef = 0.1f;
        
        // FIX: Increased the base multiplier from 0.05f to 0.125f (2.5x larger)
        volumeLabelObj.transform.localScale = Vector3.one * (1f / scaleRef) * 0.05f;
    }

    void SpawnGhost(Vector2 touchPos)
    {
        Pose hitPose = GetPlanePosition(touchPos);
        if (hitPose == Pose.identity) return; 

        GameObject prefabToUse = cubePrefab; 
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

    void MoveGhost(Vector2 touchPos)
    {
        Pose hitPose = GetPlanePosition(touchPos);
        if (hitPose != Pose.identity)
        {
            spawnedObject.transform.position = Vector3.Lerp(spawnedObject.transform.position, hitPose.position, 0.2f);
        }
    }

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
            isShowcasing = true;
            showcaseTimer = 0f;
        }
    }

    void StopShowcase()
    {
        if (isShowcasing) isShowcasing = false;
    }

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

        Vector3 volumeLabelPos = new Vector3(0, 0.5f + 0.2f, 0); 

        if (selectedShape == "Cube")
        {
            spawnedObject.transform.localScale = Vector3.one * x;
            volume = Mathf.Pow(x, 3);
            if (mathText != null) mathText.text = $"{prefix}Side: {x:F2}m\nVolume: {volume:F2} m³";
            
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"s = {x:F2}m");
            CreateVolumeLabel($"Vol: {volume:F2}m³", x, volumeLabelPos);
        }
        else if (selectedShape == "Sphere")
        {
            spawnedObject.transform.localScale = Vector3.one * x;
            float r = x / 2f;
            volume = (4f / 3f) * Mathf.PI * Mathf.Pow(r, 3);
            if (mathText != null) mathText.text = $"{prefix}Radius: {r:F2}m\nVolume: {volume:F2} m³";
            
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), Vector3.zero, $"r = {r:F2}m");
            CreateVolumeLabel($"Vol: {volume:F2}m³", x, volumeLabelPos);
        }
        else if (selectedShape == "Cylinder")
        {
            // FIX: Unity cylinders are naturally 2 units tall! So we scale Y by y/2f to make it accurate.
            spawnedObject.transform.localScale = new Vector3(x, y / 2f, x);
            float r = x / 2f;
            volume = Mathf.PI * Mathf.Pow(r, 2) * y;
            if (mathText != null) mathText.text = $"{prefix}Radius: {r:F2}m | Height: {y:F2}m\nVolume: {volume:F2} m³";
            
            // FIX: Because of the scale change, the local top is at Y=1, not Y=0.5. 
            // Offset is pushed UP so it floats on top of the cylinder.
            CreateDimension(new Vector3(0, 1f, 0), new Vector3(0.5f, 1f, 0), new Vector3(0, 0.1f, 0), $"r = {r:F2}m");
            
            // Height: Outside left edge
            CreateDimension(new Vector3(-0.5f, -1f, 0), new Vector3(-0.5f, 1f, 0), new Vector3(-0.1f, 0, 0), $"h = {y:F2}m");
            CreateVolumeLabel($"Vol: {volume:F2}m³", (x+y)/2f, volumeLabelPos);
        }
        else if (selectedShape == "Cone")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            float r = x / 2f;
            volume = (1f / 3f) * Mathf.PI * Mathf.Pow(r, 2) * y;
            if (mathText != null) mathText.text = $"{prefix}Radius: {r:F2}m | Height: {y:F2}m\nVolume: {volume:F2} m³";
            
            // RADIUS (DRAFTING STYLE): Starts exactly at the tip, draws out to the right boundary.
            // Lifted just a tiny bit (0.05f) so it doesn't clip through the physical tip of the cone.
            CreateDimension(new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0, 0.05f, 0), $"r = {r:F2}m");
            
            // HEIGHT: Absolute center of the base straight up to the exact tip.
            // Pushed to the right (0.6f) to float outside the cone.
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {y:F2}m");
            
            CreateVolumeLabel($"Vol: {volume:F2}m³", (x+y)/2f, volumeLabelPos);
        }
        else if (selectedShape == "Pyramid")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            volume = (1f / 3f) * Mathf.Pow(x, 2) * y; 
            if (mathText != null) mathText.text = $"{prefix}Base: {x:F2}m | Height: {y:F2}m\nVolume: {volume:F2} m³";
            
            // BASE EDGE: Front-left corner to Front-right corner.
            // Pushed slightly forward (-0.1f) so it rests outside the mesh.
            CreateDimension(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, 0.02f, -0.1f), $"b = {x:F2}m");
            
            // HEIGHT: Absolute center of the base straight up to the exact tip.
            // Pushed far to the right (0.6f) to float outside the pyramid.
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {y:F2}m");
            
            CreateVolumeLabel($"Vol: {volume:F2}m³", (x+y)/2f, volumeLabelPos);
        }
        else // Rectangular Prism
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            volume = x * y * z;
            if (mathText != null) mathText.text = $"{prefix}L:{x:F1} | H:{y:F1} | W:{z:F1}\nVolume: {volume:F2} m³";
            
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"L={x:F1}");
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, -0.05f), $"H={y:F1}");
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"W={z:F1}");
            CreateVolumeLabel($"Vol: {volume:F2}m³", (x+y+z)/3f, volumeLabelPos);
        }
    }

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
        Vector3 defaultLabelPos = new Vector3(0, 0.7f, 0); // Fixed the missing 3rd argument here!

        if (selectedShape == "Sphere")
        {
            spawnedObject.transform.localScale = Vector3.one * 0.6f;
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), Vector3.zero, "r = 0.3m");
            CreateVolumeLabel("Volume ≈ 0.11m³", 0.6f, defaultLabelPos); 
        }
        else
        {
            spawnedObject.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
            Vector3 p = new Vector3(-0.5f, 0, -0.5f);
            CreateDimension(p, new Vector3(0.5f, 0, -0.5f), new Vector3(0, -0.05f, -0.05f), "L = 2m");
            CreateDimension(p, new Vector3(-0.5f, 1, -0.5f), new Vector3(-0.05f, 0, -0.05f), "H = ?");
            CreateDimension(p, new Vector3(-0.5f, 0, 0.5f), new Vector3(-0.05f, -0.05f, 0), "W = 2m");
            CreateVolumeLabel("Volume = 12m³", 0.5f, defaultLabelPos); 
        }
    }

    void CreateDimension(Vector3 start, Vector3 end, Vector3 offset, string text)
    {
        if (dimensionPrefab == null) return;
        GameObject dim = Instantiate(dimensionPrefab, spawnedObject.transform);
        dim.GetComponent<DimensionBuilder>().Configure(start, end, offset, text);
        activeDimensions.Add(dim);
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