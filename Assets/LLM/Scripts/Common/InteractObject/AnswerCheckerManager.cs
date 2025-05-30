using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzleGame
{
    public class AnswerCheckerManager : MonoBehaviour
    {
        public List<AnswerEvaluationResult> GetAllResults()
        {
            List<AnswerEvaluationResult> results = new();

            AnswerTracker[] trackers = FindObjectsByType<AnswerTracker>(FindObjectsSortMode.None);
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
    }
}
