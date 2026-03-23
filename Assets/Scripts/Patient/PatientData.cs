using UnityEngine;

[CreateAssetMenu(fileName = "PatientData", menuName = "Scriptable Objects/PatientData")]
public class PatientData : ScriptableObject
{
    public string patientName;
    public float startingBlood = 100f;

    public float startingBleedRate = 3f;
    public float startingBleedMod = 1f;
}
