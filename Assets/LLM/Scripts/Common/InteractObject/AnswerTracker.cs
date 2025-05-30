using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzleGame
{
    public class AnswerTracker : MonoBehaviour
    {
        public string locationName;

        public AnswerEvaluationResult EvaluateAnswer()
        {
            List<GameObject> placedObjects = new();

            PlaceCollisionTracker[] trackers = GetComponentsInChildren<PlaceCollisionTracker>();
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
