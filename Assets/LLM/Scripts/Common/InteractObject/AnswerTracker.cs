using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzleGame
{
    public class AnswerTracker : MonoBehaviour
    {
        public string locationName;

        private PlaceCollisionTracker[] trackers;

        private void Awake()
        {
            trackers = GetComponentsInChildren<PlaceCollisionTracker>();
            foreach (PlaceCollisionTracker tracker in trackers)
            {
                tracker.onObjectPlaced += OnObjectPlacedInTrack;
                tracker.onObjectRemoved += OnObjectRemoved;
            }
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
            if (obj.name == locationName)
            {
                GameManager.Instance.OnCorrectObjectPlaced();
            }
            else
            {
                GameManager.Instance.OnWrongObjectPlaced();
            }
            GameManager.Instance.OnObjectPlaced();
        }

        public void OnObjectRemoved(GameObject obj)
        {
            GameManager.Instance.OnObjectRemoved();
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
