using DG.Tweening;
using UnityEngine;

public class DuckIntroAnimations : MonoBehaviour
{
    [SerializeField] RectTransform TofuAnim;
    [SerializeField] RectTransform BubblesAnim;
    [SerializeField] RectTransform CodaAnim;
    [SerializeField] RectTransform AsterAnim;
    [SerializeField] CanvasGroup root;
    private Sequence tofuSequence = DOTween.Sequence();
    private Sequence bubbleSequence2 = DOTween.Sequence();
    private Sequence asterSequence2 = DOTween.Sequence();
    private Sequence codaSequence = DOTween.Sequence();
    private Sequence bubbleSequence1 = DOTween.Sequence();
    private Sequence asterSequence1 = DOTween.Sequence();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TofuAnim.DOAnchorPosY(TofuAnim.anchoredPosition.y + 10f, 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        
        CodaAnim.DORotate(new Vector3(0, 0, 20), 5, RotateMode.WorldAxisAdd).
            SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
    
        asterSequence1.Append(AsterAnim.DORotate(new Vector3(0, 0, 360), 30f, 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
        asterSequence1.Join(AsterAnim.DOAnchorPosX(AsterAnim.anchoredPosition.x + 30f, 5f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
        
        bubbleSequence1.Append(BubblesAnim.DORotate(new Vector3(0, 0, 180), 8f).
                    SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear));
        // BubblesAnim.DORotate(new Vector3(0, 0, 45), 8f, RotateMode.LocalAxisAdd).
        //             SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
        // bubbleSequence1.Append(BubblesAnim.DOLocalMoveX(BubblesAnim.localPosition.x-32, 5f).
        //     SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
        // bubbleSequence1.Join(BubblesAnim.DOLocalMoveY(BubblesAnim.localPosition.y+29, 5f).
        //     SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
        bubbleSequence1.Join(BubblesAnim.DOAnchorPos(new Vector2(-30f, 30f), 5f).SetRelative().
            SetEase(Ease.InOutSine).
            SetLoops(-1, LoopType.Yoyo));
        


    }

    // Update is called once per frame
    public void secondAnimation()
    {

        tofuSequence.Append(TofuAnim.DOAnchorPos(new Vector2(-737f, -409f), 1f).SetRelative()
            .SetEase(Ease.InOutSine));
        tofuSequence.Join(TofuAnim.DORotate(new Vector3(0, 0, 360), 0.5f , 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
        
        bubbleSequence2.Append(BubblesAnim.DOAnchorPos(new Vector2(-548f, 360f), 1f)
            .SetEase(Ease.InOutSine));
        bubbleSequence2.Join(BubblesAnim.DORotate(new Vector3(0, 0, 360), 0.5f , 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
        
        asterSequence2.Append(AsterAnim.DOAnchorPos(new Vector2(563f, 325f), 1f).SetRelative()
            .SetEase(Ease.InOutSine));
        asterSequence2.Join(AsterAnim.DORotate(new Vector3(0, 0, 360), 0.5f , 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
        
        codaSequence.Append(CodaAnim.DOAnchorPos(new Vector2(548f, -360f), 1f).SetRelative()
            .SetEase(Ease.InOutSine));
        codaSequence.Join(CodaAnim.DORotate(new Vector3(0, 0, 360), 0.5f , 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));

        Invoke(nameof(killAnims), 1.0f);


    }

    public void killAnims()
    {
        // asterSequence2.Kill();
        // tofuSequence.Kill();
        // bubbleSequence2.Kill();
        // codaSequence.Kill();
        // bubbleSequence1.Kill();
        // asterSequence1.Kill();

        root.alpha = 0;

    }
}
