using System;
using UnityEngine;

/// <summary>
/// Context-owned audio state + policy.
/// Owns looping music / ambience decisions and exposes RTPC helpers.
/// 
/// Assumes Wwise Unity Integration is installed (AkSoundEngine available).
/// </summary>
public sealed class AudioStateModel
{
    private string _currentAmbienceEventName;
    private uint _currentAmbiencePlayingId;
    private AudioCue _currentAmbienceCue;

    private GameObject _globalEmitter;
    private AudioCue _currentMusicCue;

    // Track "current music" to prevent accidental restarts.
    private string _currentMusicEventName;
    private uint _currentMusicPlayingId;

    // Track paused state if you want to enforce policy.
    private bool _isPaused;

    /// <summary>
    /// Initialize with a GameObject that represents your global 2D emitter.
    /// A good default is the GameContext GameObject (DontDestroyOnLoad).
    /// </summary>
    public void Initialize(GameObject globalEmitter)
    {
        if (globalEmitter == null)
            throw new ArgumentNullException(nameof(globalEmitter));

        _globalEmitter = globalEmitter;
    }

    /// <summary>
    /// Posts a one-shot (or loop) event globally (2D-ish, or at least at the global emitter location).
    /// </summary>
    public uint PostGlobal(string wwiseEventName)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(wwiseEventName))
            return 0;

        return AkSoundEngine.PostEvent(wwiseEventName, _globalEmitter);
    }

    /// <summary>
    /// Posts an event on a specific emitter GameObject (3D positional).
    /// </summary>
    public uint PostOn(string wwiseEventName, GameObject emitter)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(wwiseEventName) || emitter == null)
            return 0;

        return AkSoundEngine.PostEvent(wwiseEventName, emitter);
    }

    // -------------------------
    // Music ownership (looping)
    // -------------------------

    /// <summary>
    /// Sets (starts) music by posting the event name on the global emitter.
    /// If the same music is already playing, it does nothing (idempotent).
    ///
    /// If you pass a different event name, it will stop the previous playing ID (if any)
    /// and start the new one. For crossfades, you typically want Wwise Music Switch/States
    /// or dedicated stop events; this is a minimal "ownership" baseline.
    /// </summary>
    public void SetMusic(string musicEventName)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(musicEventName))
            return;

        // Avoid restarting the same music.
        if (string.Equals(_currentMusicEventName, musicEventName, StringComparison.Ordinal) &&
            _currentMusicPlayingId != 0)
        {
            return;
        }

        // Stop previous track if we own a playing ID.
        StopMusic();

        _currentMusicEventName = musicEventName;
        _currentMusicPlayingId = AkSoundEngine.PostEvent(musicEventName, _globalEmitter);
    }

    /// <summary>
    /// Stops the currently owned music playing ID, if any.
    /// </summary>
    public void StopMusic()
    {
        if (_currentMusicPlayingId != 0)
        {
            AkSoundEngine.StopPlayingID(_currentMusicPlayingId);
            _currentMusicPlayingId = 0;
        }

        _currentMusicEventName = null;
    }
    
    // -------------------------
// Wwise State helpers
// -------------------------

    /// <summary>
    /// Set a Wwise State using SOs (preferred).
    /// Enforces: group/value validity + optional whitelist validation.
    /// </summary>
    public void SetState(AudioStateGroup group, AudioStateValue value)
    {
        EnsureInitialized();

        if (group == null || !group.IsValid)
            return;

        // Coalesce null to default if provided.
        var v = (value != null) ? value : group.defaultValue;
        if (v == null || !v.IsValid)
            return;

        // Ensure the value actually belongs to the group.
        // (Prevents authoring mistakes and accidental cross-group values.)
        if (!string.Equals(v.groupName, group.groupName, StringComparison.Ordinal))
            return;

        // Optional whitelist enforcement.
        if (!group.IsAllowed(v))
            return;

        AkSoundEngine.SetState(group.groupName, v.valueName);
    }

    /// <summary>
    /// Convenience overload if you want to set a state value directly.
    /// Validates the value asset itself.
    /// </summary>
    public void SetState(AudioStateValue value)
    {
        EnsureInitialized();

        if (value == null || !value.IsValid)
            return;

        AkSoundEngine.SetState(value.groupName, value.valueName);
    }

    /// <summary>
    /// String-based fallback (useful for quick probes or debugging).
    /// Prefer SetState(AudioStateGroup, AudioStateValue) for production.
    /// </summary>
    public void SetState(string groupName, string valueName)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(valueName))
            return;

        AkSoundEngine.SetState(groupName, valueName);
    }
    
    /// <summary>
    /// Applies a default state value (if set) for a given group.
    /// </summary>
    public void ApplyDefaultState(AudioStateGroup group)
    {
        EnsureInitialized();

        if (group == null || !group.IsValid)
            return;

        if (group.defaultValue == null || !group.defaultValue.IsValid)
            return;

        SetState(group, group.defaultValue);
    }
    
    public void SetGlobalMusic(AudioCue musicCue)
    {
        EnsureInitialized();

        if (musicCue == null || !musicCue.HasPlayEvent)
            return;

        // Idempotent: same cue already playing.
        if (_currentMusicCue == musicCue && _currentMusicPlayingId != 0)
            return;

        StopGlobalMusic(immediate: false);

        _currentMusicCue = musicCue;
        _currentMusicEventName = musicCue.playEvent;

        ApplyCueRtpcs(musicCue, emitter: null);
        _currentMusicPlayingId = AkSoundEngine.PostEvent(musicCue.playEvent, _globalEmitter);
    }

    public void StopGlobalMusic(bool immediate)
    {
        EnsureInitialized();

        if (immediate)
        {
            if (_currentMusicPlayingId != 0)
                AkSoundEngine.StopPlayingID(_currentMusicPlayingId);

            _currentMusicCue = null;
            _currentMusicEventName = null;
            _currentMusicPlayingId = 0;
            return;
        }

        // Graceful stop prefers stopEvent.
        if (_currentMusicCue != null && _currentMusicCue.HasStopEvent)
            AkSoundEngine.PostEvent(_currentMusicCue.stopEvent, _globalEmitter);
        else if (_currentMusicPlayingId != 0)
            AkSoundEngine.StopPlayingID(_currentMusicPlayingId);

        _currentMusicCue = null;
        _currentMusicEventName = null;
        _currentMusicPlayingId = 0;
    }



    // -------------------------
    // RTPC helpers
    // -------------------------

    /// <summary>
    /// Set a global RTPC value (applies project-wide).
    /// </summary>
    public void SetGlobalRtpc(string rtpcName, float value)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(rtpcName))
            return;

        // Global RTPC: no emitter needed
        AkSoundEngine.SetRTPCValue(rtpcName, value);
    }

    /// <summary>
    /// Set an RTPC value scoped to a specific emitter.
    /// Useful for per-object parameters (e.g., engine RPM, proximity intensity, etc.).
    /// </summary>
    public void SetRtpcOn(string rtpcName, float value, GameObject emitter)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(rtpcName) || emitter == null)
            return;

        AkSoundEngine.SetRTPCValue(rtpcName, value, emitter);
    }

    // -------------------------
    // Optional: Pause policy
    // -------------------------

    public void SetPaused(bool paused)
    {
        EnsureInitialized();

        if (_isPaused == paused)
            return;

        _isPaused = paused;

        // This is a placeholder policy.
        // Many teams prefer a Wwise State (e.g., "GameState=Paused") or a global RTPC (e.g., "PauseLowpass").
        // Uncomment if you have a pause state in Wwise:
        // AkSoundEngine.SetState("GameState", paused ? "Paused" : "Unpaused");
    }
    
        /// <summary>
    /// Plays a cue globally (posts play event and applies any RTPC bindings).
    /// </summary>
    public uint PlayCueGlobal(AudioCue cue)
    {
        EnsureInitialized();
        if (cue == null || !cue.HasPlayEvent)
            return 0;

        ApplyCueRtpcs(cue, emitter: null);
        return AkSoundEngine.PostEvent(cue.playEvent, _globalEmitter);
    }

    /// <summary>
    /// Plays a cue on a specific emitter (posts play event and applies any RTPC bindings).
    /// </summary>
    public uint PlayCueOn(AudioCue cue, GameObject emitter)
    {
        EnsureInitialized();
        if (cue == null || !cue.HasPlayEvent || emitter == null)
            return 0;

        ApplyCueRtpcs(cue, emitter);
        return AkSoundEngine.PostEvent(cue.playEvent, emitter);
    }

    /// <summary>
    /// Stops a cue globally, if it has a stop event.
    /// </summary>
    public void StopCueGlobal(AudioCue cue)
    {
        EnsureInitialized();
        if (cue == null || !cue.HasStopEvent)
            return;

        AkSoundEngine.PostEvent(cue.stopEvent, _globalEmitter);
    }

    /// <summary>
    /// Stops a cue on a specific emitter, if it has a stop event.
    /// </summary>
    public void StopCueOn(AudioCue cue, GameObject emitter)
    {
        EnsureInitialized();
        if (cue == null || !cue.HasStopEvent || emitter == null)
            return;

        AkSoundEngine.PostEvent(cue.stopEvent, emitter);
    }

    private void ApplyCueRtpcs(AudioCue cue, GameObject emitter)
    {
        if (cue == null || cue.rtpcBindings == null)
            return;

        for (int i = 0; i < cue.rtpcBindings.Length; i++)
        {
            var binding = cue.rtpcBindings[i];

            // Validate binding
            if (binding.rtpc == null || !binding.rtpc.IsValid)
                continue;

            float v = binding.rtpc.ClampValue(binding.value);

            if (binding.isGlobal)
            {
                AkSoundEngine.SetRTPCValue(binding.rtpc.rtpcName, v);
            }
            else if (emitter != null)
            {
                AkSoundEngine.SetRTPCValue(binding.rtpc.rtpcName, v, emitter);
            }
        }
    }

    
    public void SetGlobalRtpc(AudioRtpc rtpc, float value)
    {
        EnsureInitialized();
        if (rtpc == null || !rtpc.IsValid)
            return;

        float v = rtpc.ClampValue(value);
        AkSoundEngine.SetRTPCValue(rtpc.rtpcName, v);
    }

    public void SetRtpcOn(AudioRtpc rtpc, float value, GameObject emitter)
    {
        EnsureInitialized();
        if (rtpc == null || !rtpc.IsValid || emitter == null)
            return;

        float v = rtpc.ClampValue(value);
        AkSoundEngine.SetRTPCValue(rtpc.rtpcName, v, emitter);
    }

    private void EnsureInitialized()
    {
        if (_globalEmitter == null)
            throw new InvalidOperationException("AudioStateModel is not initialized. Call Initialize(globalEmitter) from GameContext.");
    }
    
    public void SetGlobalAmbience(AudioCue ambienceCue)
    {
        EnsureInitialized();

        if (ambienceCue == null || !ambienceCue.HasPlayEvent)
            return;

        // Idempotent: if the same cue is already playing, do nothing.
        if (_currentAmbienceCue == ambienceCue && _currentAmbiencePlayingId != 0)
            return;

        StopGlobalAmbience(immediate: false);

        _currentAmbienceCue = ambienceCue;
        _currentAmbienceEventName = ambienceCue.playEvent;

        // Apply cue RTPCs globally (or emitter-scoped if you prefer; global ambience typically global RTPCs)
        ApplyCueRtpcs(ambienceCue, emitter: null);

        _currentAmbiencePlayingId = AkSoundEngine.PostEvent(ambienceCue.playEvent, _globalEmitter);
    }

    public void StopGlobalAmbience(bool immediate)
    {
        // Debug.Log($"StopGlobalAmbience called\n{System.Environment.StackTrace}");
        EnsureInitialized();

        if (immediate)
        {
            // Hard stop: guarantees it is gone before scene reload.
            if (_currentAmbiencePlayingId != 0)
                AkSoundEngine.StopPlayingID(_currentAmbiencePlayingId);

            // Optional: also stop all instances of the object if you don't trust playing IDs:
            // AkSoundEngine.ExecuteActionOnEvent(_currentAmbienceCue.playEvent, AkActionOnEventType.AkActionOnEventType_Stop, _globalEmitter, 0);

            _currentAmbienceCue = null;
            _currentAmbienceEventName = null;
            _currentAmbiencePlayingId = 0;
            return;
        }

        // Graceful stop: fade-out via stop event (your current behavior)
        if (_currentAmbienceCue != null && _currentAmbienceCue.HasStopEvent)
            AkSoundEngine.PostEvent(_currentAmbienceCue.stopEvent, _globalEmitter);
        else if (_currentAmbiencePlayingId != 0)
            AkSoundEngine.StopPlayingID(_currentAmbiencePlayingId);

        _currentAmbienceCue = null;
        _currentAmbienceEventName = null;
        _currentAmbiencePlayingId = 0;
    }


}
