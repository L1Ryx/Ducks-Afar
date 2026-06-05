using DG.Tweening;
using UnityEngine;

public class DuckIntroAnimations : MonoBehaviour
{
    [SerializeField] Transform TofuAnim;
    [SerializeField] Transform BubblesAnim;
    [SerializeField] Transform CodaAnim;
    [SerializeField] Transform AsterAnim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TofuAnim.DOLocalMoveY(TofuAnim.localPosition.y+10, 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        
        CodaAnim.DORotate(new Vector3(0, 0, 20), 5, RotateMode.WorldAxisAdd).
            SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);

        Sequence bubbleSequence1 = DOTween.Sequence();
        Sequence asterSequence1 = DOTween.Sequence();
    
        asterSequence1.Append(AsterAnim.DORotate(new Vector3(0, 0, 360), 30f, 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
        asterSequence1.Join(AsterAnim.DOLocalMoveX(AsterAnim.localPosition.x+30, 5f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
        
        bubbleSequence1.Append(BubblesAnim.DORotate(new Vector3(0, 0, 180), 8f).
                    SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear));
        // BubblesAnim.DORotate(new Vector3(0, 0, 45), 8f, RotateMode.LocalAxisAdd).
        //             SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
        // bubbleSequence1.Append(BubblesAnim.DOLocalMoveX(BubblesAnim.localPosition.x-32, 5f).
        //     SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
        // bubbleSequence1.Join(BubblesAnim.DOLocalMoveY(BubblesAnim.localPosition.y+29, 5f).
        //     SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
        bubbleSequence1.Join(BubblesAnim.DOMove(new Vector3(-30, 30, 0), 5f).SetRelative().
            SetEase(Ease.InOutSine).
            SetLoops(-1, LoopType.Yoyo));
        


    }

    // Update is called once per frame
    public void secondAnimation()
    {
        Sequence tofuSequence = DOTween.Sequence();
        Sequence bubbleSequence2 = DOTween.Sequence();
        Sequence asterSequence2 = DOTween.Sequence();
        Sequence codaSequence = DOTween.Sequence();

        tofuSequence.Append(TofuAnim.DOMove(new Vector3(-737, -409, 0), 1).SetRelative()
            .SetEase(Ease.InOutSine));
        tofuSequence.Join(TofuAnim.DORotate(new Vector3(0, 0, 360), 0.5f , 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
        
        bubbleSequence2.Append(BubblesAnim.DOMove(new Vector3(-548, 360, 0), 1)
            .SetEase(Ease.InOutSine));
        bubbleSequence2.Join(BubblesAnim.DORotate(new Vector3(0, 0, 360), 0.5f , 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
        
        asterSequence2.Append(AsterAnim.DOMove(new Vector3(563, 325, 0), 1).SetRelative()
            .SetEase(Ease.InOutSine));
        asterSequence2.Join(AsterAnim.DORotate(new Vector3(0, 0, 360), 0.5f , 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));
        
        codaSequence.Append(CodaAnim.DOMove(new Vector3(548, -360, 0), 1).SetRelative()
            .SetEase(Ease.InOutSine));
        codaSequence.Join(CodaAnim.DORotate(new Vector3(0, 0, 360), 0.5f , 
            RotateMode.WorldAxisAdd).SetLoops(-1).SetEase(Ease.Linear));

        Destroy(BubblesAnim);
        Destroy(TofuAnim);
        Destroy(AsterAnim);
        Destroy(CodaAnim);


    }
}
