using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseDraw : MonoBehaviour
{
    Coroutine drawing;
    bool letGo;
    GameObject newLine;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            StartLine();
        }
        if(Input.GetMouseButtonUp(0))
        {
            EndLine();
        }
    }


    void StartLine()
    {
        letGo = false;
        if(drawing!=null)
        {
            StopCoroutine(drawing);
        }
        drawing = StartCoroutine(DrawLine());
    }

    void EndLine()
    {
        StartCoroutine(ClearLine());
        StopCoroutine(ClearLine());
    }

    IEnumerator DrawLine()
    {
        newLine = Instantiate(Resources.Load("Line") as GameObject, new Vector3(0,0,0), Quaternion.identity);
        LineRenderer line = newLine.GetComponent<LineRenderer>();
        line.positionCount = 0;

        if(letGo == true)
        {
            Destroy(newLine, 1f);
        }
        while(letGo == false)
        {
            Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            position.z = 0;
            line.positionCount++;
            line.SetPosition(line.positionCount-1, position);
            yield return null;

        }
    }

    IEnumerator ClearLine()
    {
        yield return new WaitForSeconds(0);
        letGo = true;
    }
}
