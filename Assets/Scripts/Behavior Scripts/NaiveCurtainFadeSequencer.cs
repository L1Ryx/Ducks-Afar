using System;
using System.Collections;
using UnityEngine;

public class NaiveCurtainFadeSequencer : MonoBehaviour
{
    private FadeInOnLevelStart fadeIn_t;
    private FadeOutOnLevelEnd fadeOut_t;
    private bool readyForDarknessTime;

    [Header("Settings")] [SerializeField] private float darknessTime = 5f;
    
    private void Awake()
    {
        fadeIn_t = this.gameObject.GetComponent<FadeInOnLevelStart>();
        fadeOut_t = this.gameObject.GetComponent<FadeOutOnLevelEnd>();
        readyForDarknessTime = false;
    }

    public void Begin()
    {
        fadeOut_t.PlayFadeToBlack();
    }

    void Update()
    {
        if (readyForDarknessTime)
        {
            StartCoroutine(HandleDarknessTime());
            readyForDarknessTime = false;
        }
    }

    public void ToggleDarknessTimeReadiness()
    {
        readyForDarknessTime = !readyForDarknessTime;
    }

    private IEnumerator HandleDarknessTime()
    {
        yield return new WaitForSeconds(darknessTime);
        fadeIn_t.PlayFadeFromBlack();
    }
}
