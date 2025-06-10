using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using OpenAI.Chat;
using UnityEngine.Audio;
using System.IO;

namespace BlockPuzzleGame
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(ExperimentDataCollector))]
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

        [Header("Data Collection Settings")]
        public float userBehaviorSaveInterval = 1f / 30f;
        private float userBehaviorSaveTimer = 0f; // Timer to control the saving of user behavior data

        [Header("Audio Settings")]
        public AudioSource audioSource;
        public AudioClip correctSound; // Sound to play when an item is collected correctly
        public AudioClip wrongSound; // Sound to play when an item is collected incorrectly

        [Header("User Settings")]
        public GazeSphereDetector gazeSphereDetector;
        public bool showGazeResult = true;

        private bool currentConditionComplted = true;
        private float currentProgress = 0f;

        private ExperimentDataCollector dataCollector;
        private TaskData_BlockPuzzle dataPerCondition;


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
            dataCollector = GetComponent<ExperimentDataCollector>();
            InitEventBinds();
            InitCanvas();
            InitFromConfigFile();
        }

        private void Start()
        {
            // Start the first experiment round
            currentExperimentIndex = 0;
            StartExperimentRound(currentExperimentIndex);

            //GrabInteractObjectManager.Instance.onHeldObject += ;
            //GrabInteractObjectManager.Instance.onDropedObject += ;
        }

        private void Update()
        {
            // Check if the user is behaving
            if (!currentConditionComplted)
            {
                userBehaviorSaveTimer += Time.deltaTime;
                if (userBehaviorSaveTimer >= userBehaviorSaveInterval)
                {
                    userBehaviorSaveTimer = 0f; // Reset the timer
                    OnUserBehaving(); // Call the method to save user behavior data
                }
            }
            else
            {
                userBehaviorSaveTimer = 0f; // Reset the timer if not collecting
            }

        }

        private void InitFromConfigFile()
        {
            string path = Path.Combine(Application.persistentDataPath, "config.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var config = JsonUtility.FromJson<ExperimentConfig>(json);
                Debug.Log("Config loaded: " + json);

                showGazeResult = config.is_gaze_previewd; // Load the gaze preview setting from the config
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

        private void CheckAndUpdateProgress()
        {
            bool conditionEnd = CheckOneConditionFinished(out float checkProgress);
            if (checkProgress > currentProgress)
            {
                Debug.Log($"Task progress updated: {checkProgress * 100:F1}% complete.");
            }
            currentProgress = checkProgress;

            if (conditionEnd)
            {
                onOneConditionFinished?.Invoke();
            }
        }

        // Grab interact events
        public void OnObjectPlaced(bool isCorrect, string objectName, string placeName)
        {
            if (isCorrect)
            {
                CheckAndUpdateProgress();
                PlayCorrectSound();
            }
            else
            {
                PlayWrongSound();
            }

            BlockPuzzleData_PlaceRecord placeRecord = new();
            placeRecord.time = Time.time;
            placeRecord.isCorrect = isCorrect;
            placeRecord.placeName = placeName;
            placeRecord.objectName = objectName;

            dataPerCondition.blockPuzzlePlaceRecords.Add(placeRecord);
        }

        public void OnObjectRemoved()
        {
            
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

        private void InitGazeDetector()
        {
            switch (condition)
            {
                case InteractCondition.Baseline:
                    gazeSphereDetector.showGazeResult = false; // Disable gaze result display
                    break;
                case InteractCondition.UniDirectional_Input:
                    gazeSphereDetector.showGazeResult = true; // Enable gaze result display
                    break;
                case InteractCondition.UniDirectional_Output:
                    gazeSphereDetector.showGazeResult = false; // Disable gaze result display
                    break;
                case InteractCondition.BiDirectional:
                    gazeSphereDetector.showGazeResult = true; // Enable gaze result display
                    break;
            }
            gazeSphereDetector.showGazeResult = gazeSphereDetector.showGazeResult && showGazeResult; // Combine with the global setting
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
        private void OnUserBehaving()
        {
            dataPerCondition.behaviorFrames.Add(dataCollector.GetCurrentUserBehaviorFrame());
        }

        private void OnUserMessageSent(Message message, float startRecordingTime)
        {
            dataPerCondition.conversationFrames.Add(new ConversationData_MessageFrame()
            {
                sentTime = Time.time,
                startRecordingTime = startRecordingTime,
                message = message
            });
        }

        private void OnAIMessageReceived(Message message)
        {
            dataPerCondition.conversationFrames.Add(new ConversationData_MessageFrame()
            {
                sentTime = Time.time,
                startRecordingTime = 0f, // AI messages do not have a recording start time
                message = message
            });
        }

        public void OnOneConditionStarted()
        {
            // Todo: Add logic for when one condition starts
            dataPerCondition = new();
            dataPerCondition.condition = condition;
            dataPerCondition.participantID = ExperimentManager.Instance.participantID;
            dataPerCondition.targetPlacesInfo = answerCheckerManager.GetAllTrackerData();
            dataPerCondition.behaviorFrames.Clear(); // Clear previous frames for the new round
            dataPerCondition.blockPuzzlePlaceRecords.Clear();
            dataPerCondition.conversationFrames.Clear(); // Clear previous conversation frames for the new round

            currentConditionComplted = false;
        }

        public async void OnOneConditionFinished()
        {
            Debug.Log("All the items are placed in correct position");

            currentConditionComplted = true; // Mark the current condition as completed
            string dataFileName = string.Format(
                "User{0:D2}_Condition{1:D2}_Task2_Data.json",
                ExperimentManager.Instance.participantID,
                (int)condition + 1
            );
            //ExperimentDataCollector.SaveTaskDataToJson(dataPerCondition, dataFileName);
            await ExperimentDataCollector.SaveTaskDataToJsonAsync(dataPerCondition, dataFileName);


            if (currentExperimentIndex < conditionOrders.Count - 1)
            {
                canvas_2_OneConditionCompleted.SetActive(true);
            }
            else
            {
                onAllConditionsFinished?.Invoke();
            }
        }

        public void OnAllConditionFinished()
        {
            Debug.Log("All the conditions of this task are completed");
            canvas_4_AllConditionsCompleted?.SetActive(true);
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
            conditionManager.activateAvatar.GetComponentInChildren<LLMAPI>().onUserMessageSent += OnUserMessageSent;
            conditionManager.activateAvatar.GetComponentInChildren<LLMAPI>().onAIResponseReceived += OnAIMessageReceived;

            Debug.Log($"Starting experiment round {currentExperimentIndex + 1} with condition: {condition}");

            // Initialize the game state
            InitGazeDetector();
            CreateGrabbableBox();
            CreateGoal();

            if (expIndex == 0)
            {
                canvas_1_TaskGuidance.SetActive(true);
            }
            else
            {
                canvas_3_StartNewCondition.SetActive(true);
            }
        }


        public void PlayCorrectSound()
        {
            if (audioSource != null && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
            else
            {
                Debug.LogWarning("AudioSource or collectCorrectSound is not assigned.");
            }
        }

        public void PlayWrongSound()
        {
            if (audioSource != null && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }
            else
            {
                Debug.LogWarning("AudioSource or collectWrongSound is not assigned.");
            }
        }

    }
}
