using UnityEngine;

public class PatientTest : MonoBehaviour
{
    public Patient patient;
    void Update()
    {
        if (!DebugModeState.IsEnabled || patient == null) return;

        //b to start bleed
        if (Input.GetKeyDown(KeyCode.B))
        {
            patient.applyDamage(5f);
            Debug.Log("bleed test triggered 5f");
        }
        //h to start heavy bleed
        if (Input.GetKeyDown(KeyCode.H))
        {
            patient.applyDamage(15f);
            Debug.Log("bleed test triggered 15f");
        }
        //s to stop bleed
        if (Input.GetKeyDown(KeyCode.S))
        {
            patient.stopBleeding();
            Debug.Log("patient stopped bleeding");
        }
    }
}
