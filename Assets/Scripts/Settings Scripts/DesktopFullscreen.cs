using UnityEngine;

public static class DesktopFullscreen
{
    private const int FallbackWidth = 1920;
    private const int FallbackHeight = 1080;

    public static void Apply(bool fullscreen)
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        if (MacNativeFullscreen.TrySetFullscreen(fullscreen))
            return;

        Debug.LogWarning("DesktopFullscreen: native macOS fullscreen toggle failed; leaving current window mode unchanged.");
        return;
#elif UNITY_STANDALONE_WIN
        if (fullscreen)
        {
            Vector2Int resolution = GetBestDesktopResolution();
            Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.FullScreenWindow);
            return;
        }

        Screen.fullScreenMode = FullScreenMode.Windowed;
#else
        Screen.fullScreenMode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
#endif
    }

    private static Vector2Int GetBestDesktopResolution()
    {
        int width = Display.main.systemWidth;
        int height = Display.main.systemHeight;

        if (width <= 0 || height <= 0)
        {
            Resolution currentResolution = Screen.currentResolution;
            width = currentResolution.width;
            height = currentResolution.height;
        }

        if (width <= 0 || height <= 0)
        {
            width = Screen.width;
            height = Screen.height;
        }

        if (width <= 0 || height <= 0)
            return new Vector2Int(FallbackWidth, FallbackHeight);

        return new Vector2Int(width, height);
    }
}
