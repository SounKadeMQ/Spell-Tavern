using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseDraw : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private int positionCount = 0;
    [SerializeField] private float time = 0;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; // Start with no points
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) // While left mouse button is held
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // Set z to 0 for 2D

            // Only add point if it's far enough from the last point to avoid lag
            if (positionCount == 0 || Vector3.Distance(mousePos, lineRenderer.GetPosition(positionCount - 1)) > 1f)
            {
                positionCount++;
                lineRenderer.positionCount = positionCount;
                lineRenderer.SetPosition(positionCount - 1, mousePos);
            }
        }
        else
        {
            if (time >= 1)
            {
                lineRenderer.positionCount = 0;
                time = 0;
                positionCount = 0;
            }
            time += Time.deltaTime;
        }
        if (Input.GetMouseButtonUp(0)) 
        {
            time = 0;
        }
    }
}
