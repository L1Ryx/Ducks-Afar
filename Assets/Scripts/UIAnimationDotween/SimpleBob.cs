using System.Numerics;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;

public class UIAnimation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float bobHeight = 1f;   // Distance to move up and down
    [SerializeField] private float duration = 0.3f;  // Time it takes to reach the peak

    void Start()
    {
        transform.DOLocalMoveY(transform.localPosition.y + bobHeight, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }
}
