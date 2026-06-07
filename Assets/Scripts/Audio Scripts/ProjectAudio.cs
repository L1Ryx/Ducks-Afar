using UnityEngine;

public static class ProjectAudio
{
    public static ProjectAudioConfig Config => ProjectAudioConfig.Instance;

    public static uint PlayGlobal(AudioCue cue)
    {
        if (cue == null || !cue.HasPlayEvent)
            return 0;

        if (!Game.IsReady || Game.Ctx?.Audio == null)
            return 0;

        return Game.Ctx.Audio.PlayCueGlobal(cue);
    }

    public static uint PlayOn(AudioCue cue, GameObject emitter)
    {
        if (cue == null || !cue.HasPlayEvent)
            return 0;

        if (!Game.IsReady || Game.Ctx?.Audio == null)
            return 0;

        return emitter != null
            ? Game.Ctx.Audio.PlayCueOn(cue, emitter)
            : Game.Ctx.Audio.PlayCueGlobal(cue);
    }

    public static void SetGlobalRtpc(AudioRtpc rtpc, float value)
    {
        if (rtpc == null || !rtpc.IsValid)
            return;

        if (!Game.IsReady || Game.Ctx?.Audio == null)
            return;

        Game.Ctx.Audio.SetGlobalRtpc(rtpc, value);
    }

    public static void SetGlobalMusic(AudioCue cue)
    {
        if (cue == null || !cue.HasPlayEvent)
            return;

        if (!Game.IsReady || Game.Ctx?.Audio == null)
            return;

        Game.Ctx.Audio.SetGlobalMusic(cue);
    }

    public static void StopGlobalMusic(bool immediate = false)
    {
        if (!Game.IsReady || Game.Ctx?.Audio == null)
            return;

        Game.Ctx.Audio.StopGlobalMusic(immediate);
    }

    public static void StopMusicAndAmbienceImmediate()
    {
        if (!Game.IsReady || Game.Ctx?.Audio == null)
            return;

        Game.Ctx.Audio.StopGlobalMusic(immediate: true);
        Game.Ctx.Audio.StopGlobalAmbience(immediate: true);
    }
}
