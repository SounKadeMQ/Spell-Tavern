public static class DebugModeState
{
    public static bool IsEnabled { get; private set; }

    public static void Enable()
    {
        IsEnabled = true;
    }

    public static void Disable()
    {
        IsEnabled = false;
    }
}
