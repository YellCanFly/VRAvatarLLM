using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzleGame
{
    public class AnswerCheckerManager : MonoBehaviour
    {
        public AnswerTracker[] trackers;

        public void UpdateAnswerTracker()
        {
            trackers = FindObjectsByType<AnswerTracker>(FindObjectsSortMode.None);
        }

        public List<AnswerEvaluationResult> GetAllResults()
        {
            List<AnswerEvaluationResult> results = new();

            UpdateAnswerTracker();
            foreach (var tracker in trackers)
            {
                results.Add(tracker.EvaluateAnswer());
            }

            return results;
        }

        [ContextMenu("Evaluate and Log Results")]
        public void LogAllResults()
        {
            List<AnswerEvaluationResult> results = GetAllResults();
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];

                string placedNames = result.placedObjects != null && result.placedObjects.Count > 0
                    ? string.Join(", ", result.placedObjects.ConvertAll(obj => obj != null ? obj.name : "null"))
                    : "None";

                Debug.Log($"[{i}] Expected: {result.locationName}, Placed: {placedNames}, Correct: {result.isCorrect}");
            }
        }

        public List<BlockPuzzleData_TargetPlaceInfo> GetAllTrackerData()
        {
            UpdateAnswerTracker();
            var trackerData = new List<BlockPuzzleData_TargetPlaceInfo>();
            foreach (var tracker in trackers)
            {
                var data = new BlockPuzzleData_TargetPlaceInfo
                {
                    placeName = tracker.locationName,
                    targetPosition = new SerializableVector3(tracker.transform.position),
                };
                trackerData.Add(data);
            }
            return trackerData;
        }
    }
}
