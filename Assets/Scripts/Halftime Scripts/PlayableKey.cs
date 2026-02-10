using System.Collections;
using UnityEngine;

public sealed class PlayableKey : MonoBehaviour
{
    public enum KeyInput { G, H, J, K, L, UpArrow, DownArrow, LeftArrow, RightArrow }

    public enum AudioActionType
    {
        PostGlobalEventName,
        PlayCueGlobal,
        SetGlobalRtpcByName,
        SetGlobalRtpcAsset,
        SetStateByStrings,
        SetStateAssets,
        AddToRtpc,
        SubtractFromRtpc

    }
    
    [Header("RTPC Delta (add/subtract)")]
    [SerializeField] private AudioRtpc deltaRtpcAsset;     // preferred
    [SerializeField] private string deltaRtpcName;         // fallback
    [SerializeField] private float deltaAmount = 1f;
    [SerializeField] private float deltaStartValue = 0f;

    private float deltaCurrentValue;


    [Header("Section gating")]
    [SerializeField] private SectionManager sectionManager;
    [SerializeField] private SectionManager.Section belongsToSection = SectionManager.Section.Left;

    [Header("Input")]
    [SerializeField] private KeyInput keyInput = KeyInput.G;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite pressedSprite;
    [Min(0f)][SerializeField] private float pressedDuration = 0.08f;

    [Header("Audio action")]
    [SerializeField] private AudioActionType actionType = AudioActionType.PostGlobalEventName;

    [Tooltip("Used when actionType = PostGlobalEventName")]
    [SerializeField] private string wwiseEventName;

    [Tooltip("Used when actionType = PlayCueGlobal")]
    [SerializeField] private AudioCue cue;

    [Tooltip("Used when actionType = SetGlobalRtpcByName")]
    [SerializeField] private string rtpcName;
    [SerializeField] private float rtpcValue = 0f;

    [Tooltip("Used when actionType = SetGlobalRtpcAsset")]
    [SerializeField] private AudioRtpc rtpcAsset;
    [SerializeField] private float rtpcAssetValue = 0f;

    [Tooltip("Used when actionType = SetStateByStrings")]
    [SerializeField] private string stateGroupName;
    [SerializeField] private string stateValueName;

    [Tooltip("Used when actionType = SetStateAssets")]
    [SerializeField] private AudioStateGroup stateGroup;
    [SerializeField] private AudioStateValue stateValue;

    private Coroutine spriteRoutine;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;

        deltaCurrentValue = deltaStartValue;
    }
    
    /// <summary>
    /// Adds deltaAmount to the configured RTPC and applies it globally.
    /// Uses AudioRtpc asset if provided, otherwise uses string rtpc name.
    /// Stores the running value locally for prototyping.
    /// </summary>
    public void AddToRtpc()
    {
        ApplyRtpcDelta(+deltaAmount);
    }

    /// <summary>
    /// Subtracts deltaAmount from the configured RTPC and applies it globally.
    /// </summary>
    public void SubtractFromRtpc()
    {
        ApplyRtpcDelta(-deltaAmount);
    }

    /// <summary>
    /// Apply an arbitrary delta to the RTPC (positive or negative).
    /// </summary>
    public void ApplyRtpcDelta(float delta)
    {
        // Pick which RTPC identifier to use
        bool useAsset = deltaRtpcAsset != null;
        bool useName = !string.IsNullOrWhiteSpace(deltaRtpcName);

        if (!useAsset && !useName)
            return;

        deltaCurrentValue += delta;

        if (useAsset)
        {
            // Clamp using the asset’s range rules
            float clamped = deltaRtpcAsset.ClampValue(deltaCurrentValue);
            deltaCurrentValue = clamped;
            Game.Ctx.Audio.SetGlobalRtpc(deltaRtpcAsset, clamped);
        }
        else
        {
            // No min/max knowledge here; you can clamp manually if you want.
            Game.Ctx.Audio.SetGlobalRtpc(deltaRtpcName, deltaCurrentValue);
        }
    }



    private void Update()
    {
        if (!IsInPlayableSection())
            return;

        if (WasPressedThisFrame())
            Play();
    }

    private bool IsInPlayableSection()
    {
        if (sectionManager == null)
            return true; // Prototype-friendly: if unassigned, allow play.

        return sectionManager.CurrentSection == belongsToSection;
    }

    private bool WasPressedThisFrame()
    {
        switch (keyInput)
        {
            case KeyInput.G:         return Input.GetKeyDown(KeyCode.G);
            case KeyInput.H:         return Input.GetKeyDown(KeyCode.H);
            case KeyInput.J:         return Input.GetKeyDown(KeyCode.J);
            case KeyInput.K:         return Input.GetKeyDown(KeyCode.K);
            case KeyInput.L:         return Input.GetKeyDown(KeyCode.L);
            case KeyInput.UpArrow:   return Input.GetKeyDown(KeyCode.UpArrow);
            case KeyInput.DownArrow: return Input.GetKeyDown(KeyCode.DownArrow);
            case KeyInput.LeftArrow: return Input.GetKeyDown(KeyCode.LeftArrow);
            case KeyInput.RightArrow: return Input.GetKeyDown(KeyCode.RightArrow);
            default:                 return false;
        }
    }


    private void Play()
    {
        // 1) Visual feedback
        TriggerSpritePress();

        // 2) Audio action
        ExecuteAudioAction();
    }

    private void TriggerSpritePress()
    {
        if (spriteRenderer == null || pressedSprite == null || idleSprite == null)
            return;

        if (spriteRoutine != null)
            StopCoroutine(spriteRoutine);

        spriteRoutine = StartCoroutine(SpritePressRoutine());
    }

    private IEnumerator SpritePressRoutine()
    {
        spriteRenderer.sprite = pressedSprite;

        if (pressedDuration > 0f)
            yield return new WaitForSeconds(pressedDuration);

        spriteRenderer.sprite = idleSprite;
        spriteRoutine = null;
    }

    private void ExecuteAudioAction()
    {
        // Keep this “hard-coded” to your architecture: Game.Ctx.Audio.*
        // If Game/Ctx isn’t available in some scenes, wrap with try/catch or a null check in your actual codebase.

        switch (actionType)
        {
            case AudioActionType.PostGlobalEventName:
                if (!string.IsNullOrWhiteSpace(wwiseEventName))
                    Game.Ctx.Audio.PostGlobal(wwiseEventName);
                break;

            case AudioActionType.PlayCueGlobal:
                if (cue != null)
                    Game.Ctx.Audio.PlayCueGlobal(cue);
                break;

            case AudioActionType.SetGlobalRtpcByName:
                if (!string.IsNullOrWhiteSpace(rtpcName))
                    Game.Ctx.Audio.SetGlobalRtpc(rtpcName, rtpcValue);
                break;

            case AudioActionType.SetGlobalRtpcAsset:
                if (rtpcAsset != null)
                    Game.Ctx.Audio.SetGlobalRtpc(rtpcAsset, rtpcAssetValue);
                break;

            case AudioActionType.SetStateByStrings:
                if (!string.IsNullOrWhiteSpace(stateGroupName) && !string.IsNullOrWhiteSpace(stateValueName))
                    Game.Ctx.Audio.SetState(stateGroupName, stateValueName);
                break;

            case AudioActionType.SetStateAssets:
                // Use the validated SO-based helper if possible:
                if (stateGroup != null)
                    Game.Ctx.Audio.SetState(stateGroup, stateValue);
                else if (stateValue != null)
                    Game.Ctx.Audio.SetState(stateValue);
                break;
            
            case AudioActionType.AddToRtpc:
                AddToRtpc();
                break;

            case AudioActionType.SubtractFromRtpc:
                SubtractFromRtpc();
                break;

        }
    }
}
