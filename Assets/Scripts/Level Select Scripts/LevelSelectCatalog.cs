using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSelectCatalog", menuName = "Level Select/Catalog")]
public sealed class LevelSelectCatalog : ScriptableObject
{
    [SerializeField] private List<LevelSelectPlanetDefinition> planets = new();

    public IReadOnlyList<LevelSelectPlanetDefinition> Planets => planets;
}
