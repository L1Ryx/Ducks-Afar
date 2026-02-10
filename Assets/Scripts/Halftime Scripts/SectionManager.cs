using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SectionManager : MonoBehaviour
{
    public enum Section { Left = 0, Middle = 1, Right = 2, Unknown = 99 }
    public enum Mode { Volumes, BoundsByX }

    [System.Serializable]
    public struct LightingPreset
    {
        public Color color;

        [Min(0f)]
        public float intensity;

        [Range(0f, 1f)]
        public float shadowIntensity;
    }

    [Header("Audio")] [SerializeField] private AudioCue glitchSound;

    [Header("Global Light 2D + Presets")]
    [SerializeField] private Light2D globalLight2D;
    [Header("Post Processing")]
    [SerializeField] private PostProcessBurstHandler postProcessBurst;
    private readonly HashSet<SectionVolume> activeVolumes = new HashSet<SectionVolume>();


    [SerializeField] private LightingPreset leftLighting;
    [SerializeField] private LightingPreset middleLighting;
    [SerializeField] private LightingPreset rightLighting;

    // Optional: what to do when Unknown
    [SerializeField] private bool keepLastLightingWhenUnknown = true;
    [Header("Config")]
    [SerializeField] private Mode mode = Mode.Volumes;
    [SerializeField] private Transform player;

    [Header("BoundsByX Mode (world-space X cut points)")]
    [SerializeField] private float leftToMiddleX = -2f;
    [SerializeField] private float middleToRightX = 2f;

    [Header("Section World Roots (enable only the active one)")]
    [SerializeField] private GameObject leftWorldRoot;
    [SerializeField] private GameObject middleWorldRoot;
    [SerializeField] private GameObject rightWorldRoot;

    [Header("Debug")]
    [field: SerializeField] public Section CurrentSection { get; private set; } = Section.Unknown;
    [SerializeField] private Section previousSection = Section.Unknown;

    private int volumesInsideCount = 0;

    private void Start()
    {
        ApplyWorldActivation(CurrentSection);
        ApplyLightingForSection(CurrentSection);
    }

    
    private void ApplyLightingForSection(Section section)
    {
        if (globalLight2D == null) return;

        switch (section)
        {
            case Section.Left:
                ApplyPreset(leftLighting);
                break;
            case Section.Middle:
                ApplyPreset(middleLighting);
                break;
            case Section.Right:
                ApplyPreset(rightLighting);
                break;
            case Section.Unknown:
                if (!keepLastLightingWhenUnknown)
                {
                    // If you want a “neutral” fallback, pick one or add an Unknown preset.
                    ApplyPreset(middleLighting);
                }
                break;
        }
    }

    private void ApplyPreset(LightingPreset p)
    {
        globalLight2D.color = p.color;
        globalLight2D.intensity = p.intensity;
        globalLight2D.shadowIntensity = p.shadowIntensity;
    }
    
    public void AddActiveVolume(SectionVolume v)
    {
        if (mode != Mode.Volumes) return;
        if (v == null) return;

        activeVolumes.Add(v);
        ResolveSectionFromVolumes();
    }

    public void RemoveActiveVolume(SectionVolume v)
    {
        if (mode != Mode.Volumes) return;
        if (v == null) return;

        activeVolumes.Remove(v);
        ResolveSectionFromVolumes();
    }
    
    private void ResolveSectionFromVolumes()
    {
        if (player == null)
        {
            TransitionIfNeeded(Section.Unknown);
            return;
        }

        if (activeVolumes.Count == 0)
        {
            TransitionIfNeeded(Section.Unknown);
            return;
        }

        // Pick the volume whose transform is closest to the player.
        // Works well if each volume is centered in its band.
        SectionVolume best = null;
        float bestDistSq = float.PositiveInfinity;

        Vector3 p = player.position;

        foreach (var v in activeVolumes)
        {
            if (v == null) continue;

            float d = (v.transform.position - p).sqrMagnitude;
            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = v;
            }
        }

        TransitionIfNeeded(best != null ? best.Section : Section.Unknown);
    }



    private void Update()
    {
        if (mode == Mode.BoundsByX)
        {
            if (player == null) return;
            var next = EvaluateFromX(player.position.x);
            TransitionIfNeeded(next);
        }
    }

    private Section EvaluateFromX(float x)
    {
        if (x < leftToMiddleX) return Section.Left;
        if (x < middleToRightX) return Section.Middle;
        return Section.Right;
    }

    public void SetSectionFromVolume(Section section)
    {
        if (mode != Mode.Volumes) return;
        TransitionIfNeeded(section);
    }

    public void NotifyEnteredAnyVolume()
    {
        if (mode != Mode.Volumes) return;
        volumesInsideCount++;
    }

    public void NotifyExitedAnyVolume()
    {
        if (mode != Mode.Volumes) return;

        volumesInsideCount = Mathf.Max(0, volumesInsideCount - 1);
        if (volumesInsideCount == 0)
            TransitionIfNeeded(Section.Unknown);
    }

    private void TransitionIfNeeded(Section next)
    {
        if (next == CurrentSection) return;

        previousSection = CurrentSection;
        CurrentSection = next;

        ApplyWorldActivation(CurrentSection);
        ApplyLightingForSection(CurrentSection);

        if (postProcessBurst != null && CurrentSection != Section.Unknown)
        {
            postProcessBurst.Burst();
            Game.Ctx.Audio.PlayCueGlobal(glitchSound);
        }
            
            

    }



    private void ApplyWorldActivation(Section active)
    {
        // “Only active world stays enabled”
        if (leftWorldRoot != null) leftWorldRoot.SetActive(active == Section.Left);
        if (middleWorldRoot != null) middleWorldRoot.SetActive(active == Section.Middle);
        if (rightWorldRoot != null) rightWorldRoot.SetActive(active == Section.Right);

        // If you prefer Unknown => keep previous active instead, change logic here.
    }
}
