using UnityEngine;

public sealed class PersistentId : MonoBehaviour
{
    [SerializeField] private string persistentId;

    public string Id => Normalize(persistentId);
    public bool HasId => Id.Length > 0;

    public bool TryGetId(out string id)
    {
        id = Id;
        return id.Length > 0;
    }

    private static string Normalize(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
