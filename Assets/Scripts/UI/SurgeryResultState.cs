public static class SurgeryResultState
{
    public static MissionData Mission { get; private set; }
    public static int StageScore { get; private set; }
    public static int VitalBonus { get; private set; }
    public static int TimeBonus { get; private set; }
    public static int SpecialBonus { get; private set; }
    public static int OperationScore { get; private set; }
    public static int Misses { get; private set; }
    public static string Rank { get; private set; }
    public static string Breakdown { get; private set; }
    public static string RetrySceneName { get; private set; }

    public static bool HasResult => Mission != null;

    public static void Capture(
        MissionData mission,
        SpellController spellController,
        Patient patient,
        SurgeryTimer timer,
        string retrySceneName)
    {
        Mission = mission;
        StageScore = spellController != null ? spellController.GetScore() : 0;
        Misses = spellController != null ? spellController.GetMissCount() : 0;
        Rank = spellController != null ? spellController.GetCurrentScoreRank() : string.Empty;
        Breakdown = spellController != null ? spellController.GetMissionCompleteBreakdown() : string.Empty;
        RetrySceneName = string.IsNullOrWhiteSpace(retrySceneName) ? "PatientScene" : retrySceneName;

        float bloodPercent = patient != null ? patient.bloodLevel / UnityEngine.Mathf.Max(1f, patient.MaxBlood) : 0f;
        VitalBonus = UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp01(bloodPercent) * 500f);

        float remainingTime = timer != null ? timer.RemainingTime : 0f;
        TimeBonus = UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Max(0f, remainingTime) * 5f);

        SpecialBonus = CalculateSpecialBonus();
        OperationScore = StageScore + VitalBonus + TimeBonus + SpecialBonus;
    }

    public static void Clear()
    {
        Mission = null;
        StageScore = 0;
        VitalBonus = 0;
        TimeBonus = 0;
        SpecialBonus = 0;
        OperationScore = 0;
        Misses = 0;
        Rank = string.Empty;
        Breakdown = string.Empty;
        RetrySceneName = "PatientScene";
    }

    static int CalculateSpecialBonus()
    {
        int bonus = 0;

        if (Misses == 0)
        {
            bonus += 1000;
        }
        else if (Misses <= 2)
        {
            bonus += 400;
        }

        if (StageScore >= 1800)
        {
            bonus += 500;
        }

        return bonus;
    }
}
