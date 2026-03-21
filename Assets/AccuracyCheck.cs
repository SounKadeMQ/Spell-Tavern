using UnityEngine;

public class AccuracyCheck : MonoBehaviour
{
    [SerializeField] GameObject line;
    private LineRenderer tracerLine;
    private LineRenderer drawLine;

    void Start()
    {
        tracerLine = GetComponent<LineRenderer>();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonUp(0)) 
        {
            drawLine = line.GetComponent<LineRenderer>();
        }
    }
}
