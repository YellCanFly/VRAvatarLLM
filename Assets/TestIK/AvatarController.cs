using UnityEngine;
using RootMotion.FinalIK;
using System.Collections;

public class AvatarController : MonoBehaviour
{
    public float StartPointingIKDelay = 1.75f;
    public float StopPointingIKDelay = 0.5f;
    public Transform LookAtTarget;
    public Transform AimTarget;

    private Animator Animator;
    private int StartPointingHash = Animator.StringToHash("StartPointing");
    private int StopPointingHash = Animator.StringToHash("StopPointing");

    private LookAtController LookAtController;
    private AimController AimController;
    private LookAtIK LookAtIK;
    private AimIK AimIK;

    private float LookAtIKBodyWeight;

    private void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
        LookAtController = GetComponentInChildren<LookAtController>();
        AimController = GetComponentInChildren<AimController>();
        LookAtIK = GetComponentInChildren<LookAtIK>();
        AimIK = GetComponentInChildren<AimIK>();

        AimController.target = null;
        LookAtController.target = LookAtTarget;
        LookAtIKBodyWeight = LookAtIK.solver.bodyWeight;
    }

    [ContextMenu("StartPointing")]
    public void StartPointing()
    {
        Animator.SetTrigger(StartPointingHash);
        LookAtIK.solver.bodyWeight = 0.0f;
        StartCoroutine(StartPointingIK());
    }

    [ContextMenu("StopPointing")]
    public void StopPointing()
    {
        Animator.SetTrigger(StopPointingHash);
        LookAtIK.solver.bodyWeight = LookAtIKBodyWeight;
        StartCoroutine(StopPointingIK());
    }

    [ContextMenu("EnableLookAt")]
    public void EnableLookAt()
    {
        LookAtController.target = LookAtTarget;
    }

    [ContextMenu("DisableLookAt")]
    public void DisableLookAt()
    {
        LookAtController.target = null;
    }

    private IEnumerator StartPointingIK()
    {
        yield return new WaitForSeconds(StartPointingIKDelay);
        AimController.target = AimTarget;
    }

    private IEnumerator StopPointingIK()
    {
        yield return new WaitForSeconds(StopPointingIKDelay);
        AimController.target = null;
    }
}
