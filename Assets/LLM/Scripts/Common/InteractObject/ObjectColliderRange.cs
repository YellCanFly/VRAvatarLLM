using Unity.XR.CoreUtils;
using UnityEngine;

public class ObjectColliderRange : MonoBehaviour
{
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
        // 获取当前物体上的 SphereCollider
        SphereCollider sphere = GetComponent<SphereCollider>();

        // 获取父物体中的所有 Renderer
        var renderers = transform.parent.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found on parent object.");
            return;
        }

        // 合并所有 Bounds
        bounds = renderers[0].bounds;
        foreach (var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        // 1. 设置 SphereCollider 居中
        sphere.center = transform.InverseTransformPoint(bounds.center);

        // 2. 统一使用 radius = 0.5（适配 scale）
        sphere.radius = 0.5f;

        // 3. 设置本地缩放以模拟非等比例球体（近似椭球）
        transform.localScale = bounds.size.Divide(transform.parent.localScale);

        //Debug.Log($"[FitSphereColliderWithScale] Center: {sphere.center}, Scale: {transform.localScale}, Radius: {sphere.radius}");
    }
}
