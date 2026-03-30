using UnityEngine;

public class SpellTester : MonoBehaviour
{
    public Patient patient;

    public float bleedRate = 10f;
    public float waterAmount = 20f;

    void Update()
    {
        if (!DebugModeState.IsEnabled || patient == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            patient.applyDamage(bleedRate);
            Debug.Log("Fire damage or wound opened: bleeding started");
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            patient.restoreVitals(waterAmount);
            Debug.Log("Water spell cast: vitals restored");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            patient.stopBleeding();
            Debug.Log("Fire spell cast: wound cauterised");
        }
    }
}
