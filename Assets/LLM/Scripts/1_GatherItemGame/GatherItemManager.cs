using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GatherItemManager : MonoBehaviour
{
    public static GatherItemManager Instance { get; private set; }

    [Header("Condition Settings")]
    public InteractCondition condition = InteractCondition.Baseline;
    public List<InteractCondition> conditionOrders;
    public int currentExperimentIndex = 0;
    public UnityAction onAllConditionsFinished;

    [Header("Gather Item Settings")]
    public GatherItemIconManager iconManager;
    public GatherItemObject[] targetGatherObjects;
    public int currentTargetItemIndex = 0;
    public UnityAction onStartCollectRound;
    public UnityAction onAllTargetItemCollected;

    [Header("Avatar Settings")]
    public GameObject avatarBaseline;
    public GameObject avatarUniDirecInput;
    public GameObject avatarUniDirecOutput;
    public GameObject avatarBiDirec;
    public GameObject avatarAcitivate;

    [Header("User Settings")]
    public bool isUserUseGaze = true;
    public GazeSphereDetector gazeSphereDetector;

    [Header("UI Settings")]
    public GameObject collectGuidanceCanvas;
    public GameObject collectCompletedCanvas;
    public GameObject collectNewRoundCanvas;
    public GameObject taskCompletedCanvas;

    private Button collectGuidanceButton;
    private Button collectCompletedButton;
    private Button collectNewRoundButton;
    private Button taskCompletedButton;
    

    private void Awake()
    {
        // Singleton pattern to ensure only one instance of GatherItemManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        conditionOrders = ExperimentManager.GetConditionOrder(ExperimentManager.Instance.participantID);

        // Initialize
        InitCanvasRefs();
        InitActionBinds();
    }

    void Start()
    {
        currentExperimentIndex = 0; // Reset the experiment index at the start
        StartExperimentRound(currentExperimentIndex); // Start the first experiment round
    }


    #region Initialization Methods
    public void InitExperimentCondition()
    {
        InitAvatar();
        InitGazeDetector();
        InitGatherItemIcons();
    }

    /// <summary>
    /// Initializes the avatar based on the specified interaction condition.
    /// </summary>
    private void InitAvatar()
    {
        switch (condition)
        {
            case InteractCondition.Baseline:
                avatarBaseline.SetActive(true);
                avatarUniDirecInput.SetActive(false);
                avatarUniDirecOutput.SetActive(false);
                avatarBiDirec.SetActive(false);
                avatarAcitivate = avatarBaseline;
                break;
            case InteractCondition.UniDirectional_Input:
                avatarBaseline.SetActive(false);
                avatarUniDirecInput.SetActive(true);
                avatarUniDirecOutput.SetActive(false);
                avatarBiDirec.SetActive(false);
                avatarAcitivate = avatarUniDirecInput;
                break;
            case InteractCondition.UniDirectional_Output:
                avatarBaseline.SetActive(false);
                avatarUniDirecInput.SetActive(false);
                avatarUniDirecOutput.SetActive(true);
                avatarBiDirec.SetActive(false);
                avatarAcitivate = avatarUniDirecOutput;
                break;
            case InteractCondition.BiDirectional:
                avatarBaseline.SetActive(false);
                avatarUniDirecInput.SetActive(false);
                avatarUniDirecOutput.SetActive(false);
                avatarBiDirec.SetActive(true);
                avatarAcitivate = avatarBiDirec;
                break;
        }

        // Initialize the gaze sphere detector for the active avatar
        avatarAcitivate.GetComponentInChildren<LLMAPI>().gazeSphereDetector = gazeSphereDetector;
    }

    /// <summary>
    /// Initializes the gaze detector based on the interaction condition.
    /// </summary>
    private void InitGazeDetector()
    {
        switch (condition)
        {
            case InteractCondition.Baseline:
                isUserUseGaze = false; // Baseline condition does not use gaze
                gazeSphereDetector.showGazeResult = false; // Disable gaze result display
                break;
            case InteractCondition.UniDirectional_Input:
                isUserUseGaze = true; // UniDirectionalInput condition uses gaze
                gazeSphereDetector.showGazeResult = true; // Enable gaze result display
                break;
            case InteractCondition.UniDirectional_Output:
                isUserUseGaze = false; // UniDirectionalOutput condition does not use gaze
                gazeSphereDetector.showGazeResult = false; // Disable gaze result display
                break;
            case InteractCondition.BiDirectional:
                isUserUseGaze = true; // BiDirectional condition uses gaze
                gazeSphereDetector.showGazeResult = true; // Enable gaze result display
                break;
        }
    }

    /// <summary>
    /// Initializes the gather item icons for each target gather object.
    /// </summary>
    private void InitGatherItemIcons()
    {
        if (iconManager == null)
        {
            Debug.LogError("GatherItemIconManager is not assigned in GatherItemManager.");
            return;
        }

        foreach (var item in targetGatherObjects)
        {
            if (item != null)
            {
                // Create an icon for each gatherable item
                GatherItemIcon icon = iconManager.CreateIcon(item.itemIcon);
                item.gatherItemIconWidget = icon;
                item.SetItemIconActive(false); 
            }
        }
        targetGatherObjects[currentTargetItemIndex].SetItemIconActive(true); // Activate the icon for the first item
    }

    private void InitCanvasRefs()
    {
        collectGuidanceButton = collectGuidanceCanvas.GetComponentInChildren<Button>();
        collectCompletedButton = collectCompletedCanvas.GetComponentInChildren<Button>();
        collectNewRoundButton = collectNewRoundCanvas.GetComponentInChildren<Button>();
        taskCompletedButton = taskCompletedCanvas.GetComponentInChildren<Button>();

        if (collectGuidanceButton != null)
        {
            collectGuidanceButton.onClick.AddListener(OnCollecGuidanceButtonClicked);
        }
        else
        {
            Debug.LogError("Collect Guidance Button is not assigned in GatherItemManager.");
        }
        if (collectCompletedButton != null)
        {
            collectCompletedButton.onClick.AddListener(OnCollectCompletedButtonClicked);
        }
        else
        {
            Debug.LogError("Collect Completed Button is not assigned in GatherItemManager.");
        }
        if (collectNewRoundButton != null)
        {
            collectNewRoundButton.onClick.AddListener(OnCollectNewRoundButtonClicked);
        }
        else
        {
            Debug.LogError("Collect New Round Button is not assigned in GatherItemManager.");
        }
        if (taskCompletedButton != null)
        {
            taskCompletedButton.onClick.AddListener(OnTaskCompletedButtonClicked);
        }
        else
        {
            Debug.LogError("Task Completed Button is not assigned in GatherItemManager.");
        }
    }

    private void InitActionBinds()
    {
        onStartCollectRound += OnStartCollectItem;
        onAllTargetItemCollected += OnAllTargetItemCollected;
        onAllConditionsFinished += OnAllConditionsFinished;
    }
    #endregion


    #region Task processing methods
    public GatherItemObject GetCurrentTargetGatherItem()
    {
        return targetGatherObjects[currentTargetItemIndex];
    }

    public void ExecuteGatherCurrentTargetItem()
    {
        var targetItem = GetCurrentTargetGatherItem();
        if (targetItem != null)
        {
            targetItem.SetObjectGathered(); // Mark the item as gathered
            currentTargetItemIndex++;
            if (currentTargetItemIndex < targetGatherObjects.Length)
            {
                targetGatherObjects[currentTargetItemIndex].SetItemIconActive(true); // Activate the next item icon
            }
            else
            {
                Debug.Log("All items have been gathered.");
                onAllTargetItemCollected?.Invoke(); // Invoke the event when all items are gathered
            }
        }
    }

    public void StartExperimentRound(int expIndex)
    {
        if (expIndex < 0 || expIndex >= conditionOrders.Count)
        {
            Debug.LogError("Invalid experiment index: " + expIndex);
            return;
        }

        condition = conditionOrders[expIndex];
        InitExperimentCondition();


        if (expIndex == 0)
        {
            collectGuidanceCanvas.SetActive(true); // Show guidance for the first round
        }
        else
        {
            collectNewRoundCanvas.SetActive(true); // Show new round canvas for subsequent rounds
        }

    }
    #endregion


    #region Event Handlers
    private void OnStartCollectItem()
    {
        // Todo: Implement the logic to start record conversation history, time, etc.
    }

    private void OnAllTargetItemCollected()
    {
        // Todo: Implement the logic to handle when all target items are collected

        // Trigger to next round or finish the experiment
        if (currentExperimentIndex < conditionOrders.Count - 1)
        {
            collectNewRoundCanvas.SetActive(true); // Show the new round canvas for the next experiment
        }
        else
        {
            onAllConditionsFinished?.Invoke(); // Invoke the action when all conditions are finished
        }
    }

    private void OnAllConditionsFinished()
    {
        taskCompletedCanvas.SetActive(true); // Show the task completed canvas
    }
    #endregion


    #region UI Button Callbacks
    private void OnCollecGuidanceButtonClicked()
    {
        collectGuidanceCanvas.SetActive(false); // Hide the guidance canvas
        onStartCollectRound?.Invoke(); // Invoke the action to start the collection round
    }

    private void OnCollectCompletedButtonClicked()
    {
        collectCompletedCanvas.SetActive(false); // Hide the completion canvas

        currentExperimentIndex++;
        if (currentExperimentIndex < conditionOrders.Count)
        {
            StartExperimentRound(currentExperimentIndex); // Start the next experiment round
        }
    }

    private void OnCollectNewRoundButtonClicked()
    {
        collectNewRoundCanvas.SetActive(false); // Hide the new round canvas
    }

    private void OnTaskCompletedButtonClicked()
    {
        taskCompletedCanvas.SetActive(false); // Hide the task completed canvas
        ExperimentManager.Instance.TurnToSceneByName("L_LLMScene_2_BlockPuzzle"); // Transition to the next scene
    }
    #endregion

}
