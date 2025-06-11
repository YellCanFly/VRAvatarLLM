using UnityEngine;
using UnityEngine.Events;

public class Tutorial_GrabItemCheck : MonoBehaviour
{
    public GameObject GrabItem;
    public GameObject PlaceTarget;
    public float finishDistance = 0.25f; // Distance to consider the task finished

    public UnityAction OnTaskFinished;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        //float distance = Vector3.Distance(GrabItem.transform.position, PlaceTarget.transform.position);
        //if (distance < finishDistance && !isFinished)
        //{
        //    isFinished = true;
        //    Debug.Log("Task Finished: Item placed correctly!");
        //    OnTaskFinished?.Invoke();
        //}
       
    }

    public bool CheckIsFinished()
    {
        float distance = Vector3.Distance(GrabItem.transform.position, PlaceTarget.transform.position);
        return distance < finishDistance;
    }
}
