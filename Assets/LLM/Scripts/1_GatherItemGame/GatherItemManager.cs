using UnityEngine;

public class GatherItemManager : MonoBehaviour
{
    [Header("Gather Item Settings")]
    public InteractCondition condition = InteractCondition.Baseline;
    public GatherItemObject[] targetGatherObjects;
    public GatherItemIconManager iconManager;

    [Header("Avatar Settings")]
    public GameObject avatarBaseline;
    public GameObject avatarUniDirecInput;
    public GameObject avatarUniDirecOutput;
    public GameObject avatarBiDirec;

    [Header("User Settings")]
    public bool isUserUseGaze = true;
    public GazeSphereDetector gazeSphereDetector;


    private void Awake()
    {
        InitGazeDetector();
        InitAvatar();
    }

    void Start()
    {
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
                break;
            case InteractCondition.UniDirectional_Input:
                avatarBaseline.SetActive(false);
                avatarUniDirecInput.SetActive(true);
                avatarUniDirecOutput.SetActive(false);
                avatarBiDirec.SetActive(false);
                break;
            case InteractCondition.UniDirectional_Output:
                avatarBaseline.SetActive(false);
                avatarUniDirecInput.SetActive(false);
                avatarUniDirecOutput.SetActive(true);
                avatarBiDirec.SetActive(false);
                break;
            case InteractCondition.BiDirectional:
                avatarBaseline.SetActive(false);
                avatarUniDirecInput.SetActive(false);
                avatarUniDirecOutput.SetActive(false);
                avatarBiDirec.SetActive(true);
                break;
        }
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
            }
        }
    }

}
