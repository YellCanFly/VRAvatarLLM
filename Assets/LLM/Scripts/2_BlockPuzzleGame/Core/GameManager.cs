using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BlockPuzzleGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Condition Settings")]
        public ConditionManager conditionManager;
        public InteractCondition condition = InteractCondition.Baseline;
        public List<InteractCondition> conditionOrders;
        public int currentExperimentIndex = 0;

        public UnityAction onOneConditionStarted;
        public UnityAction onOneConditionFinished;
        public UnityAction onAllConditionsFinished;

        [Header("Prefab Spawners")]
        public PrefabSpawner grabbableBoxSpawner;
        public PrefabSpawner goalSpawner;

        [Header("Answer Checkers")]
        public AnswerCheckerManager answerCheckerManager;

        [Header("UI Settings")]
        public GameObject canvas_1_TaskGuidance;
        public GameObject canvas_2_OneConditionCompleted;
        public GameObject canvas_3_StartNewCondition;
        public GameObject canvas_4_AllConditionsCompleted;

        private Button button_1_TaskGuidance;
        private Button button_2_OneConditionCompleted;
        private Button button_3_StartNewCondition;
        private Button button_4_AllConditionsCompleted;


        private float checkInterval = 0.5f; // Interval to check task progress
        private float checkTimer = 0f;
        private bool currentConditionComplted = false;
        private float currentProgress = 0f;


        private void OnValidate()
        {
            if (conditionManager != null)
            {
                conditionManager.SetMode(condition);
            }
        }

        private void Awake()
        {
            // Ensure this is a singleton instance
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Initialize
            conditionOrders = ExperimentManager.GetConditionOrder(ExperimentManager.Instance.participantID);
            InitEventBinds();
            InitCanvas();
        }

        private void Start()
        {
            // Start the first experiment round
            currentExperimentIndex = 0;
            StartExperimentRound(currentExperimentIndex);
        }

        private void Update()
        {
            // Check if the task progress should be updated
            if (checkTimer >= checkInterval)
            {
                if (CheckOneConditionFinished(out float checkProgress) && !currentConditionComplted)
                {
                    onOneConditionFinished?.Invoke(); // Trigger the event for one condition started
                }
                if (checkProgress > currentProgress)
                {
                    Debug.Log($"Task progress updated: {checkProgress * 100:F1}% complete.");
                }
                currentProgress = checkProgress;
                checkTimer = 0f; // Reset the timer
            }
            else
            {
                checkTimer += Time.deltaTime; // Increment the timer
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

        public bool CheckOneConditionFinished(out float progress)
        {
            if (answerCheckerManager == null)
            {
                Debug.LogWarning("AnswerCheckerManager is not assigned.");
                progress = 0f;
                return false;
            }

            var results = answerCheckerManager.GetAllResults();
            if (results.Count == 0)
            {
                Debug.Log("No answer trackers found.");
                progress = 0f;
                return false;
            }

            int correctCount = 0;
            foreach (var result in results)
            {
                if (result.isCorrect) correctCount++;
            }

            progress = (float)correctCount / results.Count;
            return correctCount == results.Count;
        }

        // Initialization methods for event bindings and canvas setup
        public void InitEventBinds()
        {
            onOneConditionStarted += OnOneConditionStarted;
            onOneConditionFinished += OnOneConditionFinished;
            onAllConditionsFinished += OnAllConditionFinished;
        }

        public void InitCanvas()
        {
            button_1_TaskGuidance = canvas_1_TaskGuidance.GetComponentInChildren<Button>();
            button_2_OneConditionCompleted = canvas_2_OneConditionCompleted.GetComponentInChildren<Button>();
            button_3_StartNewCondition = canvas_3_StartNewCondition.GetComponentInChildren<Button>();
            button_4_AllConditionsCompleted = canvas_4_AllConditionsCompleted.GetComponentInChildren<Button>();

            if (button_1_TaskGuidance != null)
            {
                button_1_TaskGuidance.onClick.AddListener(OnTaskGuidanceButtonClicked);
            }
            else
            {
                Debug.LogWarning("Button for Task Guidance is not found.");
            }
            if (button_2_OneConditionCompleted != null)
            {
                button_2_OneConditionCompleted.onClick.AddListener(OnOneConditionCompletedButtonClicked);
            }
            else
            {
                Debug.LogWarning("Button for One Condition Completed is not found.");
            }
            if (button_3_StartNewCondition != null)
            {
                button_3_StartNewCondition.onClick.AddListener(OnStartNewConditionButtonClicked);
            }
            else
            {
                Debug.LogWarning("Button for Start New Condition is not found.");
            }
            if (button_4_AllConditionsCompleted != null)
            {
                button_4_AllConditionsCompleted.onClick.AddListener(OnAllConditionsCompletedButtonClicked);
            }
            else
            {
                Debug.LogWarning("Button for All Conditions Completed is not found.");
            }
        }

        /// Button click handlers
        private void OnTaskGuidanceButtonClicked()
        {
            canvas_1_TaskGuidance.SetActive(false);
            onOneConditionStarted?.Invoke();
        }

        private void OnOneConditionCompletedButtonClicked()
        {
            canvas_2_OneConditionCompleted.SetActive(false);

            currentExperimentIndex++;
            if (currentExperimentIndex < conditionOrders.Count)
            {
                StartExperimentRound(currentExperimentIndex); // Start the next experiment round
            }
        }

        private void OnStartNewConditionButtonClicked()
        {
            canvas_3_StartNewCondition.SetActive(false);
            onOneConditionStarted?.Invoke();
        }

        private void OnAllConditionsCompletedButtonClicked()
        {
            canvas_4_AllConditionsCompleted.SetActive(false);
            // Handle any logic needed when all conditions are completed
            Debug.Log("All conditions have been completed.");
        }

        // Task Event handlers
        public void OnOneConditionStarted()
        {
            // Todo: Add logic for when one condition starts
        }

        public void OnOneConditionFinished()
        {
            Debug.Log("All the items are placed in correct position");
            if (currentExperimentIndex < conditionOrders.Count - 1)
            {
                canvas_2_OneConditionCompleted.SetActive(true);
            }
            else
            {
                canvas_4_AllConditionsCompleted?.SetActive(true);
            }
        }

        public void OnAllConditionFinished()
        {
            Debug.Log("All the conditions of this task are completed");
            canvas_4_AllConditionsCompleted.SetActive(false);
        }

        // Task specific methods
        public void StartExperimentRound(int expIndex)
        {
            if (expIndex < 0 || expIndex >= conditionOrders.Count)
            {
                Debug.LogError($"Experiment index {expIndex} is out of range.");
                return;
            }

            currentExperimentIndex = expIndex;
            condition = conditionOrders[currentExperimentIndex];
            conditionManager.SetMode(condition);
            Debug.Log($"Starting experiment round {currentExperimentIndex + 1} with condition: {condition}");

            // Initialize the game state
            CreateGrabbableBox();
            CreateGoal();
            currentConditionComplted = false;

            if (expIndex == 0)
            {
                canvas_1_TaskGuidance.SetActive(true);
            }
            else
            {
                canvas_3_StartNewCondition.SetActive(true);
            }
        }

    }
}
