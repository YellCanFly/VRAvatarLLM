using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeFollower : MonoBehaviour
{
    public GameObject eyeObject;
    public float distance = 5f;

    // Update is called once per frame
    void Update()
    {
        if (eyeObject != null)
        {
            transform.position = eyeObject.transform.position + eyeObject.transform.forward * distance;
        }

        
    }
}
