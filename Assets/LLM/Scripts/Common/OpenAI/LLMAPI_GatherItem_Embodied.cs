using UnityEngine;
using UnityEngine.UI;

using OpenAI.Chat;
using OpenAI;

using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using TMPro;
using UnityEditor;


public class LLMAPI_GatherItem_Embodied : LLMAPI
{
    [Header("UI Settings")]
    // UI references
    public TMP_InputField inputField;
    public TextMeshProUGUI responseText;
    public Button sendButton;


    protected bool confirmAndHandOver = false;

    protected override void Init()
    {
        base.Init();
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClick);
        }
    }

    override public async void UserChatInput(string userContent)
    {
        debugTime = Time.time;

        // Init user input object
        UserInput userInput = new();
        userInput.question = userContent;

        // Parse gaze data from GazeSphereDetector
        if (gazeSphereDetector != null)
        {
            string gazeObjectName = gazeSphereDetector.GetLatestGazeObject(); // string For the latest gaze object
            var gazeObjectNameList = gazeSphereDetector.GetGazeObjectList(); // List<string> For gaze history
            var allObjectInEyeFieldList = gazeSphereDetector.GetAllObjectInEyeFieldList(); // List<string> For all objects in eye field

            Dictionary<string, RelativePosition> objRelativePosDict = new();
            foreach (var obj in allObjectInEyeFieldList)
            {
                Debug.Log($"Object in eye field: {obj}");
                var interactObj = InteractObjectManager.Instance?.GetObjectByName(obj);
                if (interactObj != null && !objRelativePosDict.ContainsKey(obj))
                {
                    objRelativePosDict.Add(obj, interactObj.GetRelativePositionToCamera(Camera.main.transform));
                }
            }
            foreach (var obj in gazeObjectNameList)
            {
                var interactObj = InteractObjectManager.Instance?.GetObjectByName(obj);
                if (interactObj != null && !objRelativePosDict.ContainsKey(obj))
                {
                    objRelativePosDict.Add(obj, interactObj.GetRelativePositionToCamera(Camera.main.transform));
                }
            }

            userInput.current_gaze_object = gazeObjectName;
            userInput.gaze_history = gazeObjectNameList;
            userInput.objects_in_view = allObjectInEyeFieldList;
            userInput.objects_info = objRelativePosDict.Select(kvp => new ObjectInfo
            {
                object_name = kvp.Key,
                object_relative_position = kvp.Value
            }).ToList();
        }
        else
        {
            // Fallback to default values if GazeSphereDetector is not available
            userInput.current_gaze_object = "null";
            userInput.gaze_history = new List<string>();
            userInput.objects_in_view = new List<string>();
            userInput.objects_info = new List<ObjectInfo>();
        }

        // Add user input to the message list
        string userInputJson = JsonUtility.ToJson(userInput, true);
        AddMessage(new Message(Role.User, userInputJson));
        Debug.Log($"User: {userInputJson}");

        // Create a chat request and send it to OpenAI, wait until get response
        var chatRequest = new ChatRequest(GetAllMessages(), llmModel);
        var (jsonObjResponse, response) = await openAI.ChatEndpoint.GetCompletionAsync<AIResponse>(chatRequest);

        // Debug print the LLM's inference and data communication time
        float llmTime = Time.time - debugTime;
        Debug.Log("LLM Time = " + llmTime + " s");

        // Handle response
        HandleAIResponse(jsonObjResponse, response);
    }

    public async void HandleAIResponse(AIResponse jsonObjResponse, ChatResponse rawResponse)
    {
        if (jsonObjResponse == null)
        {
            Debug.LogError("Error: Response is null!");
            return;
        }

        Debug.Log("Raw Chat Response: " + rawResponse);

        // Add response to message list
        AddMessage(new Message(Role.Assistant, rawResponse));

        // Update UI textg
        responseText.text = jsonObjResponse.answer;

        // Update current point object
        currentInteractObject = jsonObjResponse.gaze_and_pointing_object;

        // Check if the avatar should confirm and hand over the object
        confirmAndHandOver = jsonObjResponse.confirm_and_hand_over;

        // Send TTS Request
        await TextToSpeechRequest(jsonObjResponse.answer);
    }

    protected override void AvatarAnimationWhileSpeaking(float speechDuration)
    {
        if (confirmAndHandOver)
        {
            bool isAnswerCorrect = false;
            Debug.Log("Avatar is confirming and handing over the object: " + currentInteractObject);
            // Todo: add animation and locig to confirm and hand over the object (Current version is just disappeared)
            var gazeObject = InteractObjectManager.Instance?.GetObjectByName(currentInteractObject);
            if (gazeObject != null)
            {
                if (gazeObject.TryGetComponent<GatherItemObject>(out var gatherItemObject))
                {
                    if (gatherItemObject == GatherItemManager.Instance.GetCurrentTargetGatherItem())
                    {
                        GatherItemManager.Instance.ExecuteGatherCurrentTargetItem();
                        isAnswerCorrect = true;
                    }
                    else
                    {
                        Debug.LogWarning($"Object '{currentInteractObject}' is not the current target item.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Object '{currentInteractObject}' is not a GatherItemObject.");
                }
            }
            else
            {
                Debug.LogWarning($"Object '{currentInteractObject}' not found in InteractObjectManager.");
            }

            if (!isAnswerCorrect)
            {
                Debug.LogWarning("Avatar failed to confirm and hand over the object correctly.");
                // Todo: Play warning voice

            }
        }
        else
        {
            Debug.Log("Avatar is speaking while pointing an object.");
            AvatarStartPointingByName(currentInteractObject, speechDuration);
        }
    }

    public void OnSendButtonClick()
    {
        string userInput = inputField.text;
        if (!string.IsNullOrEmpty(userInput))
        {
            UserChatInput(userInput);
            inputField.text = string.Empty;
        }
    }

    // Structures

    [System.Serializable]
    public class UserInput
    {
        [JsonProperty("question")]
        public string question;

        [JsonProperty("current_gaze_object")]
        public string current_gaze_object;

        [JsonProperty("gaze_history")]
        public List<string> gaze_history;

        [JsonProperty("objects_in_view")]
        public List<string> objects_in_view;

        [JsonProperty("objects_info")]
        public List<ObjectInfo> objects_info;
    }

    [System.Serializable]
    public class ObjectInfo
    {
        [JsonProperty("object_name")]
        public string object_name;

        [JsonProperty("object_relative_position")]
        public RelativePosition object_relative_position;
    }


    /// <summary>
    /// Serializable response class for JSON parsing.
    /// </summary>
    [System.Serializable]
    public class AIResponse
    {
        [JsonProperty("answer")]
        public string answer { get; private set; }

        [JsonProperty("gaze_and_pointing_object")]
        public string gaze_and_pointing_object { get; private set; }

        [JsonProperty("confirm_and_hand_over")]
        public bool confirm_and_hand_over { get; private set; }
    }
}
