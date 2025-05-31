using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GatherItemManager : MonoBehaviour
{
    public static GatherItemManager Instance { get; private set; }

    [Header("Gather Item Settings")]
    public InteractCondition condition = InteractCondition.Baseline;
    public List<InteractCondition> conditionOrders;
    public GatherItemObject[] targetGatherObjects;
    public GatherItemIconManager iconManager;
    public int currentTargetItemIndex = 0;
    public UnityAction onAllTargetItemGathered;

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
    public GameObject gatherGuidance;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        InitExperimentCondition();
    }

    void Start()
    {
        InitGatherItemIcons();
    }

    public void InitExperimentCondition()
    {
        InitAvatar();
        InitGazeDetector();
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
                onAllTargetItemGathered?.Invoke(); // Invoke the event when all items are gathered
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



    }

}
