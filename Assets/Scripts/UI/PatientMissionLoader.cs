using UnityEngine;

public class PatientMissionLoader : MonoBehaviour
{
    [SerializeField] private Patient patient;

    void Awake()
    {
        if (patient == null)
        {
            patient = FindAnyObjectByType<Patient>();
        }

        MissionData mission = MissionFlowState.CurrentMission;
        if (patient != null && mission != null && mission.patientData != null)
        {
            patient.Initialize(mission.patientData);
        }
    }
}
