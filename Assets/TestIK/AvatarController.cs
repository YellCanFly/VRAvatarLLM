using UnityEngine;
using RootMotion.FinalIK;
using System.Collections;

public class AvatarController : MonoBehaviour
{
    public Transform LookAtTarget;
    public Transform[] WalkTransforms;
    public float MaxDistanceBeforeWalk = 2.0f;

    private Animator Animator;
    private int StartPointingHash = Animator.StringToHash("StartPointing");
    private int StopPointingHash = Animator.StringToHash("StopPointing");
    private int ThinkHask = Animator.StringToHash("Think");

    private LookAtIK LookAtIK;
    private AimIK AimIK;

    private int LeftHash = Animator.StringToHash("Left");
    private int RightHash = Animator.StringToHash("Right");
    private int WalkHash = Animator.StringToHash("Walk");

    private Vector3 InitialTargetLook;

    private void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
        LookAtIK = GetComponentInChildren<LookAtIK>();
        AimIK = GetComponentInChildren<AimIK>();
        AimIK.solver.target = LookAtTarget;

        InitialTargetLook = AimIK.transform.position + AimIK.transform.forward * 3.0f;
    }

    [ContextMenu("Trigger Thinking Animation")]
    public void TriggerThinkingAnimation()
    {
        Animator.SetTrigger(ThinkHask);
    }

    [ContextMenu("StartPointing")]
    public void StartPointing(Vector3 pointAt)
    {
        StartCoroutine(StartPointingIK(pointAt));
    }

    [ContextMenu("StopPointing")]
    public void StopPointing()
    {
        StartCoroutine(StopPointingIK());
    }

    private IEnumerator StartPointingIK(Vector3 pointAt)
    {
        Animator.ResetTrigger(StopPointingHash);

        // Disable LookAt to avoid conflicts with pointing ----------------------------------------------------------------
        const float lookAtAndAnimTime = 0.3f;
        float tLookAt = 0.0f;

        if (WalkTransforms != null && WalkTransforms.Length > 0 && Vector3.Distance(pointAt, AimIK.transform.position) > MaxDistanceBeforeWalk)
        {
            Transform walkTarget = WalkTransforms[0];
            float minDistance = float.MaxValue;
            for (int i = 0; i < WalkTransforms.Length; i++)
            {
                float dist = Vector3.Distance(WalkTransforms[i].position, pointAt);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    walkTarget = WalkTransforms[i];
                }
            }
            // Rotate towards walk target --------------------------------------------------------------------------------
            const float rotationAdjustmentSpeed = 2.5f;
            Vector3 bodyDir = AimIK.transform.forward;
            bodyDir.y = 0.0f;
            bodyDir.Normalize();
            Vector3 targetDir = walkTarget.position - AimIK.transform.position;
            targetDir.y = 0.0f;
            targetDir.Normalize();

            float dot = Vector3.Dot(bodyDir, targetDir);
            bool isRight = Vector3.Cross(bodyDir, targetDir).y > 0.0f;
            float prevDot = dot;
            while (true)
            {
                bodyDir = AimIK.transform.forward;
                bodyDir.y = 0.0f;
                bodyDir.Normalize();
                dot = Vector3.Dot(bodyDir, targetDir);

                if (dot < 0.8f && prevDot - Mathf.Abs(dot) * 0.1f <= dot)
                {
                    if (isRight)
                    {
                        Animator.SetBool(RightHash, true);
                    }
                    else
                    {
                        Animator.SetBool(LeftHash, true);
                    }
                }
                else
                {
                    Animator.SetBool(LeftHash, false);
                    Animator.SetBool(RightHash, false);
                    break;
                }

                prevDot = dot;

                // Look At
                tLookAt += Time.deltaTime;
                LookAtIK.solver.IKPositionWeight = Mathf.Lerp(LookAtIK.solver.IKPositionWeight, 0.0f, Mathf.Clamp01(tLookAt / lookAtAndAnimTime));

                Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);
                AimIK.transform.rotation = Quaternion.Slerp(AimIK.transform.rotation, targetRotation, Time.deltaTime * rotationAdjustmentSpeed);
                yield return null;
            }
            // Walk towards walk target --------------------------------------------------------------------------------
            const float walkThreshold = 0.5f;
            float walkDistance = Vector3.Distance(walkTarget.position, AimIK.transform.position);
            while (walkDistance > walkThreshold)
            {
                Animator.SetBool(WalkHash, true);
                walkDistance = Vector3.Distance(walkTarget.position, AimIK.transform.position);

                targetDir = walkTarget.position - AimIK.transform.position;
                targetDir.y = 0.0f;
                targetDir.Normalize();
                Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);
                AimIK.transform.rotation = Quaternion.Slerp(AimIK.transform.rotation, targetRotation, Time.deltaTime * rotationAdjustmentSpeed);

                // Look At
                tLookAt += Time.deltaTime;
                LookAtIK.solver.IKPositionWeight = Mathf.Lerp(LookAtIK.solver.IKPositionWeight, 0.0f, Mathf.Clamp01(tLookAt / lookAtAndAnimTime));

                yield return null;
            }
            Animator.SetBool(WalkHash, false);
        }

        // Rotate towards target if needed --------------------------------------------------------------------------------
        {
            Vector3 bodyDir = AimIK.transform.forward;
            bodyDir.y = 0.0f;
            bodyDir.Normalize();
            Vector3 targetDir = pointAt - AimIK.transform.position;
            targetDir.y = 0.0f;
            targetDir.Normalize();

            float dot = Vector3.Dot(bodyDir, targetDir);
            bool isRight = Vector3.Cross(bodyDir, targetDir).y > 0.0f;
            float prevDot = dot;
            while (true)
            {
                bodyDir = AimIK.transform.forward;
                bodyDir.y = 0.0f;
                bodyDir.Normalize();
                dot = Vector3.Dot(bodyDir, targetDir);

                if (dot < 0.8f && prevDot - Mathf.Abs(dot) * 0.1f <= dot)
                {
                    if (isRight)
                    {
                        Animator.SetBool(RightHash, true);
                    }
                    else
                    {
                        Animator.SetBool(LeftHash, true);
                    }
                }
                else
                {
                    Animator.SetBool(LeftHash, false);
                    Animator.SetBool(RightHash, false);
                    break;
                }

                prevDot = dot;

                // Look At
                tLookAt += Time.deltaTime;
                LookAtIK.solver.IKPositionWeight = Mathf.Lerp(LookAtIK.solver.IKPositionWeight, 0.0f, Mathf.Clamp01(tLookAt / lookAtAndAnimTime));

                //const float rotationAdjustmentSpeed = 0.1f;
                //Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);
                //AimIK.transform.rotation = Quaternion.Slerp(AimIK.transform.rotation, targetRotation, Time.deltaTime * rotationAdjustmentSpeed);
                yield return null;
            }
        }

        // Start pointing --------------------------------------------------------------------------------------------------------------------------------
        LookAtTarget.transform.position = pointAt;
        Animator.SetTrigger(StartPointingHash);
        float tAnim = 0.0f;
        while (tAnim < lookAtAndAnimTime)
        {
            tAnim += Time.deltaTime;
            tLookAt += Time.deltaTime;
            LookAtIK.solver.IKPositionWeight = Mathf.Lerp(LookAtIK.solver.IKPositionWeight, 0.0f, Mathf.Clamp01(tLookAt / lookAtAndAnimTime));
            yield return null; // Wait for the animation to start
        }
        const float pointingIKTime = 1.0f;
        float t = 0.0f;
        while (t < pointingIKTime)
        {
            t += Time.deltaTime;
            AimIK.solver.IKPositionWeight = Mathf.Lerp(0.0f, 1.0f, Mathf.Pow(t / pointingIKTime, 3.0f));
            yield return null;
        }
    }

    private IEnumerator StopPointingIK()
    {
        // Stop Pointing Animation ----------------------------------------------------------------
        const float pointingIKTime = 0.5f;
        float t = 0.0f;
        bool triggered = false;
        while (t < pointingIKTime)
        {
            t += Time.deltaTime;
            AimIK.solver.IKPositionWeight = Mathf.Lerp(1.0f, 0.0f, t / pointingIKTime);
            if (t > pointingIKTime * 0.1f)
            {
                Animator.SetTrigger(StopPointingHash);
                triggered = true;
            }
            yield return null;
        }
        if (!triggered)
        {
            Animator.SetTrigger(StopPointingHash);
            triggered = true;
        }

        // Rotate towards the user ----------------------------------------------------------------
        Vector3 bodyDir = AimIK.transform.forward;
        bodyDir.y = 0.0f;
        bodyDir.Normalize();
        Vector3 targetDir = InitialTargetLook - AimIK.transform.position;
        targetDir.y = 0.0f;
        targetDir.Normalize();

        float dot = Vector3.Dot(bodyDir, targetDir);
        bool isRight = Vector3.Cross(bodyDir, targetDir).y > 0.0f;
        float prevDot = dot;
        while (true)
        {
            bodyDir = AimIK.transform.forward;
            bodyDir.y = 0.0f;
            bodyDir.Normalize();
            dot = Vector3.Dot(bodyDir, targetDir);

            if (dot < 0.8f && prevDot - Mathf.Abs(dot) * 0.1f <= dot)
            {
                if (isRight)
                {
                    Animator.SetBool(RightHash, true);
                }
                else
                {
                    Animator.SetBool(LeftHash, true);
                }
            }
            else
            {
                Animator.SetBool(LeftHash, false);
                Animator.SetBool(RightHash, false);
                break;
            }

            prevDot = dot;

            //const float rotationAdjustmentSpeed = 0.1f;
            //Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);
            //AimIK.transform.rotation = Quaternion.Slerp(AimIK.transform.rotation, targetRotation, Time.deltaTime * rotationAdjustmentSpeed);
            yield return null;
        }

        // Look at ON again ----------------------------------------------------------------
        const float lookAtIKTime = 0.5f;
        t = 0.0f;
        while (t < lookAtIKTime)
        {
            t += Time.deltaTime;
            LookAtIK.solver.IKPositionWeight = Mathf.Lerp(LookAtIK.solver.IKPositionWeight, 1.0f, t / lookAtIKTime);
            yield return null;
        }
    }
}
