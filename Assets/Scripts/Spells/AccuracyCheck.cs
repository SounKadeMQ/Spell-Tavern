using UnityEngine;

public class AccuracyCheck : MonoBehaviour
{
    [SerializeField] GameObject line;
    private LineRenderer tracerLine;
    private LineRenderer drawLine;
    [SerializeField] private float accuracy = 0;
    [SerializeField] private int minimumSampleCount = 3;
    [SerializeField] private float minimumStrokeLength = 2f;
    [SerializeField] private float minimumLengthCoverage = 0.7f;
    [SerializeField] private int normalizedSampleCount = 64;
    [SerializeField] private float normalizedPathTolerance = 0.22f;
    [SerializeField] private float minimumPathMatchRatio = 0.78f;
    [SerializeField] private float normalizedEndpointTolerance = 0.18f;
    [SerializeField] private float normalizedMidpointTolerance = 0.24f;
    [SerializeField] private float maximumPointDistance = 0.42f;
    [SerializeField] private float maximumAverageDistance = 0.3f;
    private bool hasFreshAccuracy;

    public float Accuracy => accuracy;

    void Start()
    {
        tracerLine = GetComponent<LineRenderer>();
    }


    void Update()
    {
        if (GameplayPause.IsPaused)
        {
            hasFreshAccuracy = false;
            return;
        }

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

            Vector2[] normalizedTracerPoints = GetNormalizedPoints(tracerLine);
            Vector2[] normalizedDrawPoints = GetNormalizedPoints(drawLine);
            if (normalizedTracerPoints == null || normalizedDrawPoints == null)
            {
                return;
            }

            bool reverseMatch = GetBestMatchDirection(normalizedTracerPoints, normalizedDrawPoints);
            AlignPointSetToTemplate(normalizedDrawPoints, normalizedTracerPoints, reverseMatch);

            if (!HasValidEndpoints(normalizedTracerPoints, normalizedDrawPoints, reverseMatch))
            {
                return;
            }

            if (!HasValidMidpoint(normalizedTracerPoints, normalizedDrawPoints, reverseMatch))
            {
                return;
            }

            if (!HasValidPathMatch(normalizedTracerPoints, normalizedDrawPoints, reverseMatch))
            {
                return;
            }

            float averageDistance = GetAverageDistance(normalizedTracerPoints, normalizedDrawPoints, reverseMatch);
            if (averageDistance > maximumAverageDistance)
            {
                return;
            }

            for (int i = 0; i < normalizedSampleCount; i++)
            {
                int drawIndex = reverseMatch
                    ? normalizedSampleCount - i - 1
                    : i;
                accuracy += Vector2.Distance(normalizedTracerPoints[i], normalizedDrawPoints[drawIndex]);
            }
            accuracy /= normalizedSampleCount;
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

    Vector2[] GetNormalizedPoints(LineRenderer targetLine)
    {
        Vector2[] resampledPoints = ResampleLine(targetLine, normalizedSampleCount);
        if (resampledPoints == null)
        {
            return null;
        }

        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < resampledPoints.Length; i++)
        {
            centroid += resampledPoints[i];
        }

        centroid /= resampledPoints.Length;

        float maxDistance = 0f;
        for (int i = 0; i < resampledPoints.Length; i++)
        {
            resampledPoints[i] -= centroid;
            maxDistance = Mathf.Max(maxDistance, resampledPoints[i].magnitude);
        }

        if (maxDistance <= Mathf.Epsilon)
        {
            return null;
        }

        for (int i = 0; i < resampledPoints.Length; i++)
        {
            resampledPoints[i] /= maxDistance;
        }

        return resampledPoints;
    }

    bool HasValidEndpoints(Vector2[] normalizedTracerPoints, Vector2[] normalizedDrawPoints, bool reverseMatch)
    {
        Vector2 drawStart = GetAlignedDrawPoint(normalizedDrawPoints, 0, reverseMatch);
        Vector2 drawEnd = GetAlignedDrawPoint(normalizedDrawPoints, normalizedTracerPoints.Length - 1, reverseMatch);

        return Vector2.Distance(normalizedTracerPoints[0], drawStart) <= normalizedEndpointTolerance &&
               Vector2.Distance(normalizedTracerPoints[normalizedTracerPoints.Length - 1], drawEnd) <= normalizedEndpointTolerance;
    }

    bool HasValidMidpoint(Vector2[] normalizedTracerPoints, Vector2[] normalizedDrawPoints, bool reverseMatch)
    {
        int midpointIndex = normalizedTracerPoints.Length / 2;
        Vector2 drawMidpoint = GetAlignedDrawPoint(normalizedDrawPoints, midpointIndex, reverseMatch);
        return Vector2.Distance(normalizedTracerPoints[midpointIndex], drawMidpoint) <= normalizedMidpointTolerance;
    }

    bool HasValidPathMatch(Vector2[] normalizedTracerPoints, Vector2[] normalizedDrawPoints, bool reverseMatch)
    {
        int matchingPoints = 0;

        for (int i = 0; i < normalizedTracerPoints.Length; i++)
        {
            float pointDistance = Vector2.Distance(
                normalizedTracerPoints[i],
                GetAlignedDrawPoint(normalizedDrawPoints, i, reverseMatch));

            if (pointDistance > maximumPointDistance)
            {
                return false;
            }

            if (pointDistance <= normalizedPathTolerance)
            {
                matchingPoints++;
            }
        }

        float matchRatio = (float)matchingPoints / normalizedTracerPoints.Length;
        return matchRatio >= minimumPathMatchRatio;
    }

    Vector2 GetAlignedDrawPoint(Vector2[] normalizedDrawPoints, int index, bool reverseMatch)
    {
        int drawIndex = reverseMatch
            ? normalizedDrawPoints.Length - index - 1
            : index;
        return normalizedDrawPoints[drawIndex];
    }

    bool GetBestMatchDirection(Vector2[] normalizedTracerPoints, Vector2[] normalizedDrawPoints)
    {
        Vector2[] forwardPoints = CopyPoints(normalizedDrawPoints);
        Vector2[] reversePoints = CopyPoints(normalizedDrawPoints);

        AlignPointSetToTemplate(forwardPoints, normalizedTracerPoints, false);
        AlignPointSetToTemplate(reversePoints, normalizedTracerPoints, true);

        float forwardDistance = GetAverageDistance(normalizedTracerPoints, forwardPoints, false);
        float reverseDistance = GetAverageDistance(normalizedTracerPoints, reversePoints, true);

        return reverseDistance < forwardDistance;
    }

    Vector2[] CopyPoints(Vector2[] points)
    {
        Vector2[] copy = new Vector2[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            copy[i] = points[i];
        }

        return copy;
    }

    float GetAverageDistance(Vector2[] normalizedTracerPoints, Vector2[] normalizedDrawPoints, bool reverseMatch)
    {
        float totalDistance = 0f;

        for (int i = 0; i < normalizedTracerPoints.Length; i++)
        {
            totalDistance += Vector2.Distance(
                normalizedTracerPoints[i],
                GetAlignedDrawPoint(normalizedDrawPoints, i, reverseMatch));
        }

        return totalDistance / normalizedTracerPoints.Length;
    }

    void AlignPointSetToTemplate(Vector2[] pointsToAlign, Vector2[] templatePoints, bool reverseMatch)
    {
        Vector2 sourceStart = reverseMatch
            ? pointsToAlign[pointsToAlign.Length - 1]
            : pointsToAlign[0];
        Vector2 sourceEnd = reverseMatch
            ? pointsToAlign[0]
            : pointsToAlign[pointsToAlign.Length - 1];

        Vector2 templateStart = templatePoints[0];
        Vector2 templateEnd = templatePoints[templatePoints.Length - 1];

        float sourceAngle = Mathf.Atan2(sourceEnd.y - sourceStart.y, sourceEnd.x - sourceStart.x) * Mathf.Rad2Deg;
        float templateAngle = Mathf.Atan2(templateEnd.y - templateStart.y, templateEnd.x - templateStart.x) * Mathf.Rad2Deg;
        float angleDelta = templateAngle - sourceAngle;

        Quaternion rotation = Quaternion.Euler(0f, 0f, angleDelta);
        for (int i = 0; i < pointsToAlign.Length; i++)
        {
            pointsToAlign[i] = rotation * pointsToAlign[i];
        }
    }

    Vector2[] ResampleLine(LineRenderer targetLine, int sampleCount)
    {
        if (targetLine == null || targetLine.positionCount < 2 || sampleCount < 2)
        {
            return null;
        }

        float totalLength = GetLineLength(targetLine, targetLine.positionCount);
        if (totalLength <= Mathf.Epsilon)
        {
            return null;
        }

        Vector2[] sampledPoints = new Vector2[sampleCount];
        float stepLength = totalLength / (sampleCount - 1);
        float traversedLength = 0f;
        float targetLength = 0f;
        int sampleIndex = 0;

        Vector3 previousPoint = targetLine.GetPosition(0);
        sampledPoints[sampleIndex++] = previousPoint;

        for (int i = 1; i < targetLine.positionCount && sampleIndex < sampleCount - 1; i++)
        {
            Vector3 currentPoint = targetLine.GetPosition(i);
            float segmentLength = Vector3.Distance(previousPoint, currentPoint);

            while (traversedLength + segmentLength >= targetLength + stepLength &&
                   sampleIndex < sampleCount - 1)
            {
                targetLength += stepLength;
                float distanceIntoSegment = targetLength - traversedLength;
                float t = segmentLength <= Mathf.Epsilon ? 0f : distanceIntoSegment / segmentLength;
                sampledPoints[sampleIndex++] = Vector3.Lerp(previousPoint, currentPoint, t);
            }

            traversedLength += segmentLength;
            previousPoint = currentPoint;
        }

        sampledPoints[sampleCount - 1] = targetLine.GetPosition(targetLine.positionCount - 1);
        return sampledPoints;
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
