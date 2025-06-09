using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzleGame
{
    public class AnswerTracker : MonoBehaviour
    {
        public string locationName;

        private bool readyToCheck = false;
        private GameObject placedObj;
        private PlaceCollisionTracker[] trackers;

        private void Start()
        {
            trackers = GetComponentsInChildren<PlaceCollisionTracker>();
            foreach (PlaceCollisionTracker tracker in trackers)
            {
                tracker.onObjectPlaced += OnObjectPlacedInTrack;
                tracker.onObjectRemoved += OnObjectRemoved;
            }
            GrabInteractObjectManager.Instance.onDropedObject += OnUserDropedObject;
        }

        public AnswerEvaluationResult EvaluateAnswer()
        {
            List<GameObject> placedObjects = new();
            foreach (var tracker in trackers)
            {
                foreach (GameObject obj in tracker.objectsOnPlane)
                {
                    if (obj != null && !placedObjects.Contains(obj))
                    {
                        placedObjects.Add(obj);
                    }
                }
            }

            bool isCorrect = placedObjects.Exists(obj => obj.name == locationName);
            return new AnswerEvaluationResult(isCorrect, locationName, placedObjects);
        }

        private void OnObjectPlacedInTrack(GameObject obj)
        {
            readyToCheck = GrabInteractObjectManager.Instance.CurrentHeldObject != null;
            placedObj = obj;
        }

        public void OnObjectRemoved(GameObject obj)
        {
            readyToCheck = false;
            GameManager.Instance.OnObjectRemoved();
        }

        private void OnUserDropedObject()
        {
            if (readyToCheck && placedObj != null)
            {
                bool isCorrect = placedObj.name == locationName;
                string objectName = placedObj.name;
                string placeName = locationName;
                GameManager.Instance.OnObjectPlaced(isCorrect, objectName, placeName);
            }
            readyToCheck = false;
        }
    }

    public class AnswerEvaluationResult
    {
        public bool isCorrect;
        public string locationName;
        public List<GameObject> placedObjects;

        public AnswerEvaluationResult(bool isCorrect, string locationName, List<GameObject> placedObjects)
        {
            this.isCorrect = isCorrect;
            this.locationName = locationName;
            this.placedObjects = placedObjects;
        }
    }
}
