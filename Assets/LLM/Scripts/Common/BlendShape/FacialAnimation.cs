using UnityEngine;
public abstract class FacialAnimation : MonoBehaviour
{
    public BlendShapeMapperSO BlendShapeMapperSO;

    public SkinnedMeshRenderer FacialExpressionTarget = null;

    protected FaceAnimationType _faceAnimationTarget;

    protected bool _init = false;

    protected virtual void Awake()
    {
        Init();
    }

    protected virtual void Init(){
        SetFaceAnimation();
        AssignFacialExpressionTarget();
        _init = true;
    }

    protected abstract void SetFaceAnimation();


    /// <summary>
    /// Assigns a SkinnedMeshRenderer with blend shapes to the LipSync's facial expression target.
    /// </summary>
    protected void AssignFacialExpressionTarget()
    {

        if (FacialExpressionTarget == null)
        {
            FacialExpressionTarget = FindSuitableSkinnedMeshRenderer();
            
            if (FacialExpressionTarget != null)
            {
                Debug.Log("Facial expression target successfully assigned.");
            }
            else
            {
                Debug.LogWarning("No suitable SkinnedMeshRenderer with blend shapes found among siblings.");
            }
        }
    }

    /// <summary>
    /// Searches for a SkinnedMeshRenderer with blend shapes in the same hierarchy level.
    /// </summary>
    /// <returns>The first matching SkinnedMeshRenderer, or null if none is found.</returns>
    protected virtual SkinnedMeshRenderer FindSuitableSkinnedMeshRenderer()
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("Parent transform is null. Cannot search for SkinnedMeshRenderer.");
            return null;
        }

        foreach (Transform sibling in transform.parent)
        {
            if (sibling == transform) continue; // Skip self

            var renderer = sibling.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null && renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
            {
                return renderer; // Found a valid SkinnedMeshRenderer
            }
        }

        return null; // No suitable SkinnedMeshRenderer found
    }
}