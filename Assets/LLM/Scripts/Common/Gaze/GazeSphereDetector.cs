using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Utilities.Extensions;

public class GazeSphereDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public GameObject gazeEye;
    public float sphereRadius = 0.025f;
    public float maxDistance = 10f;
    public LayerMask detectionLayer;
    private float detectionDistance;
    public Vector3 gazeDirection;
    public float gazeDurationThreshold = 0.5f; // Duration to consider an object as gazed at

    private InteractObject lastGazedObject = null;
    private Queue<string> gazeObjectQueue = new();
    private List<string> allObjectInEyeFieldList = new();
    public int maxGazeMemory = 5;

    public bool showGazeResult = false;
    public GazeFollower gazeFollower;

    [Header("Debug")]
    public TMPro.TextMeshProUGUI gazeObjectName;
    public bool showDebugGizmo = true;
    public Color debugColor = Color.cyan;

    private void Start()
    {
        //gazeFollower.gameObject.SetActive(showGazeResult);

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

                interactObj.beGazedDuration += Time.fixedDeltaTime;
                if (interactObj.beGazedDuration >= gazeDurationThreshold)
                {
                    if (GetLatestGazeObject() != interactObj.objectName)
                    {
                        AddGazeObject(interactObj.objectName);
                        Debug.Log("Detected InteractObject: " + interactObj.objectName);
                    }

                    if (showGazeResult)
                    {
                        interactObj.SetObjectToHeightLightColor();
                    }
                }

                // Update the last gazed object and add to the queue
                if (interactObj != lastGazedObject)
                {
                    if (lastGazedObject != null)
                    {
                        // Reset the last gazed object's duration
                        lastGazedObject.beGazedDuration = 0f;
                        lastGazedObject.ResetObjectToDefaultColor();
                    }
                    lastGazedObject = interactObj;
                }

                hasDetectedInteractObject = true;
                gazeFollower.SetActive(showGazeResult);
                gazeFollower.transform.position = origin + gazeDirection * detectionDistance;
            }
        }
        
        if (!hasDetectedInteractObject)
        {
            GazeNothing();
            gazeFollower.SetActive(false);
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
        if (lastGazedObject != null)
        {
            lastGazedObject.beGazedDuration = 0f; // Reset the duration if no object is gazed at
            lastGazedObject.ResetObjectToDefaultColor(); // Reset color to default
            lastGazedObject = null; // Clear the last gazed object
        }
    }

    private bool CheckHitObject(RaycastHit hit, out InteractObject interactObj)
    {
        if (hit.collider.GetComponent<ObjectColliderRange>() != null && hit.collider.transform.parent != null)
        {
            var interactable = hit.collider.GetComponentInParent<InteractObject>();
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
