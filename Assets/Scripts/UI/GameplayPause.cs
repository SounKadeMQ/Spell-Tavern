using UnityEngine;

public static class GameplayPause
{
    public static bool IsPaused { get; private set; }
    public static float GameplayTimeScale { get; private set; } = 1f;

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
        ApplyTimeScale();
    }

    public static void SetGameplayTimeScale(float timeScale)
    {
        GameplayTimeScale = Mathf.Max(0.01f, timeScale);
        ApplyTimeScale();
    }

    public static void ResetGameplayTimeScale()
    {
        GameplayTimeScale = 1f;
        ApplyTimeScale();
    }

    static void ApplyTimeScale()
    {
        Time.timeScale = IsPaused ? 0f : GameplayTimeScale;
    }
}
