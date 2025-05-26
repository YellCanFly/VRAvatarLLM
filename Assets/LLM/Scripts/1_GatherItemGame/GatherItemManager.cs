using UnityEngine;

public class GatherItemManager : MonoBehaviour
{
    [Header("Gather Item Settings")]
    public InteractCondition condition = InteractCondition.Baseline;
    public GatherItemObject[] targetGatherObjects;
    public GatherItemIconManager iconManager;

    [Header("Avatar Settings")]
    public GameObject avatarBaseline;
    public GameObject avatarVoiceOnly;
    public GameObject avatarEmbodied;

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
                avatarVoiceOnly.SetActive(false);
                avatarEmbodied.SetActive(false);
                break;
            case InteractCondition.UniDirectional:
                avatarBaseline.SetActive(false);
                avatarVoiceOnly.SetActive(true);
                avatarEmbodied.SetActive(false);
                break;
            case InteractCondition.BiDirectional:
                avatarBaseline.SetActive(false);
                avatarVoiceOnly.SetActive(false);
                avatarEmbodied.SetActive(true);
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
            case InteractCondition.UniDirectional:
                isUserUseGaze = true; // UniDirectional condition uses gaze
                gazeSphereDetector.showGazeResult = true; // Enable gaze result display
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
