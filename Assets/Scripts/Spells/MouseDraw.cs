using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseDraw : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private int positionCount = 0;
    [SerializeField] private float time = 0;
    private bool hasStroke;
    private Vector3 strokeStartWorldPosition;

    public bool HasStroke => hasStroke;
    public LineRenderer CurrentLine => lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; // Start with no points
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BeginStroke();
        }

        if (Input.GetMouseButton(0)) // While left mouse button is held
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // Set z to 0 for 2D

            // Only add point if it's far enough from the last point to avoid lag
            if (positionCount == 0 || Vector3.Distance(mousePos, lineRenderer.GetPosition(positionCount - 1)) > 1f)
            {
                if (positionCount == 0)
                {
                    strokeStartWorldPosition = mousePos;
                    hasStroke = true;
                }

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
                hasStroke = false;
            }
            time += Time.deltaTime;
        }
        if (Input.GetMouseButtonUp(0)) 
        {
            time = 0;
        }
    }

    public bool TryGetStrokeStart(out Vector3 worldPosition)
    {
        worldPosition = strokeStartWorldPosition;
        return hasStroke;
    }

    void BeginStroke()
    {
        lineRenderer.positionCount = 0;
        positionCount = 0;
        time = 0f;
        hasStroke = false;
    }
}
