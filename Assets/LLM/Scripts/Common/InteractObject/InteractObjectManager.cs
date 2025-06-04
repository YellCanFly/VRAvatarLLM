using System.Collections.Generic;
using UnityEngine;

public class InteractObjectManager : MonoBehaviour
{
    public static InteractObjectManager Instance { get; private set; }

    public List<InteractObject> allInteractObjects = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Log number of registered InteractObjects
        Debug.Log($"Number of registered InteractObjects: {allInteractObjects.Count}");
        //For Testing: Log all registered InteractObjects
        foreach (var obj in allInteractObjects)
        {
            Debug.Log($"Registered InteractObject: {obj.name}");
        }
    }

    /// <summary>
    /// 注册交互对象（在 InteractObject 的 OnEnable 中调用）
    /// </summary>
    public void Register(InteractObject obj)
    {
        // log object name
        Debug.Log($"Registering InteractObject: {obj.name}");
        if (!allInteractObjects.Contains(obj))
        {
            allInteractObjects.Add(obj);
        }
    }

    /// <summary>
    /// 注销交互对象（在 InteractObject 的 OnDisable 中调用）
    /// </summary>
    public void Unregister(InteractObject obj)
    {
        allInteractObjects.Remove(obj);
    }

    public IReadOnlyList<InteractObject> GetAllObjects()
    {
        return allInteractObjects.AsReadOnly();
    }

    /// <summary>
    /// 获取距离某个点最近的 InteractObject
    /// </summary>
    public InteractObject GetNearestObject(Vector3 position, float maxDistance = Mathf.Infinity)
    {
        InteractObject nearest = null;
        float minDist = maxDistance;

        foreach (var obj in allInteractObjects)
        {
            if (obj == null) continue;
            float dist = Vector3.Distance(position, obj.transform.position);
            if (dist < minDist)
            {
                nearest = obj;
                minDist = dist;
            }
        }

        return nearest;
    }

    public InteractObject GetObjectByName(string objectName)
    {
        foreach (var obj in allInteractObjects)
        {
            if (obj != null && obj.name == objectName)
            {
                return obj;
            }
        }

        Debug.LogWarning($"InteractObject with name '{objectName}' not found.");
        return null; // 没有找到
    }


    public InteractObject FindObjectContainingName(string keyword)
    {
        foreach (var obj in allInteractObjects)
        {
            if (obj != null && obj.name.ToLower().Contains(keyword.ToLower()))
            {
                return obj;
            }
        }

        return null;
    }

}
