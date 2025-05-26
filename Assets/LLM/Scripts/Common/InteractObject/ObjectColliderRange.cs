using Unity.XR.CoreUtils;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(CapsuleCollider))]
public class ObjectColliderRange : MonoBehaviour
{
    public DetectorType detectorType = DetectorType.Capsule;
    public Bounds bounds;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitColliderSize();
    }

    private void OnValidate()
    {
        InitColliderSize();
    }

    private void InitColliderSize()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        switch (detectorType)
        {
            case DetectorType.Sphere:
                sphere.enabled = true;
                capsule.enabled = false;
                break;
            case DetectorType.Capsule:
                sphere.enabled = false;
                capsule.enabled = true;
                break;
        }

        // Get all Renderers
        var renderers = transform.parent.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found on parent object.");
            return;
        }

        // Combine Bounds
        bounds = renderers[0].bounds;
        foreach (var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        // 1. Set collider to object center
        sphere.center = transform.InverseTransformPoint(bounds.center);
        capsule.center = transform.InverseTransformPoint(bounds.center);

        // 2. Set collider to uniform size
        sphere.radius = 0.5f;
        capsule.radius = 0.5f;
        capsule.height = 1.0f;

        // 3. Set local scale
        transform.localScale = bounds.size.Divide(transform.parent.localScale);

        //Debug.Log($"[FitSphereColliderWithScale] Center: {sphere.center}, Scale: {transform.localScale}, Radius: {sphere.radius}");
    }

    public enum DetectorType
    {
        Sphere,
        Capsule
    }
}
