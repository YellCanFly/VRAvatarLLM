using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GazeSphereDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public GameObject gazeEye;
    public float sphereRadius = 0.025f;
    public float maxDistance = 10f;
    public LayerMask detectionLayer;
    private float detectionDistance;
    private Vector3 gazeDirection;

    private GameObject lastGazedObject = null;
    private Queue<string> gazeObjectQueue = new();
    private List<string> allObjectInEyeFieldList = new();
    public int maxGazeMemory = 5;

    [Header("Debug")]
    public TMPro.TextMeshProUGUI gazeObjectName;
    public bool showDebugGizmo = true;
    public Color debugColor = Color.cyan;

    private void Start()
    {
        gazeObjectQueue.Clear();
    }

    void FixedUpdate()
    {
        // 从当前相机向前方进行球形检测
        Vector3 origin = Camera.main.transform.position;
        gazeDirection = Camera.main.transform.forward;
        if (gazeEye != null)
        {
            gazeDirection = gazeEye.transform.forward;
        }
        detectionDistance = maxDistance;

        bool hasDetectedInteractObject = false;
        if (Physics.SphereCast(origin, sphereRadius, gazeDirection, out RaycastHit hit, maxDistance, detectionLayer))
        {
            detectionDistance = hit.distance;
            
            if (CheckHitObject(hit, out InteractObject interactObj))
            {
                // Update UI
                if (gazeObjectName != null)
                {
                    gazeObjectName.text = "Gaze object: " + interactObj.name;
                }

                // Update the last gazed object and add to the queue
                if (hit.collider.gameObject != lastGazedObject)
                {
                    lastGazedObject = hit.collider.gameObject;
                    AddGazeObject(interactObj.name);
                }

                // Highlight the object if it is not already gazed at
                if (!interactObj.IsGazed)
                {
                    interactObj.ChangeColorForDuration(interactObj.gazeHightlightColor, 0.1f);
                    Debug.Log("Detected InteractObject: " + interactObj.gameObject.name);
                }
                hasDetectedInteractObject = true;
            }
        }
        if (!hasDetectedInteractObject)
        {
            GazeNothing();
        }

        var RangeHitResults = Physics.BoxCastAll(origin, new Vector3(10.0f, 10.0f, 0.05f), gazeDirection);
        allObjectInEyeFieldList.Clear();
        foreach (var hitResult in RangeHitResults)
        {
            if (CheckHitObject(hitResult, out InteractObject interactObj))
            {
                allObjectInEyeFieldList.Add(interactObj.name);
            }
        }
         //Debug.Log("All objects in eye field: " + string.Join(", ", allObjectInEyeFieldList));

    }

    private void OnDrawGizmos()
    {
        if (showDebugGizmo)
        {
            Gizmos.color = debugColor;
            Gizmos.DrawWireSphere(Camera.main.transform.position + gazeDirection * detectionDistance, sphereRadius);
            Gizmos.DrawLine(Camera.main.transform.position, Camera.main.transform.position + gazeDirection * detectionDistance);
        }
    }

    public void AddGazeObject(string objectName)
    {
        // 如果超出限制，移除最旧的元素
        if (gazeObjectQueue.Count >= maxGazeMemory)
        {
            gazeObjectQueue.Dequeue();
        }

        gazeObjectQueue.Enqueue(objectName);
    }

    public string GetLatestGazeObject()
    {
        if (gazeObjectQueue.Count == 0) return null;

        return gazeObjectQueue.Last(); // 需要 using System.Linq;
    }

    public List<string> GetGazeObjectList()
    {
        return gazeObjectQueue.ToList();
    }

    public List<string> GetAllObjectInEyeFieldList()
    {
        return allObjectInEyeFieldList;
    }

    private void GazeNothing()
    {
        if (gazeObjectName != null)
        {
            gazeObjectName.text = "Gaze object: None";
        }
        lastGazedObject = null;
    }

    private bool CheckHitObject(RaycastHit hit, out InteractObject interactObj)
    {
        if (hit.collider.GetComponent<ObjectColliderRange>() != null && hit.collider.transform.parent != null)
        {
            var interactable = hit.collider.transform.parent.GetComponent<InteractObject>();
            if (interactable != null)
            {
                interactObj = interactable;
                return true;
            }
        }
        interactObj = null;
        return false;
    }

}
