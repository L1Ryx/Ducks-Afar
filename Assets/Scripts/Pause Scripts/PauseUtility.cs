public static class PauseUtility
{
    public static bool IsPaused => Game.IsReady && Game.Ctx?.Pause?.IsPaused == true;
}
