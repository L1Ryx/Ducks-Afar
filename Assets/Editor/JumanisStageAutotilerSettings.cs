using UnityEditor;
using UnityEngine;

public sealed class JumanisStageAutotilerSettings : ScriptableObject
{
    [System.Serializable]
    public sealed class DecorationSettings
    {
        [SerializeField] private GameObject[] prefabs;
        [SerializeField] private int seed = 9173;
        [SerializeField, Min(0.5f)] private float spread = 2.35f;
        [SerializeField, Range(0f, 2f)] private float density = 1f;
        [SerializeField, Min(0f)] private float minimumDistance = 1.35f;
        [SerializeField, Min(0f)] private float edgePadding = 0.42f;
        [SerializeField, Range(0f, 0.49f)] private float cellJitter = 0.34f;
        [SerializeField, Min(0f)] private float noiseScale = 0.18f;
        [SerializeField, Range(0f, 1f)] private float noiseInfluence = 0.25f;
        [SerializeField, Min(0)] private int maxGenerated;
        [SerializeField] private bool avoidSceneSpriteBounds = true;
        [SerializeField, Min(0f)] private float objectAvoidancePadding = 0.25f;

        public GameObject[] Prefabs => prefabs ?? new GameObject[0];
        public int Seed => seed;
        public float Spread => Mathf.Max(0.5f, spread);
        public float Density => Mathf.Max(0f, density);
        public float MinimumDistance => Mathf.Max(0f, minimumDistance);
        public float EdgePadding => Mathf.Max(0f, edgePadding);
        public float CellJitter => Mathf.Clamp(cellJitter, 0f, 0.49f);
        public float NoiseScale => Mathf.Max(0f, noiseScale);
        public float NoiseInfluence => Mathf.Clamp01(noiseInfluence);
        public int MaxGenerated => Mathf.Max(0, maxGenerated);
        public bool AvoidSceneSpriteBounds => avoidSceneSpriteBounds;
        public float ObjectAvoidancePadding => Mathf.Max(0f, objectAvoidancePadding);

        public void EnsureDefaultPrefabs(string[] prefabPaths)
        {
            bool hasPrefab = false;
            if (prefabs != null)
            {
                for (int i = 0; i < prefabs.Length; i++)
                {
                    if (prefabs[i] != null)
                    {
                        hasPrefab = true;
                        break;
                    }
                }
            }

            if (hasPrefab)
                return;

            prefabs = new GameObject[prefabPaths.Length];
            for (int i = 0; i < prefabPaths.Length; i++)
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
        }

        public void SetDefaults(int defaultSeed, float defaultSpread, float defaultDensity, float defaultMinimumDistance)
        {
            seed = defaultSeed;
            spread = Mathf.Max(0.5f, defaultSpread);
            density = Mathf.Max(0f, defaultDensity);
            minimumDistance = Mathf.Max(0f, defaultMinimumDistance);
        }

        public void Validate()
        {
            spread = Mathf.Max(0.5f, spread);
            density = Mathf.Max(0f, density);
            minimumDistance = Mathf.Max(0f, minimumDistance);
            edgePadding = Mathf.Max(0f, edgePadding);
            cellJitter = Mathf.Clamp(cellJitter, 0f, 0.49f);
            noiseScale = Mathf.Max(0f, noiseScale);
            noiseInfluence = Mathf.Clamp01(noiseInfluence);
            maxGenerated = Mathf.Max(0, maxGenerated);
            objectAvoidancePadding = Mathf.Max(0f, objectAvoidancePadding);
        }
    }

    [Header("Jumanis Flowers")]
    [SerializeField] private DecorationSettings flowers = new();

    [Header("Jumanis Grass Spots")]
    [SerializeField] private DecorationSettings grassSpots = new();

    public DecorationSettings Flowers => flowers;
    public DecorationSettings GrassSpots => grassSpots;

    public void EnsureDefaults(string[] flowerPrefabPaths, string[] grassSpotPrefabPaths)
    {
        flowers ??= new DecorationSettings();
        grassSpots ??= new DecorationSettings();

        flowers.EnsureDefaultPrefabs(flowerPrefabPaths);
        grassSpots.EnsureDefaultPrefabs(grassSpotPrefabPaths);
    }

    public void ApplyInitialDefaults()
    {
        flowers ??= new DecorationSettings();
        grassSpots ??= new DecorationSettings();

        flowers.SetDefaults(9173, 2.35f, 1.2f, 1.35f);
        grassSpots.SetDefaults(14891, 1.4f, 0.875f, 0.45f);
    }

    private void OnValidate()
    {
        flowers?.Validate();
        grassSpots?.Validate();
    }
}
