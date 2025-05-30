using UnityEngine;

namespace BlockPuzzleGame
{
    public class GameManager : MonoBehaviour
    {
        public ConditionManager conditionManager;
        public PrefabSpawner grabbableBoxSpawner;
        public PrefabSpawner goalSpawner;
        public AnswerCheckerManager answerCheckerManager;

        [SerializeField]
        private Condition _condition;

        public Condition Condition
        {
            get => _condition;
            set
            {
                _condition = value;
                conditionManager?.SetMode(_condition);
            }
        }

        private void OnValidate()
        {
            if (conditionManager != null)
            {
                conditionManager.SetMode(_condition);
            }
        }

        public void CreateGrabbableBox()
        {
            grabbableBoxSpawner?.Spawn();
        }

        public void CreateGoal()
        {
            goalSpawner?.Spawn();
        }

        public void ShowTaskProgress()
        {
            if (answerCheckerManager == null)
            {
                Debug.LogWarning("AnswerCheckerManager is not assigned.");
                return;
            }

            var results = answerCheckerManager.GetAllResults();
            if (results.Count == 0)
            {
                Debug.Log("No answer trackers found.");
                return;
            }

            int correctCount = 0;
            foreach (var result in results)
            {
                if (result.isCorrect) correctCount++;
            }

            float percent = (float)correctCount / results.Count * 100f;
            Debug.Log($"✅ Task Progress: {correctCount}/{results.Count} ({percent:F1}%) complete.");
        }
    }

    public enum Condition
    {
        Baseline,
        UnidirecInput,
        UnidirecOutput,
        Bidirectional
    }
}
