using System.Collections.Generic;

public static class MissionFlowState
{
    private static readonly HashSet<string> completedMissionIds = new HashSet<string>();
    private static MissionData currentMission;

    public static MissionData CurrentMission => currentMission;

    public static void SetCurrentMission(MissionData mission)
    {
        currentMission = mission;
    }

    public static void MarkCompleted(MissionData mission)
    {
        if (mission == null || string.IsNullOrEmpty(mission.missionId))
        {
            return;
        }

        completedMissionIds.Add(mission.missionId);
    }

    public static bool IsCompleted(MissionData mission)
    {
        return mission != null &&
               !string.IsNullOrEmpty(mission.missionId) &&
               completedMissionIds.Contains(mission.missionId);
    }

    public static bool IsUnlocked(MissionData mission, IReadOnlyList<MissionData> campaign)
    {
        if (mission == null)
        {
            return false;
        }

        if (mission.unlockedByDefault || IsCompleted(mission))
        {
            return true;
        }

        if (campaign == null)
        {
            return false;
        }

        for (int i = 0; i < campaign.Count; i++)
        {
            MissionData candidate = campaign[i];
            if (candidate != null &&
                candidate.nextMissionId == mission.missionId &&
                IsCompleted(candidate))
            {
                return true;
            }
        }

        return false;
    }
}
