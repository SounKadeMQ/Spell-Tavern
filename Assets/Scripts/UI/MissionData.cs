using UnityEngine;

[CreateAssetMenu(fileName = "MissionData", menuName = "Spell Tavern/Mission Data")]
public class MissionData : ScriptableObject
{
    public enum MissionKind
    {
        Surgery,
        Intermission
    }

    public string missionId;
    public string displayName;
    public string chapterLabel = "Chapter 1";
    public MissionKind kind = MissionKind.Surgery;
    public PatientData patientData;

    [TextArea(2, 5)]
    public string missionSummary;

    public string sceneName = "preOpScene";
    public string nextMissionId;
    public bool unlockedByDefault;
    public DialogueLine[] preOpLines;
    public PatientDialogueLine[] surgeryLines;
    public DialogueLine[] intermissionLines;
}
