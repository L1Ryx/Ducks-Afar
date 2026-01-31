using System.Collections;
using UnityEngine;

[System.Serializable]
struct TimedCallbackInstance
{
    public float waitTime;
    public AudioCue[] audioCuesToPlay;
}
public class TimedCallback : MonoBehaviour
{
    [SerializeField] private GameEvent OnTimedCallbackFinish;
    [SerializeField] private TimedCallbackInstance[] timedCallbacks;
    private int currentTimedCallbackIdx = 0;

    public void DoTimedCallback()
    {
        StartCoroutine(timedCallbackCoroutine(timedCallbacks[currentTimedCallbackIdx].waitTime));
        foreach (AudioCue ac in  timedCallbacks[currentTimedCallbackIdx].audioCuesToPlay)
        {
            Game.Ctx.Audio.PlayCueGlobal(ac);
        }
    }

    private IEnumerator timedCallbackCoroutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        OnTimedCallbackFinish.Raise();;
        currentTimedCallbackIdx++;
        if (currentTimedCallbackIdx >= timedCallbacks.Length)
        {
            currentTimedCallbackIdx = 0;
        }
    } 
}
