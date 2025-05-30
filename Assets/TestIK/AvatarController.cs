using UnityEngine;
using RootMotion.FinalIK;
using System.Collections;

public class AvatarController : MonoBehaviour
{
    public Transform LookAtTarget;

    private Animator Animator;
    private int StartPointingHash = Animator.StringToHash("StartPointing");
    private int StopPointingHash = Animator.StringToHash("StopPointing");

    private LookAtController LookAtController;
    private LookAtIK LookAtIK;
    private AimIK AimIK;

    private float LookAtIKBodyWeight;

    private int LeftHash = Animator.StringToHash("Left");
    private int RightHash = Animator.StringToHash("Right");

    private void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
        LookAtIK = GetComponentInChildren<LookAtIK>();
        AimIK = GetComponentInChildren<AimIK>();
        AimIK.solver.target = LookAtTarget;

        LookAtIKBodyWeight = LookAtIK.solver.bodyWeight;
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
        const float lookAtRemoveTime = 0.2f;
        float t = 0.0f;
        while (t < lookAtRemoveTime)
        {
            t += Time.deltaTime;
            LookAtIK.solver.IKPositionWeight = Mathf.Lerp(LookAtIK.solver.IKPositionWeight, 0.0f, t / lookAtRemoveTime);
            yield return null;
        }
        // Rotate towards target if needed --------------------------------------------------------------------------------
        while (true)
        {
            Vector3 bodyDir = AimIK.transform.forward;
            bodyDir.y = 0.0f;
            bodyDir.Normalize();
            Vector3 targetDir = pointAt - AimIK.transform.position;
            targetDir.y = 0.0f; 
            targetDir.Normalize();

            Debug.DrawRay(AimIK.transform.position, targetDir, Color.blue);
            Debug.DrawRay(AimIK.transform.position, bodyDir, Color.red);

            float dot = Vector3.Dot(bodyDir, targetDir);
            bool isRight = Vector3.Cross(bodyDir, targetDir).y < 0.0f;

            if (dot < 0.5f)
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

            //const float rotationAdjustmentSpeed = 0.1f;
            //Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);
            //AimIK.transform.rotation = Quaternion.Slerp(AimIK.transform.rotation, targetRotation, Time.deltaTime * rotationAdjustmentSpeed);
            yield return null;
        }
        // Start pointing --------------------------------------------------------------------------------------------------------------------------------
        LookAtTarget.transform.position = pointAt;
        Animator.SetTrigger(StartPointingHash);
        yield return new WaitForSeconds(0.5f); // Wait for the animation to start
        const float pointingIKTime = 1.0f;
        t = 0.0f;
        while (t < pointingIKTime)
        {
            t += Time.deltaTime;
            AimIK.solver.IKPositionWeight = Mathf.Lerp(0.0f, 1.0f, t / pointingIKTime);
            yield return null;
        }
    }

    private IEnumerator StopPointingIK()
    {
        const float pointingIKTime = 0.5f;
        float t = 0.0f;
        bool triggered = false;
        while (t < pointingIKTime)
        {
            t += Time.deltaTime;
            AimIK.solver.IKPositionWeight = Mathf.Lerp(1.0f, 0.0f, t / pointingIKTime);
            if (t > pointingIKTime * 0.3f)
            {
                Animator.SetTrigger(StopPointingHash);
                triggered = true;
            }
            yield return null;
        }
        if (triggered)
        {
            Animator.SetTrigger(StopPointingHash);
            triggered = true;
        }
        //const float lookAtIKTime = 0.5f;
        //t = 0.0f;
        //while (t < lookAtIKTime)
        //{
        //    t += Time.deltaTime;
        //    LookAtIK.solver.bodyWeight = Mathf.Lerp(LookAtIK.solver.bodyWeight, LookAtIKBodyWeight, t / lookAtIKTime);
        //    yield return null;
        //}
    }
}
