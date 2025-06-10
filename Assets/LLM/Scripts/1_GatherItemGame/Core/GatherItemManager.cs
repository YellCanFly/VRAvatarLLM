using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilities.Extensions;
using System.Collections;
using System.IO;
using OpenAI.Chat;
using System;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(ExperimentDataCollector))]
public class GatherItemManager : MonoBehaviour
{
    public static GatherItemManager Instance { get; private set; }

    [Header("Condition Settings")]
    public InteractCondition condition = InteractCondition.Baseline;
    public List<InteractCondition> conditionOrders;
    public int currentExperimentIndex = 0;
    public UnityAction onAllConditionsFinished;

    [Header("Gather Item Settings")]
    public GatherItemObject[] allGatherObjects;
    public List<int> targetGatherItemIdList = new();
    public int collectNumber = 3; // Number of items to collect in each round
    public int currentTargetItemIndex = 0;
    public bool isCollecting = false; // Flag to indicate if the user is currently collecting items
    public Camera renderCamera;
    public UnityAction onOneConditionStarted;
    public UnityAction onOneConditionFinished;

    [Header("Avatar Settings")]
    public GameObject avatarBaseline;
    public GameObject avatarUniDirecInput;
    public GameObject avatarUniDirecOutput;
    public GameObject avatarBiDirec;
    public GameObject avatarAcitivate;

    [Header("User Settings")]
    public GazeSphereDetector gazeSphereDetector;
    public bool showGazeResult = true;

    [Header("UI Settings")]
    public GameObject collectGuidanceCanvas;
    public GameObject collectCompletedCanvas;
    public GameObject collectNewRoundCanvas;
    public GameObject taskCompletedCanvas;

    private Button collectGuidanceButton;
    private Button collectCompletedButton;
    private Button collectNewRoundButton;
    private Button taskCompletedButton;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip collectCorrectSound; // Sound to play when an item is collected correctly
    public AudioClip collectWrongSound; // Sound to play when an item is collected incorrectly


    [Header("Data Collection Settings")]
    public float userBehaviorSaveInterval = 1f / 30f;
    private float userBehaviorSaveTimer = 0f; // Timer to control the saving of user behavior data

    private ExperimentDataCollector dataCollector;
    private TaskData_CollectItem dataPerCondition;

    private void Awake()
    {
        // Singleton pattern to ensure only one instance of GatherItemManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Variables initialization
        conditionOrders = ExperimentManager.GetConditionOrder(ExperimentManager.Instance.participantID);
        audioSource = GetComponent<AudioSource>();
        dataCollector = GetComponent<ExperimentDataCollector>();

        // Initialize for the whole experiment process
        InitCanvasRefs();
        InitActionBinds();
    }

    void Start()
    {
        currentExperimentIndex = 0; // Reset the experiment index at the start
        StartExperimentRound(currentExperimentIndex); // Start the first experiment round
    }

    void Update()
    {
        // Check if the user is behaving
        if (isCollecting)
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


    #region Initialization Methods
    // ------ Initialization for the whole experiment process ------
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
        onOneConditionStarted += OnOneConditionStarted;
        onOneConditionFinished += OnOneConditionFinished;
        onAllConditionsFinished += OnAllConditionsFinished;
    }

    // ------ Initialization for each experiment condition ------
    public void InitExperimentCondition()
    {
        InitAvatar();
        InitGazeDetector();
        InitRandomTargetIDList();
        InitAllGatherItems();
    }

    private void InitAvatar()
    {
        avatarBaseline.SetActive(condition == InteractCondition.Baseline);
        avatarUniDirecInput.SetActive(condition == InteractCondition.UniDirectional_Input);
        avatarUniDirecOutput.SetActive(condition == InteractCondition.UniDirectional_Output);
        avatarBiDirec.SetActive(condition == InteractCondition.BiDirectional);
        switch (condition)
        {
            case InteractCondition.Baseline:
                avatarAcitivate = avatarBaseline;
                break;
            case InteractCondition.UniDirectional_Input:
                avatarAcitivate = avatarUniDirecInput;
                break;
            case InteractCondition.UniDirectional_Output:
                avatarAcitivate = avatarUniDirecOutput;
                break;
            case InteractCondition.BiDirectional:
                avatarAcitivate = avatarBiDirec;
                break;
        }

        // Initialize the gaze sphere detector for the active avatar
        avatarAcitivate.GetComponentInChildren<LLMAPI>().AddObjectInfoToSystemPrompot();
        avatarAcitivate.GetComponentInChildren<LLMAPI>().gazeSphereDetector = gazeSphereDetector;
        avatarAcitivate.GetComponentInChildren<LLMAPI>().onUserMessageSent += OnUserMessageSent;
        avatarAcitivate.GetComponentInChildren<LLMAPI>().onAIResponseReceived += OnAIMessageReceived;
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

    private void InitRandomTargetIDList()
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime nowUtc = DateTime.UtcNow;
        TimeSpan timeSinceEpoch = nowUtc - epoch;
        int seed = (int)timeSinceEpoch.TotalSeconds;

        if (collectNumber > allGatherObjects.Length)
        {
            Debug.LogWarning("Collect number exceeds available gather items. Adjusting to maximum available items.");
            collectNumber = allGatherObjects.Length;
        }

        targetGatherItemIdList.Clear();
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < allGatherObjects.Length; i++)
        {
            availableIndices.Add(i);
        }

        System.Random rng = new System.Random(seed);  // random number generator with a specific seed

        for (int i = 0; i < collectNumber && availableIndices.Count > 0; i++)
        {
            int randomIndex = rng.Next(availableIndices.Count);  // random index from 0 to availableIndices.Count - 1
            targetGatherItemIdList.Add(availableIndices[randomIndex]);
            availableIndices.RemoveAt(randomIndex);
        }
    }

    private void InitAllGatherItems()
    {
        foreach (var item in allGatherObjects)
        {
            if (item != null)
            {
                item.SetActive(true); // Reactivate the item object
                SetItemAsNoTargetItem(item);
            }
        }
        currentTargetItemIndex = 0; // Reset the current target item index
        SetItemAsTargetItem(GetCurrentTargetGatherItem()); // Set the first item as the target item
    }
    #endregion

    #region Task processing methods
    public GatherItemObject GetCurrentTargetGatherItem()
    {
        int targetItemID = targetGatherItemIdList[currentTargetItemIndex];
        return allGatherObjects[targetItemID];
    }

    public void SetItemAsTargetItem(GatherItemObject itemObject)
    {
        itemObject.SetItemToRenderLayer();
        renderCamera.transform.position = itemObject.interactObject.GetObjectBounds().center + new Vector3(0, 0, -0.75f); // Position the camera behind the target item
    }

    public void SetItemAsNoTargetItem(GatherItemObject itemObject)
    {
        itemObject.SetItemToDefaultLayer(); // Reset the item layer to default
    }

    [ContextMenu("Execute Gather Current Target Item")]
    public void ExecuteGatherCurrentTargetItem()
    {
        var targetItem = GetCurrentTargetGatherItem();
        if (targetItem != null)
        {
            // Set current target item as collected
            SetItemAsNoTargetItem(targetItem); // Reset the item layer to default
            var defaultPosition = targetItem.transform.position; // Store the default position of the item
            DisplayItem(targetItem); // Display the item in front of the camera
            StartCoroutine(DelayHideTargetItem(targetItem, 2.0f, defaultPosition)); // Delay hiding the item object for 1 second

            // Set next target item / check if all items are collected
            currentTargetItemIndex++;
            if (currentTargetItemIndex < targetGatherItemIdList.Count)
            {
                SetItemAsTargetItem(GetCurrentTargetGatherItem()); // Set the next item as the target item
            }
            else
            {
                Debug.Log("All items have been gathered.");
                onOneConditionFinished?.Invoke(); // Invoke the event when all items are gathered
            }
        }
    }

    IEnumerator DelayHideTargetItem(GatherItemObject itemObject, float delay, Vector3 defaultPosition)
    {
        yield return new WaitForSeconds(delay);
        if (itemObject != null)
        {
            itemObject.transform.position = defaultPosition; // Reset the position of the item object
            itemObject.SetActive(false); // Deactivate the item object after the delay
        }
        else
        {
            Debug.LogWarning("Item object is null, cannot hide.");
        }
    }

    public void ExcuteShowWrongItem(GatherItemObject wrongItem)
    {
        if (wrongItem != null)
        {
            var defaultPosition = wrongItem.transform.position; // Store the default position of the wrong item
            DisplayItem(wrongItem); // Display the wrong item in front of the camera
            StartCoroutine(DelayPutBackWrongItem(wrongItem, 2.0f, defaultPosition)); // Delay putting back the wrong item for 1 second
        }
        else
        {
            Debug.LogWarning("Wrong item is null, cannot execute display wrong item.");
        }
    }

    IEnumerator DelayPutBackWrongItem(GatherItemObject wrongItem, float delay, Vector3 defaultPosition)
    {
        yield return new WaitForSeconds(delay);
        if (wrongItem != null)
        {
            wrongItem.transform.position = defaultPosition; // Reset the position of the wrong item
        }
        else
        {
            Debug.LogWarning("Wrong item is null, cannot put back.");
        }
    }

    public void DisplayItem(GatherItemObject item)
    {
        //item.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2; // Position the item in front of the camera
        item.transform.position = avatarAcitivate.transform.position + avatarAcitivate.transform.forward * 1 + new Vector3(0, 1f, 0); // Position the item in front of the camera
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

    [ContextMenu("Start Process Render And Save")]
    public void StartProcessRenderAndSave()
    {
        StartCoroutine(ProcessRenderAndSave());
    }

    private IEnumerator ProcessRenderAndSave()
    {
        for (int i = 0; i < allGatherObjects.Length; i++)
        {
            var obj = allGatherObjects[i];

            // 移动摄像机到目标物体背后
            renderCamera.transform.position = obj.interactObject.GetObjectBounds().center + new Vector3(0, 0, -0.75f);

            // 设置渲染图层
            obj.SetItemToRenderLayer();

            // 等待一帧以确保渲染生效
            yield return new WaitForSeconds(1f);

            // 保存图片
            SaveRenderTextureAsImage(renderCamera.targetTexture, "RT_" + i + "_" + obj.name + ".png");

            // 等待 1 秒
            yield return new WaitForSeconds(2f);

            // 恢复图层
            obj.SetItemToDefaultLayer();
        }
    }
    #endregion

    #region Event Handlers
    private void OnUserBehaving()
    {
        if (dataPerCondition == null)
        {
            Debug.LogWarning("Data per condition is not initialized. Cannot record user message.");
            return;
        }
        dataPerCondition.behaviorFrames.Add(dataCollector.GetCurrentUserBehaviorFrame());
    }

    private void OnUserMessageSent(Message message, float startRecordingTime)
    {
        if (dataPerCondition == null)
        {
            Debug.LogWarning("Data per condition is not initialized. Cannot record user message.");
            return;
        }
        float sentTime = Time.time;
        dataPerCondition.conversationFrames.Add(new ConversationData_MessageFrame()
        {
            sentTime = sentTime,
            startRecordingTime = startRecordingTime,
            message = message
        });
        dataPerCondition.currentTargetRecords.Add(new CollectItemData_CurrentTargetRecords()
        {
            timeStamp = sentTime,
            targetObjectName = GetCurrentTargetGatherItem().interactObject.objectName,
        });
    }

    private void OnAIMessageReceived(Message message)
    {
        if (dataPerCondition == null)
        {
            Debug.LogWarning("Data per condition is not initialized. Cannot record user message.");
            return;
        }
        dataPerCondition.conversationFrames.Add(new ConversationData_MessageFrame()
        {
            sentTime = Time.time,
            startRecordingTime = 0f, // AI messages do not have a recording start time
            message = message
        });
    }

    private void OnOneConditionStarted()
    {
        // Todo: Implement the logic to start record conversation history, time, etc.
        dataPerCondition = new();
        dataPerCondition.condition = condition;
        dataPerCondition.participantID = ExperimentManager.Instance.participantID;
        dataPerCondition.behaviorFrames.Clear(); // Clear previous frames for the new round
        dataPerCondition.conversationFrames.Clear(); // Clear previous conversation frames for the new round
        isCollecting = true; // Set the collecting flag to true
    }

    private async void OnOneConditionFinished()
    {
        // Todo: Implement the logic to handle when all target items are collected
        isCollecting = false; // Set the collecting flag to false
        string dataFileName = string.Format(
            "User{0:D2}_Condition{1:D2}_Task1_Data.json",
            ExperimentManager.Instance.participantID,
            (int)condition + 1
        );
        //ExperimentDataCollector.SaveTaskDataToJson(dataPerCondition, dataFileName);
        await ExperimentDataCollector.SaveTaskDataToJsonAsync(dataPerCondition, dataFileName);

        // Trigger to next round or finish the experiment
        if (currentExperimentIndex < conditionOrders.Count - 1)
        {
            collectCompletedCanvas.SetActive(true); // Show the new round canvas for the next experiment
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
        onOneConditionStarted?.Invoke(); // Invoke the action to start the collection round
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
        onOneConditionStarted?.Invoke(); // Invoke the action to start the new collection round
    }

    private void OnTaskCompletedButtonClicked()
    {
        taskCompletedCanvas.SetActive(false); // Hide the task completed canvas
        ExperimentManager.Instance.TurnToSceneByName("L_LLMScene_2_BlockPuzzle"); // Transition to the next scene
    }
    #endregion

    public void PlayCollectCorrectSound()
    {
        if (audioSource != null && collectCorrectSound != null)
        {
            audioSource.PlayOneShot(collectCorrectSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or collectCorrectSound is not assigned.");
        }
    }

    public void PlayCollectWrongSound()
    {
        if (audioSource != null && collectWrongSound != null)
        {
            audioSource.PlayOneShot(collectWrongSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or collectWrongSound is not assigned.");
        }
    }

    public Transform GetActivateAvatarHeadTransform()
    {
        return avatarAcitivate.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
    }

    public void SaveRenderTextureAsImage(RenderTexture rt, string filename)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        // Gamma adjuastment
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                Color c = tex.GetPixel(x, y);
                tex.SetPixel(x, y, c.gamma);
            }
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllBytes(path, bytes);

        Debug.Log($"Saved image with transparency to: {path}");

        RenderTexture.active = currentRT;
    }
}
