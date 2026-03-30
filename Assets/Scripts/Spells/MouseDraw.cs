using UnityEngine;

public class MouseDraw : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private int positionCount = 0;
    [SerializeField] private float time = 0;
    [SerializeField] private float minimumPointDistance = 0.15f;
    [SerializeField] private float pointSmoothing = 0.35f;
    [SerializeField] private float minimumDirectionalDistance = 0.75f;
    private bool hasStroke;
    private Vector3 strokeStartWorldPosition;
    private Vector3 lastStrokeEndWorldPosition;
    private float strokeStartTime;
    private float lastStrokeDuration;
    private bool strokeStartedThisFrame;
    private Vector3 currentStrokeDirection = Vector3.right;

    public bool HasStroke => hasStroke;
    public LineRenderer CurrentLine => lineRenderer;
    public float LastStrokeDuration => lastStrokeDuration;
    public Vector3 LastStrokeEndWorldPosition => lastStrokeEndWorldPosition;
    public Vector3 CurrentStrokeDirection => currentStrokeDirection;
    public bool HasDirectionalStroke => hasStroke && positionCount >= 2;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; // Start with no points //deprec - manually set
    }

    void Update()
    {
        if (GameplayPause.IsPaused)
        {
            if (hasStroke || positionCount > 0)
            {
                ClearStroke();
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            BeginStroke();
        }

        strokeStartedThisFrame = false;

        if (Input.GetMouseButton(0)) // While left mouse button is held
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // Set z to 0 for 2D

            // Sample points more densely and interpolate long cursor jumps to avoid a stepped line.
            if (positionCount == 0)
            {
                strokeStartWorldPosition = mousePos;
                lastStrokeEndWorldPosition = mousePos;
                hasStroke = true;
                strokeStartedThisFrame = true;
                AddPoint(mousePos);
                return;
            }

            Vector3 previousPoint = lineRenderer.GetPosition(positionCount - 1);
            float distanceToMouse = Vector3.Distance(mousePos, previousPoint);
            if (distanceToMouse < minimumPointDistance)
            {
                return;
            }

            int interpolationSteps = Mathf.Max(1, Mathf.CeilToInt(distanceToMouse / minimumPointDistance));
            for (int i = 1; i <= interpolationSteps; i++)
            {
                float t = (float)i / interpolationSteps;
                Vector3 targetPoint = Vector3.Lerp(previousPoint, mousePos, t);
                Vector3 smoothedPoint = Vector3.Lerp(previousPoint, targetPoint, 1f - pointSmoothing);
                AddPoint(smoothedPoint);
                previousPoint = smoothedPoint;
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
            if (hasStroke)
            {
                lastStrokeDuration = Time.time - strokeStartTime;
            }

            time = 0;
        }
    }

    public bool TryGetStrokeStart(out Vector3 worldPosition)
    {
        worldPosition = strokeStartWorldPosition;
        return hasStroke;
    }

    public bool TryConsumeStrokeStart(out Vector3 worldPosition)
    {
        worldPosition = strokeStartWorldPosition;
        if (!strokeStartedThisFrame)
        {
            return false;
        }

        strokeStartedThisFrame = false;
        return true;
    }

    void BeginStroke()
    {
        ClearStroke();
        strokeStartTime = Time.time;
        lastStrokeDuration = 0f;
        currentStrokeDirection = Vector3.right;
    }

    void ClearStroke()
    {
        lineRenderer.positionCount = 0;
        positionCount = 0;
        time = 0f;
        hasStroke = false;
        strokeStartedThisFrame = false;
    }

    void AddPoint(Vector3 point)
    {
        positionCount++;
        lineRenderer.positionCount = positionCount;
        lineRenderer.SetPosition(positionCount - 1, point);
        lastStrokeEndWorldPosition = point;

        if (positionCount >= 2)
        {
            Vector3 strokeDirection = point - strokeStartWorldPosition;
            if (strokeDirection.sqrMagnitude >= minimumDirectionalDistance * minimumDirectionalDistance)
            {
                currentStrokeDirection = strokeDirection.normalized;
            }
        }
    }
}
