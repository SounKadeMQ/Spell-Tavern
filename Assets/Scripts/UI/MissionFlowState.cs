using System.Collections.Generic;

public static class MissionFlowState
{
    private static readonly HashSet<string> completedMissionIds = new HashSet<string>();
    private static readonly Dictionary<string, string> missionRanks = new Dictionary<string, string>();
    private static MissionData currentMission;

    public static MissionData CurrentMission => currentMission;

    public static void SetCurrentMission(MissionData mission)
    {
        currentMission = mission;
    }

    public static void MarkCompleted(MissionData mission)
    {
        MarkCompleted(mission, null);
    }

    public static void MarkCompleted(MissionData mission, string rank)
    {
        if (mission == null || string.IsNullOrEmpty(mission.missionId))
        {
            return;
        }

        completedMissionIds.Add(mission.missionId);

        if (!string.IsNullOrWhiteSpace(rank) &&
            (!missionRanks.TryGetValue(mission.missionId, out string existingRank) ||
             IsBetterRank(rank, existingRank)))
        {
            missionRanks[mission.missionId] = rank;
        }
    }

    public static bool IsCompleted(MissionData mission)
    {
        return mission != null &&
               !string.IsNullOrEmpty(mission.missionId) &&
               completedMissionIds.Contains(mission.missionId);
    }

    public static string GetRank(MissionData mission)
    {
        if (mission == null || string.IsNullOrEmpty(mission.missionId))
        {
            return string.Empty;
        }

        return missionRanks.TryGetValue(mission.missionId, out string rank) ? rank : string.Empty;
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

    static bool IsBetterRank(string newRank, string existingRank)
    {
        return GetRankScore(newRank) > GetRankScore(existingRank);
    }

    static int GetRankScore(string rank)
    {
        if (string.IsNullOrWhiteSpace(rank))
        {
            return 0;
        }

        if (rank.StartsWith("MS"))
        {
            return 6;
        }

        switch (rank[0])
        {
            case 'S':
                return 5;
            case 'A':
                return 4;
            case 'B':
                return 3;
            case 'C':
                return 2;
            case 'D':
                return 1;
            default:
                return 0;
        }
    }
}
