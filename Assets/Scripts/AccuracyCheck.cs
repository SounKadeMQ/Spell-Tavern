using UnityEngine;

public class AccuracyCheck : MonoBehaviour
{
    [SerializeField] GameObject line;
    private LineRenderer tracerLine;
    private LineRenderer drawLine;
    [SerializeField] private float accuracy = 0;
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

            int posCount = Mathf.Min(tracerLine.positionCount,drawLine.positionCount);
            if (posCount <= 0)
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
}
