using UnityEngine;

public class AccuracyCheck : MonoBehaviour
{
    [SerializeField] GameObject line;
    private LineRenderer tracerLine;
    private LineRenderer drawLine;
    [SerializeField] private float accuracy = 0;

    void Start()
    {
        tracerLine = GetComponent<LineRenderer>();
    }


    void Update()
    {
        if (Input.GetMouseButtonUp(0)) 
        {
            accuracy = 0;
            drawLine = line.GetComponent<LineRenderer>();
            int posCount = Mathf.Min(tracerLine.positionCount,drawLine.positionCount);
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
        }
    }
}
