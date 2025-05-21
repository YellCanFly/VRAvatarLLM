using System.Collections.Generic;
using UnityEngine;

public class LipSync : FacialAnimation
{
    [Header("LipSync Parameters")]
    public OVRLipSyncContext OVRLipSyncContext = null;
    // data that will be queried and passed between phoneme and renderer each frame
    protected OVRLipSync.Frame _frame = new OVRLipSync.Frame();

    protected override void SetFaceAnimation()
    {
        _faceAnimationTarget = FaceAnimationType.LipSync;
    }

    protected override void Init()
    {
        base.Init();
    }

    void Update()
    {
        _frame = OVRLipSyncContext.GetCurrentPhonemeFrame();
        if(!_init || _frame == null || OVRLipSyncContext == null) return;

        List<BlendShapeGroup> _blendShapeGroups = BlendShapeMapperSO.GetGroups(_faceAnimationTarget);
        //For Each BlendShape
        //if there are too many blendshapes, this will be slow
        //TODO: Set a limit to the number of blendshapes?
        for(int i = 0; i < FacialExpressionTarget.sharedMesh.blendShapeCount; i++)
        {
            float _bsWeight = CalculateBlendShape(_blendShapeGroups, i);
            FacialExpressionTarget.SetBlendShapeWeight(i, _bsWeight);
        }           
    }

    private float CalculateBlendShape(List<BlendShapeGroup> blendShapeGroups, int blendShapeIndex)
    {
        float weight = 0.0f;
        for(int i = 0; i < blendShapeGroups.Count; i++){
            //blendShapeGroups: sil, PP, FF, TH, DD, kk, CH, SS, nn, RR, aa, E, ih, oh, ou(15 phonemes)
            for(int j = 0; j < blendShapeGroups[i].Targets.Count; j++){
                //Targets are defined in the BlendShapeMapperSO
                //e.g. CCAvatar Case:CH's target's indexes are 3, 114, 117, SS's target's indexes are 5, 7...
                //TODO: Create a dictionary in the BlendShapeMapperSO to store the indexes of each phoneme?
                if(blendShapeGroups[i].Targets[j].Index == blendShapeIndex){
                    weight += _frame.Visemes[i] * blendShapeGroups[i].Targets[j].TargetValue;
                }
            }
        }
        return weight;
    }
}