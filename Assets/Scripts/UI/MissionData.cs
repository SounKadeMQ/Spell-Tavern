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
    public string episodeCode;
    public int episodeOrder;
    public string chapterLabel = "Chapter 1";
    public MissionKind kind = MissionKind.Surgery;
    public PatientData patientData;

    [TextArea(2, 5)]
    public string missionSummary;

    public string sceneName = "preOpScene";
    public string surgeryMusicResource;
    public string nextMissionId;
    public bool unlockedByDefault;
    public DialogueLine[] preOpLines;
    public PatientDialogueLine[] surgeryLines;
    public DialogueLine[] intermissionLines;
    public WoundLayoutEntry[] woundLayout;
}

[System.Serializable]
public class WoundLayoutEntry
{
    public string woundName;
    public int woundIndex;
    public bool active = true;
    public CutWound.WoundType woundType = CutWound.WoundType.Cut;
    public CutWound.WoundLocation woundLocation = CutWound.WoundLocation.Part;
    public string spawnAreaId = "Chest";
    public Vector2 localPosition;
    public float rotationDegrees;
    public Vector2 localScale = Vector2.one;
}
