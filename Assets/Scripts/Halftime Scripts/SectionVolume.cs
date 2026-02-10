using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SectionVolume : MonoBehaviour
{
    [SerializeField] private SectionManager.Section section = SectionManager.Section.Left;
    [SerializeField] private SectionManager manager;
    [SerializeField] private bool autoFindManager = true;

    public SectionManager.Section Section => section;

    private void Awake()
    {
        if (autoFindManager && manager == null)
            manager = FindFirstObjectByType<SectionManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (manager == null) return;
        if (!other.CompareTag("Player")) return;

        manager.AddActiveVolume(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (manager == null) return;
        if (!other.CompareTag("Player")) return;

        manager.RemoveActiveVolume(this);
    }
}