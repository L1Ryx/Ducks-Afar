using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LevelStarter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float startDelay = 1f;

    [SerializeField] private bool lockInteractionsByDefault = true;
    [Header("Events")] 
    [SerializeField] private UnityEvent onInteractionsLocked;

    private void Awake()
    {
        if (lockInteractionsByDefault)
        {
            onInteractionsLocked?.Invoke();
        }
    }

    private void Start()
    {
        if (!Game.IsReady || Game.Ctx.LevelState == null)
        {
            Debug.LogWarning("LevelStarter: GameContext not ready.");
            return;
        }

        StartCoroutine(DelayedStart(startDelay));


    }

    private IEnumerator DelayedStart(float delay)
    {
        if (Game.IsReady && Game.Ctx?.SceneLoader != null)
            yield return Game.Ctx.SceneLoader.WaitUntilLoadComplete();

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Game.Ctx.LevelState.BeginLevel();
    }

    public void PrintStartLog()
    {
        Debug.Log("LevelStarter: Level has started.");
    }
}
