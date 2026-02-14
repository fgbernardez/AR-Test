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
            
            if (selectedShape == "Sphere") {
                if (sliderY != null) sliderY.gameObject.SetActive(false);
                if (sliderZ != null) sliderZ.gameObject.SetActive(false);
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

        GameObject prefabToUse = (selectedShape == "Sphere") ? spherePrefab : cubePrefab;
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
    float l = 0.5f + Mathf.Sin(showcaseTimer * 1.5f) * 0.2f;
    float h = 0.5f + Mathf.Cos(showcaseTimer * 1.2f) * 0.2f; 
    float w = 0.5f + Mathf.Sin(showcaseTimer * 0.8f) * 0.2f;

    // 3. APPLY SCALING (The Cube stays at its original rotation)
    if (selectedShape == "Sphere")
    {
        float r = l;
        spawnedObject.transform.localScale = Vector3.one * r;
        float radius = r / 2f;
        float vol = (4f / 3f) * Mathf.PI * Mathf.Pow(radius, 3);
        
        if (mathText != null) mathText.text = $"<color=yellow>Watch Mode</color>\nRadius: {radius:F2}m\nVolume: {vol:F2} m³";
        
        ClearOldLabels();
        CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), Vector3.zero, $"r = {radius:F2}m");
        CreateVolumeLabel($"Vol: {vol:F2}m³", r);
    }
    else
    {
        spawnedObject.transform.localScale = new Vector3(l, h, w);
        float vol = l * h * w;

        if (mathText != null) mathText.text = $"<color=yellow>Watch Mode</color>\nL:{l:F1} | H:{h:F1} | W:{w:F1}\nVolume: {vol:F2} m³";
        
        ClearOldLabels();
        Vector3 p = new Vector3(-0.5f, 0, -0.5f);
        CreateDimension(p, new Vector3(0.5f, 0, -0.5f), new Vector3(0, -0.05f, -0.05f), $"L={l:F1}");
        CreateDimension(p, new Vector3(-0.5f, 1, -0.5f), new Vector3(-0.05f, 0, -0.05f), $"H={h:F1}");
        CreateDimension(p, new Vector3(-0.5f, 0, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"W={w:F1}");
        CreateVolumeLabel($"Vol: {vol:F2}", (l+h+w)/3f);
    }

    // 4. ROTATE TEXT ONLY
    // A. Rotate the main Volume label
    if (volumeLabelObj != null) 
        volumeLabelObj.transform.rotation = textRotation;

    // B. Rotate the text INSIDE the dimension lines
    foreach (var dim in activeDimensions)
    {
        if (dim != null)
        {
            // We find the TextMeshPro component in the children of the line
            // and only rotate THAT transform. The line remains fixed.
            TextMeshPro label = dim.GetComponentInChildren<TextMeshPro>();
            if (label != null)
            {
                label.transform.rotation = textRotation;
            }
        }
    }

    // 5. SLIDER SYNC
    if (sliderX != null) sliderX.SetValueWithoutNotify(l);
    if (sliderY != null) sliderY.SetValueWithoutNotify(h);
    if (sliderZ != null) sliderZ.SetValueWithoutNotify(w);
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
        ClearOldLabels();

        if (selectedShape == "Sphere")
        {
            float scale = sliderX.value; 
            spawnedObject.transform.localScale = Vector3.one * scale;
            float radius = scale / 2f;
            float volume = (4f / 3f) * Mathf.PI * Mathf.Pow(radius, 3);

            if (mathText != null) mathText.text = $"Radius: {radius:F2}m\nVolume: {volume:F2} m³";
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), Vector3.zero, $"r = {radius:F2}m");
            CreateVolumeLabel($"Vol: {volume:F2}m³", scale);
        }
        else
        {
            float l = sliderX.value; float h = sliderY.value; float w = sliderZ.value;
            spawnedObject.transform.localScale = new Vector3(l, h, w);
            float volume = l * h * w;

            if (mathText != null) mathText.text = $"L:{l:F1} | H:{h:F1} | W:{w:F1}\nVolume: {volume:F2} m³";
            
            Vector3 p = new Vector3(-0.5f, 0, -0.5f);
            CreateDimension(p, new Vector3(0.5f, 0, -0.5f), new Vector3(0, -0.05f, -0.05f), $"L={l:F1}");
            CreateDimension(p, new Vector3(-0.5f, 1, -0.5f), new Vector3(-0.05f, 0, -0.05f), $"H={h:F1}");
            CreateDimension(p, new Vector3(-0.5f, 0, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"W={w:F1}");
            CreateVolumeLabel($"Vol: {volume:F2}", (l+h+w)/3f);
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