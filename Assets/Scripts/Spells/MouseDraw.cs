using UnityEngine;

public class MouseDraw : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private int positionCount = 0;
    [SerializeField] private float time = 0;
    [SerializeField] private float minimumPointDistance = 0.15f;
    [SerializeField] private float pointSmoothing = 0.35f;
    [SerializeField] private float minimumGuideLockDistance = 0.5f;
    [SerializeField] private float drawPlaneZ = -8f;
    [SerializeField] private float referenceOrthographicSize = 5f;
    [SerializeField] private float zoomedOrthographicSize = 2.4f;
    private bool hasStroke;
    private Vector3 strokeStartWorldPosition;
    private Vector3 lastStrokeEndWorldPosition;
    private float strokeStartTime;
    private float lastStrokeDuration;
    private float baseWidthMultiplier = 1f;
    private bool strokeStartedThisFrame;
    private Vector3 currentStrokeDirection = Vector3.right;

    public bool HasStroke => hasStroke;
    public LineRenderer CurrentLine => lineRenderer;
    public float LastStrokeDuration => lastStrokeDuration;
    public Vector3 LastStrokeEndWorldPosition => lastStrokeEndWorldPosition;
    public Vector3 CurrentStrokeDirection => currentStrokeDirection;
    public bool HasDirectionalStroke =>
        hasStroke &&
        positionCount >= 2 &&
        Vector3.Distance(lastStrokeEndWorldPosition, strokeStartWorldPosition) >= minimumGuideLockDistance;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        baseWidthMultiplier = lineRenderer != null ? lineRenderer.widthMultiplier : 1f;
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

        if (!TouchPointerInput.TryGetPrimaryPointer(
                out Vector2 screenPosition,
                out bool pointerBegan,
                out bool pointerHeld,
                out bool pointerEnded))
        {
            pointerHeld = false;
            pointerEnded = false;
        }

        if (pointerBegan)
        {
            BeginStroke();
        }

        strokeStartedThisFrame = false;

        if (pointerHeld)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 pointerWorldPosition = camera.ScreenToWorldPoint(screenPosition);
            pointerWorldPosition.z = drawPlaneZ;

            // Sample points more densely and interpolate long cursor jumps to avoid a stepped line.
            if (positionCount == 0)
            {
                strokeStartWorldPosition = pointerWorldPosition;
                lastStrokeEndWorldPosition = pointerWorldPosition;
                hasStroke = true;
                strokeStartedThisFrame = true;
                AddPoint(pointerWorldPosition);
                return;
            }

            Vector3 previousPoint = lineRenderer.GetPosition(positionCount - 1);
            float scaledMinimumPointDistance = GetScaledMinimumPointDistance();
            float distanceToPointer = Vector3.Distance(pointerWorldPosition, previousPoint);
            if (distanceToPointer < scaledMinimumPointDistance)
            {
                return;
            }

            int interpolationSteps = Mathf.Max(1, Mathf.CeilToInt(distanceToPointer / scaledMinimumPointDistance));
            for (int i = 1; i <= interpolationSteps; i++)
            {
                float t = (float)i / interpolationSteps;
                Vector3 targetPoint = Vector3.Lerp(previousPoint, pointerWorldPosition, t);
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
            time += Time.unscaledDeltaTime;
        }
        if (pointerEnded)
        {
            if (hasStroke)
            {
                lastStrokeDuration = Time.unscaledTime - strokeStartTime;
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
        strokeStartTime = Time.unscaledTime;
        lastStrokeDuration = 0f;
        currentStrokeDirection = Vector3.right;
    }

    void ClearStroke()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = baseWidthMultiplier * GetCameraScale();
        positionCount = 0;
        time = 0f;
        hasStroke = false;
        strokeStartedThisFrame = false;
    }

    void AddPoint(Vector3 point)
    {
        ApplyLineCameraScale();

        positionCount++;
        lineRenderer.positionCount = positionCount;
        lineRenderer.SetPosition(positionCount - 1, point);
        lastStrokeEndWorldPosition = point;

        if (positionCount >= 2)
        {
            Vector3 previousPoint = lineRenderer.GetPosition(positionCount - 2);
            Vector3 strokeDirection = point - previousPoint;
            if (strokeDirection.sqrMagnitude > Mathf.Epsilon)
            {
                currentStrokeDirection = strokeDirection.normalized;
            }
        }
    }

    void ApplyLineCameraScale()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.widthMultiplier = baseWidthMultiplier * GetCameraScale();
    }

    float GetScaledMinimumPointDistance()
    {
        return minimumPointDistance * GetCameraScale();
    }

    float GetCameraScale()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic)
        {
            return 1f;
        }

        float referenceSize = Mathf.Max(0.01f, referenceOrthographicSize);
        float targetSize = Mathf.Max(0.01f, zoomedOrthographicSize);
        float cameraSize = Mathf.Max(targetSize, camera.orthographicSize);
        return Mathf.Max(0.01f, cameraSize / referenceSize);
    }
}
