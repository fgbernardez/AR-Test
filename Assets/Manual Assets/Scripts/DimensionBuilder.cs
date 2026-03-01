using UnityEngine;
using TMPro;

public class DimensionBuilder : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public TextMeshPro textMesh;

    // We store the original configuration so we can refresh it
    private Vector3 _start, _end, _offset;

    public void Configure(Vector3 start, Vector3 end, Vector3 offsetDir, string textValue)
    {
        _start = start;
        _end = end;
        _offset = offsetDir;

        UpdateText(textValue);
    }

    // Helper to just change the text number (e.g. "2.0m" -> "2.5m")
    public void UpdateText(string textValue)
    {
        if (textMesh != null) textMesh.text = textValue;
        RefreshVisuals(); // Re-calculate positions
    }

    void Update()
    {
        // constantly fix the size if the parent is scaling (Learning Mode)
        if (transform.parent != null && transform.parent.hasChanged)
        {
            RefreshVisuals();
        }
    }

    void RefreshVisuals()
    {
        // 1. Calculate Floating Positions
        Vector3 lineStart = _start + _offset;
        Vector3 lineEnd   = _end + _offset;

        // 2. Draw Line
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] { lineStart, lineEnd });
        }

        // 3. Place Text
        if (textMesh != null)
        {
            textMesh.transform.localPosition = (lineStart + lineEnd) / 2f;
        }

        // 4. FIX SCALING (The Magic Part)
        if (transform.parent != null)
        {
            // Get the average scale of the parent
            float parentAvgScale = (transform.parent.localScale.x + transform.parent.localScale.y) / 2f;
            
            // Protect against zero division
            if (parentAvgScale < 0.001f) parentAvgScale = 0.001f;

            // Keep line thin (0.015 constant)
            if (lineRenderer != null) 
                lineRenderer.widthMultiplier = 0.015f / parentAvgScale;

            // Keep text small (0.05 constant)
            if (textMesh != null)
                textMesh.transform.localScale = Vector3.one * (1f / parentAvgScale) * 0.05f;
        }
    }
}