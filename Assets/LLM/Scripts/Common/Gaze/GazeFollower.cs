using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeFollower : MonoBehaviour
{
    public GameObject eyeObject;
    public float distance = 5f;
    public float defaultDistance = 2f;
    public float maxRayDistance = 20f;
    public LayerMask raycastLayerMask = Physics.DefaultRaycastLayers;

    // Update is called once per frame
    void Update()
    {
        if (eyeObject != null)
        {
            Vector3 origin = eyeObject.transform.position;
            Vector3 direction = eyeObject.transform.forward;

            // 射线检测
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, raycastLayerMask))
            {
                distance = hit.distance;
                Debug.DrawLine(origin, hit.point, Color.red);
            }
            else
            {
                distance = defaultDistance;
                Debug.DrawLine(origin, origin + direction * maxRayDistance, Color.green);
            }

            // 更新位置
            transform.position = origin + direction * distance;
        }
    }
}
