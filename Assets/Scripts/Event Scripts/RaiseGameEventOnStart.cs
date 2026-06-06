using System.Collections;
using UnityEngine;

public sealed class RaiseGameEventOnStart : MonoBehaviour
{
    [SerializeField] private GameEvent gameEvent;
    [SerializeField, Min(0f)] private float delaySeconds;
    [SerializeField] private bool useUnscaledTime = true;

    private void Start()
    {
        if (delaySeconds > 0f)
            StartCoroutine(RaiseAfterDelay());
        else
            Raise();
    }

    private IEnumerator RaiseAfterDelay()
    {
        if (useUnscaledTime)
        {
            float endTime = Time.unscaledTime + delaySeconds;
            while (Time.unscaledTime < endTime)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        Raise();
    }

    private void Raise()
    {
        gameEvent?.Raise();
    }
}
