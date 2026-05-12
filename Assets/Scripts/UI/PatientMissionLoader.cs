using UnityEngine;

public class PatientMissionLoader : MonoBehaviour
{
    [SerializeField] private Patient patient;

    void Awake()
    {
        // Patient.Awake owns mission patient initialization and wound layout application.
    }
}
