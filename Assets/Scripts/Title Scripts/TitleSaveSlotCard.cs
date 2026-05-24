using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TitleSaveSlotCard : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private int slotIndex;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private TMP_Text primaryButtonText;

    [Header("Buttons")]
    [SerializeField] private Button primaryButton;
    [SerializeField] private Button deleteButton;

    [Header("Display Options")]
    [SerializeField] private bool showSlotNumber = true;
    [SerializeField] private bool showSceneName = true;
    [SerializeField] private bool showLocation = true;
    [SerializeField] private bool showPlayTime = true;
    [SerializeField] private bool showCompanion = true;
    [SerializeField] private bool showLastSaved = true;

    [Header("Labels")]
    [SerializeField] private string emptyTitle = "New File";
    [SerializeField] private string occupiedTitleFormat = "File {0}";
    [SerializeField] private string newGameButtonLabel = "New Game";
    [SerializeField] private string continueButtonLabel = "Continue";

    private TitleFileSelectPanel owner;
    private SaveSlotData boundData;

    private void OnEnable()
    {
        primaryButton?.onClick.AddListener(HandlePrimaryClicked);
        deleteButton?.onClick.AddListener(HandleDeleteClicked);
    }

    private void OnDisable()
    {
        primaryButton?.onClick.RemoveListener(HandlePrimaryClicked);
        deleteButton?.onClick.RemoveListener(HandleDeleteClicked);
    }

    public void SetOwner(TitleFileSelectPanel fileSelectPanel)
    {
        owner = fileSelectPanel;
    }

    public void Bind(SaveSlotData data)
    {
        boundData = data ?? SaveSlotData.CreateEmpty(slotIndex);
        slotIndex = boundData.slotIndex;

        if (!boundData.hasData)
        {
            SetText(titleText, emptyTitle);
            SetText(detailsText, string.Empty);
            SetText(primaryButtonText, newGameButtonLabel);
            SetDeleteVisible(false);
            return;
        }

        SetText(titleText, string.Format(occupiedTitleFormat, slotIndex + 1));
        SetText(detailsText, BuildDetailsText(boundData));
        SetText(primaryButtonText, continueButtonLabel);
        SetDeleteVisible(true);
    }

    public void Use()
    {
        owner?.UseSlot(boundData);
    }

    public void Delete()
    {
        owner?.DeleteSlot(slotIndex);
    }

    private void HandlePrimaryClicked()
    {
        Use();
    }

    private void HandleDeleteClicked()
    {
        Delete();
    }

    private string BuildDetailsText(SaveSlotData data)
    {
        StringBuilder sb = new StringBuilder();

        AppendLine(sb, showSlotNumber, $"Slot: {data.slotIndex + 1}");
        AppendLine(sb, showSceneName, $"Scene: {Fallback(data.sceneName, "Unknown")}");
        AppendLine(sb, showLocation, $"Location: {Fallback(data.location, "Unknown")}");
        AppendLine(sb, showPlayTime, $"Playtime: {FormatPlayTime(data.timePlayedSeconds)}");
        AppendLine(sb, showCompanion, $"Companion: {Fallback(data.companionId, SaveSystem.NoneCompanionId)}");
        AppendLine(sb, showLastSaved, $"Saved: {FormatSavedTime(data.lastSavedUtc)}");

        return sb.ToString().TrimEnd();
    }

    private void SetDeleteVisible(bool visible)
    {
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(visible);
    }

    private static void AppendLine(StringBuilder sb, bool shouldShow, string line)
    {
        if (shouldShow)
            sb.AppendLine(line);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static string Fallback(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string FormatPlayTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        return time.Hours > 0
            ? $"{(int)time.TotalHours}h {time.Minutes:00}m"
            : $"{time.Minutes}m {time.Seconds:00}s";
    }

    private static string FormatSavedTime(string utcTimestamp)
    {
        if (string.IsNullOrWhiteSpace(utcTimestamp))
            return "Unknown";

        if (!DateTime.TryParse(
                utcTimestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime savedUtc))
        {
            return utcTimestamp;
        }

        return savedUtc.ToLocalTime().ToString("g");
    }
}
