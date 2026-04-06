using UnityEngine;
using DG.Tweening;
using UnityEngine.Splines;


public class TestMove : MonoBehaviour
{
    enum EaseType
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
    }
    [SerializeField] private Transform rightTarget;
    [SerializeField] private Transform leftTarget;
    [SerializeField] private bool atRight = false;
    [SerializeField] private EaseType currentEaseType = EaseType.Linear;

    // Update is called once per frame
    void Update()
    {
        MoveGameObjectWithEase();

        HandleInputForTransformEffects();
    }

    private void HandleInputForTransformEffects()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            gameObject.transform.DOPunchPosition(Vector3.up * 2, 0.5f, 10, 1f);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            gameObject.transform.DOPunchRotation(Vector3.forward * 2, 0.5f, 10, 1f);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            gameObject.transform.DOShakePosition(0.5f, 0.5f, 10, 90, false, false);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            gameObject.transform.DOShakePosition(0.5f, 0.5f, 10, 90, true, false);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            gameObject.transform.DOShakeRotation(0.5f, 0.5f, 10, .5f, false, ShakeRandomnessMode.Full);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            gameObject.transform.DOShakeScale(0.5f, 0.5f, 10, .5f, false, ShakeRandomnessMode.Full);
        }
    }

    private void MoveGameObjectWithEase()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Ease ease;
            switch (currentEaseType)
            {
                case EaseType.Linear: ease = Ease.Linear; break;
                case EaseType.InQuad: ease = Ease.InQuad; break;
                case EaseType.OutQuad: ease = Ease.OutQuad; break;
                case EaseType.InOutQuad: ease = Ease.InOutQuad; break;
                default: ease = Ease.Linear; 
                    break;
            }
            gameObject.transform.DOMove(atRight ? leftTarget.position : rightTarget.position, 1f).SetEase(ease);
            atRight = !atRight;
        }
    }
}
