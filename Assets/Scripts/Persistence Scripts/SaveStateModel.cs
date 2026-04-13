using UnityEngine;

public sealed class SaveStateModel
{
    public float TimePlayedSeconds { get; private set; }
    public string CurrentLocation { get; set; }
    public string CurrentCompanionId { get; set; }

    public SaveStateModel()
    {
        TimePlayedSeconds = 0f;
        CurrentLocation = string.Empty;
        CurrentCompanionId = SaveSystem.NoneCompanionId;
    }

    public void AddPlayTime(float deltaTime)
    {
        TimePlayedSeconds += Mathf.Max(0f, deltaTime);
    }

    public void SetPlayTime(float seconds)
    {
        TimePlayedSeconds = Mathf.Max(0f, seconds);
    }

    public void ResetForNewGame(string startLocation)
    {
        TimePlayedSeconds = 0f;
        CurrentLocation = startLocation ?? string.Empty;
        CurrentCompanionId = SaveSystem.NoneCompanionId;
    }
}