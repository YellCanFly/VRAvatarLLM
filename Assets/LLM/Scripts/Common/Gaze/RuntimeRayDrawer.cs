using UnityEngine;

public class RuntimeRayDrawer : MonoBehaviour
{
    public Vector3 startPoint = Vector3.zero;
    public Vector3 endPoint = new Vector3(0, 0, 10);
    public float lineWidth = 0.1f;
    public Material lineMaterial;

    private LineRenderer lineRenderer;

    void Start()
    {
        // 创建空对象
        GameObject lineObj = new GameObject("RuntimeRay");
        lineRenderer = lineObj.AddComponent<LineRenderer>();

        // 设置材质
        lineRenderer.material = lineMaterial ?? new Material(Shader.Find("Sprites/Default"));

        // 设置顶点数量
        lineRenderer.positionCount = 2;

        // 设置线宽（起始 / 结束）
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // 设置世界坐标
        lineRenderer.useWorldSpace = true;

        // 可选：设置颜色
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;

        // 设置顶点位置
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    void Update()
    {
        // 若射线动态变化，更新位置
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }
}
