using UnityEngine;

public class PatientTest : MonoBehaviour
{
    public Patient patient;
    void Update()
    {
        if (patient == null) return;

        //b to start bleed
        if (Input.GetKeyDown(KeyCode.B))
        {
            patient.applyDamage(10f);
            Debug.Log("bleed test triggered 10f");
        }
        //h to start heavy bleed
        if (Input.GetKeyDown(KeyCode.H))
        {
            patient.applyDamage(30f);
            Debug.Log("bleed test triggered 30f");
        }
        //s to stop bleed
        if (Input.GetKeyDown(KeyCode.S))
        {
            patient.stopBleeding();
            Debug.Log("patient stopped bleeding");
        }
    }
}
