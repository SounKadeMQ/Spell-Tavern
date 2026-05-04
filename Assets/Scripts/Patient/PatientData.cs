using UnityEngine;

[CreateAssetMenu(fileName = "PatientData", menuName = "Scriptable Objects/PatientData")]
public class PatientData : ScriptableObject
{
    public string patientName;
    public string caseTitle;

    [TextArea(2, 5)]
    public string triageSummary;

    [TextArea(2, 5)]
    public string surgicalGoal;

    public string recommendedSpells;
    public int chapterNumber = 1;
    public int operationNumber = 1;
    public float operationTimeLimit = 180f;
    public float startingBlood = 100f;

    public float startingBleedRate = 3f;
    public float startingBleedMod = 1f;
}
