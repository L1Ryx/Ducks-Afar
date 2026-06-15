using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class MacNativeFullscreen
{
    private const string ObjCLibrary = "libobjc";
    private const ulong NSWindowStyleMaskFullScreen = 1UL << 14;

    public static bool TrySetFullscreen(bool fullscreen)
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        IntPtr window;
        try
        {
            window = GetPlayerWindow();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MacNativeFullscreen: failed to access AppKit fullscreen. {ex.Message}");
            return false;
        }

        if (window == IntPtr.Zero)
        {
            Debug.LogWarning("MacNativeFullscreen: no macOS player window was available.");
            return false;
        }

        try
        {
            bool isFullscreen = IsFullscreen(window);
            if (isFullscreen != fullscreen)
                ToggleFullscreen(window);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MacNativeFullscreen: failed to toggle native fullscreen. {ex.Message}");
            return false;
        }

        return true;
#else
        return false;
#endif
    }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern ulong UInt64_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void Void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    private static IntPtr GetPlayerWindow()
    {
        IntPtr appClass = objc_getClass("NSApplication");
        if (appClass == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr app = IntPtr_objc_msgSend(appClass, sel_registerName("sharedApplication"));
        if (app == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr window = IntPtr_objc_msgSend(app, sel_registerName("keyWindow"));
        if (window != IntPtr.Zero)
            return window;

        return IntPtr_objc_msgSend(app, sel_registerName("mainWindow"));
    }

    private static bool IsFullscreen(IntPtr window)
    {
        ulong styleMask = UInt64_objc_msgSend(window, sel_registerName("styleMask"));
        return (styleMask & NSWindowStyleMaskFullScreen) != 0;
    }

    private static void ToggleFullscreen(IntPtr window)
    {
        Void_objc_msgSend_IntPtr(window, sel_registerName("toggleFullScreen:"), IntPtr.Zero);
    }
#endif
}
