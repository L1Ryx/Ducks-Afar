using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSelectPlanet", menuName = "Level Select/Planet")]
public sealed class LevelSelectPlanetDefinition : ScriptableObject
{
    [SerializeField] private string planetId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite sprite;
    [SerializeField] private Vector2 levelCardsScreenOffset = new Vector2(180f, 0f);
    [SerializeField] private List<LevelDefinition> levels = new();

    public string PlanetId => planetId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? planetId : displayName;
    public Sprite Sprite => sprite;
    public Vector2 LevelCardsScreenOffset => levelCardsScreenOffset;
    public IReadOnlyList<LevelDefinition> Levels => levels;
}
