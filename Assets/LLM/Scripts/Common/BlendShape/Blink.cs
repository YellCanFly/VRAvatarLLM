using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blink : FacialAnimation
{
    [Header("Blink Parameters")]
    [Tooltip("Minimum time interval between blinks (in seconds).")]
    public float minBlinkInterval = 2.0f;

    [Tooltip("Maximum time interval between blinks (in seconds).")]
    public float maxBlinkInterval = 5.0f;

    [Tooltip("Duration of a single blink (in seconds).")]
    public float blinkDuration = 0.2f;
    private List<BlendShapeGroup> _blendShapeGroups;
    private bool _isBlinking = false;

    protected override void SetFaceAnimation()
    {
        _faceAnimationTarget = FaceAnimationType.Blink;
    }

    protected override void Init()
    {
        base.Init();
        _blendShapeGroups = BlendShapeMapperSO.GetGroups(_faceAnimationTarget);
    }

    private void Start()
    {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float blinkInterval = Random.Range(minBlinkInterval, maxBlinkInterval);
            yield return new WaitForSeconds(blinkInterval);

            if (!_isBlinking)
            {
                StartCoroutine(PerformBlink());
            }
        }
    }

    private IEnumerator PerformBlink()
    {
        _isBlinking = true;

        float leftGoal = _blendShapeGroups[0].Targets[0].TargetValue;
        float rightGoal = _blendShapeGroups[1].Targets[0].TargetValue;

        // Close eyes
        for (float t = 0; t < blinkDuration; t += Time.deltaTime)
        {
            float leftValue = Mathf.Lerp(0, leftGoal, t / blinkDuration);
            float rightValue = Mathf.Lerp(0, rightGoal, t / blinkDuration);
            SetBlinkValue(leftValue, rightValue);
            yield return null;
        }
        SetBlinkValue(leftGoal, rightGoal);

        // Open eyes
        for (float t = 0; t < blinkDuration; t += Time.deltaTime)
        {
            float leftValue = Mathf.Lerp(leftGoal, 0, t / blinkDuration);
            float rightValue = Mathf.Lerp(rightGoal, 0, t / blinkDuration);
            SetBlinkValue(leftValue, rightValue);
            yield return null;
        }
        SetBlinkValue(0, 0);

        _isBlinking = false;
    }

    private void SetBlinkValue(float leftValue, float rightValue)
    {
        if (_blendShapeGroups == null || FacialExpressionTarget == null) return;

        // Left Eye
        if (_blendShapeGroups[0].Targets.Count > 0)
        {
            FacialExpressionTarget.SetBlendShapeWeight(_blendShapeGroups[0].Targets[0].Index, leftValue);
        }

        // Right Eye
        if (_blendShapeGroups[1].Targets.Count > 0)
        {
            FacialExpressionTarget.SetBlendShapeWeight(_blendShapeGroups[1].Targets[0].Index, rightValue);
        }
    }
}