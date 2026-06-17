using UnityEditor;
using UnityEngine;

public sealed class HomeStageAutotilerSettings : ScriptableObject
{
    [Header("Beige Flowers")]
    [SerializeField] private GameObject[] beigeFlowerPrefabs;
    [SerializeField] private int seed = 9173;
    [SerializeField, Min(0.5f)] private float spread = 2.35f;
    [SerializeField, Range(0f, 2f)] private float density = 1f;
    [SerializeField, Min(0f)] private float minimumDistance = 1.35f;
    [SerializeField, Min(0f)] private float edgePadding = 0.42f;
    [SerializeField, Range(0f, 0.49f)] private float cellJitter = 0.34f;
    [SerializeField, Min(0f)] private float noiseScale = 0.18f;
    [SerializeField, Range(0f, 1f)] private float noiseInfluence = 0.25f;
    [SerializeField, Min(0)] private int maxGeneratedFlowers;
    [SerializeField] private bool avoidSceneSpriteBounds = true;
    [SerializeField, Min(0f)] private float objectAvoidancePadding = 0.25f;

    public GameObject[] BeigeFlowerPrefabs => beigeFlowerPrefabs ?? new GameObject[0];
    public int Seed => seed;
    public float Spread => Mathf.Max(0.5f, spread);
    public float Density => Mathf.Max(0f, density);
    public float MinimumDistance => Mathf.Max(0f, minimumDistance);
    public float EdgePadding => Mathf.Max(0f, edgePadding);
    public float CellJitter => Mathf.Clamp(cellJitter, 0f, 0.49f);
    public float NoiseScale => Mathf.Max(0f, noiseScale);
    public float NoiseInfluence => Mathf.Clamp01(noiseInfluence);
    public int MaxGeneratedFlowers => Mathf.Max(0, maxGeneratedFlowers);
    public bool AvoidSceneSpriteBounds => avoidSceneSpriteBounds;
    public float ObjectAvoidancePadding => Mathf.Max(0f, objectAvoidancePadding);

    public void EnsureDefaultPrefabs(string[] prefabPaths)
    {
        bool hasPrefab = false;
        if (beigeFlowerPrefabs != null)
        {
            for (int i = 0; i < beigeFlowerPrefabs.Length; i++)
            {
                if (beigeFlowerPrefabs[i] != null)
                {
                    hasPrefab = true;
                    break;
                }
            }
        }

        if (hasPrefab)
            return;

        beigeFlowerPrefabs = new GameObject[prefabPaths.Length];
        for (int i = 0; i < prefabPaths.Length; i++)
            beigeFlowerPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);

        EditorUtility.SetDirty(this);
    }

    private void OnValidate()
    {
        spread = Mathf.Max(0.5f, spread);
        density = Mathf.Max(0f, density);
        minimumDistance = Mathf.Max(0f, minimumDistance);
        edgePadding = Mathf.Max(0f, edgePadding);
        cellJitter = Mathf.Clamp(cellJitter, 0f, 0.49f);
        noiseScale = Mathf.Max(0f, noiseScale);
        noiseInfluence = Mathf.Clamp01(noiseInfluence);
        maxGeneratedFlowers = Mathf.Max(0, maxGeneratedFlowers);
        objectAvoidancePadding = Mathf.Max(0f, objectAvoidancePadding);
    }
}
