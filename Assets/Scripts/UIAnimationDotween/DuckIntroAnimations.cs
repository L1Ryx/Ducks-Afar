using DG.Tweening;
using UnityEngine;

public class DuckIntroAnimations : MonoBehaviour
{
    [SerializeField] Transform TofuAnim1;
    [SerializeField] Transform BubblesAnim1;
    [SerializeField] Transform CodaAnim1;
    [SerializeField] Transform AsterAnim1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TofuAnim1.DOLocalMoveY(transform.localPosition.y + 1, 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // Update is called once per frame
    public void secondAnimation()
    {
        Sequence tofuSequence = DOTween.Sequence();
        tofuSequence.Append(TofuAnim1.DOMove(new Vector3(-737, -409, 0), 1)
            .SetEase(Ease.InOutSine));
        tofuSequence.Join(TofuAnim1.DORotate(new Vector3(0, 0, 360), 0.5f , RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
    }
}
