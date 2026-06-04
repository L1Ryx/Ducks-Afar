using DG.Tweening;
using UnityEngine;

public class UIIntroAnimation : MonoBehaviour
{
    [SerializeField] private Vector3 rotation_Vector = new Vector3(0f, 0f, 360f);
    [SerializeField] private float rotation_speed = 1f;
    [SerializeField] private float bobHeight = 1f;
    [SerializeField] private float duration = 0.3f;  

    void Start()
    {
        Sequence UISequence = DOTween.Sequence();
        UISequence.Append(transform.DOLocalMoveY(transform.localPosition.y + bobHeight, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo));
        UISequence.Join(transform.DORotate(rotation_Vector, rotation_speed, RotateMode.WorldAxisAdd).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear));
    }
}
