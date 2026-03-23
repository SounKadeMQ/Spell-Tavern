using UnityEngine;

public class AccuracyCheck : MonoBehaviour
{
    [SerializeField] GameObject line;
    private LineRenderer tracerLine;
    private LineRenderer drawLine;
    [SerializeField] private float accuracy = 0;
    [SerializeField] private int minimumSampleCount = 3;
    [SerializeField] private float minimumStrokeLength = 2f;
    [SerializeField] private float minimumLengthCoverage = 0.8f;
    [SerializeField] private float endpointTolerance = 1.5f;
    [SerializeField] private float pathTolerance = 1.25f;
    [SerializeField] private float minimumPathMatchRatio = 0.85f;
    private bool hasFreshAccuracy;

    public float Accuracy => accuracy;

    void Start()
    {
        tracerLine = GetComponent<LineRenderer>();
    }


    void Update()
    {
        if (Input.GetMouseButtonUp(0)) 
        {
            accuracy = 0;
            hasFreshAccuracy = false;

            if (line == null || tracerLine == null)
            {
                return;
            }

            drawLine = line.GetComponent<LineRenderer>();
            if (drawLine == null)
            {
                return;
            }

            if (drawLine.positionCount < minimumSampleCount)
            {
                return;
            }

            int posCount = Mathf.Min(tracerLine.positionCount,drawLine.positionCount);
            if (posCount <= 0)
            {
                return;
            }

            float drawLength = GetLineLength(drawLine, drawLine.positionCount);
            if (drawLength < minimumStrokeLength)
            {
                return;
            }

            float tracerLength = GetLineLength(tracerLine, tracerLine.positionCount);
            if (tracerLength <= 0f)
            {
                return;
            }

            if (drawLength / tracerLength < minimumLengthCoverage)
            {
                return;
            }

            if (!HasValidEndpoints())
            {
                return;
            }

            if (!HasValidPathMatch())
            {
                return;
            }

            for (int i = 0; i < posCount;i++)
            {
                float x = Mathf.Abs(tracerLine.GetPosition(i).x -drawLine.GetPosition(i).x) + Mathf.Abs(tracerLine.GetPosition(i).y -drawLine.GetPosition(i).y);
                float xInverse = Mathf.Abs(tracerLine.GetPosition(i).x -drawLine.GetPosition(drawLine.positionCount-i-1).x) + Mathf.Abs(tracerLine.GetPosition(i).y -drawLine.GetPosition(drawLine.positionCount-i-1).y);
                if(x >= xInverse)
                {
                    accuracy += xInverse;
                }
                else 
                {
                    accuracy += x;
                }

            }
            accuracy = accuracy/posCount;
            hasFreshAccuracy = true;
        }
    }

    public bool TryConsumeAccuracy(out float result)
    {
        if (!hasFreshAccuracy)
        {
            result = 0f;
            return false;
        }

        hasFreshAccuracy = false;
        result = accuracy;
        return true;
    }

    bool HasValidEndpoints()
    {
        Vector3 drawStart = drawLine.GetPosition(0);
        Vector3 drawEnd = drawLine.GetPosition(drawLine.positionCount - 1);
        Vector3 tracerStart = tracerLine.GetPosition(0);
        Vector3 tracerEnd = tracerLine.GetPosition(tracerLine.positionCount - 1);

        bool forwardMatch =
            Vector3.Distance(drawStart, tracerStart) <= endpointTolerance &&
            Vector3.Distance(drawEnd, tracerEnd) <= endpointTolerance;

        bool reverseMatch =
            Vector3.Distance(drawStart, tracerEnd) <= endpointTolerance &&
            Vector3.Distance(drawEnd, tracerStart) <= endpointTolerance;

        return forwardMatch || reverseMatch;
    }

    bool HasValidPathMatch()
    {
        int matchingPoints = 0;

        for (int i = 0; i < drawLine.positionCount; i++)
        {
            Vector3 point = drawLine.GetPosition(i);
            if (GetDistanceToLine(point, tracerLine) <= pathTolerance)
            {
                matchingPoints++;
            }
        }

        float matchRatio = (float)matchingPoints / drawLine.positionCount;
        return matchRatio >= minimumPathMatchRatio;
    }

    float GetDistanceToLine(Vector3 point, LineRenderer targetLine)
    {
        if (targetLine.positionCount == 0)
        {
            return float.MaxValue;
        }

        if (targetLine.positionCount == 1)
        {
            return Vector3.Distance(point, targetLine.GetPosition(0));
        }

        float shortestDistance = float.MaxValue;

        for (int i = 1; i < targetLine.positionCount; i++)
        {
            float segmentDistance = DistanceToSegment(
                point,
                targetLine.GetPosition(i - 1),
                targetLine.GetPosition(i));

            if (segmentDistance < shortestDistance)
            {
                shortestDistance = segmentDistance;
            }
        }

        return shortestDistance;
    }

    float DistanceToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
    {
        Vector3 segment = segmentEnd - segmentStart;
        float segmentLengthSquared = segment.sqrMagnitude;

        if (segmentLengthSquared <= Mathf.Epsilon)
        {
            return Vector3.Distance(point, segmentStart);
        }

        float t = Vector3.Dot(point - segmentStart, segment) / segmentLengthSquared;
        t = Mathf.Clamp01(t);

        Vector3 closestPoint = segmentStart + (segment * t);
        return Vector3.Distance(point, closestPoint);
    }

    float GetLineLength(LineRenderer targetLine, int sampleCount)
    {
        float length = 0f;

        for (int i = 1; i < sampleCount; i++)
        {
            length += Vector3.Distance(targetLine.GetPosition(i - 1), targetLine.GetPosition(i));
        }

        return length;
    }
}
