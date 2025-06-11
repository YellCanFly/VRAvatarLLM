using UnityEngine;
using UnityEngine.UI;

using OpenAI.Chat;
using OpenAI;

using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using TMPro;


public class LLMAPI_GatherItem_UniDirec_Output : LLMAPI
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

    override public void AddObjectInfoToSystemPrompot()
    {
        // Add objects info to system prompt
        ObjectsInfo objectsInfo = new ObjectsInfo();
        foreach (var interactObj in InteractObjectManager.Instance.allInteractObjects)
        {
            objectsInfo.objects_info.Add(interactObj.GetComponentInChildren<GatherItemObject>().GetGrabItemInfoInt());
        }
        string objectsInfoJsonStr = JsonUtility.ToJson(objectsInfo, false);
        systemPrompt += objectsInfoJsonStr;
        ResetMessages();
    }

    override public async void UserChatInput(string userContent)
    {
        base.UserChatInput(userContent);

        debugTime = Time.time;

        // Init user input object
        UserInput userInput = new();
        userInput.question = userContent;

        // Parse gaze data from GazeSphereDetector
        //if (gazeSphereDetector != null)
        //{
        //    var allObjectInEyeFieldList = gazeSphereDetector.GetAllObjectInEyeFieldList(); // List<string> For all objects in eye field

        //    userInput.objects_in_view = allObjectInEyeFieldList;
        //}
        //else
        //{
        //    // Fallback to default values if GazeSphereDetector is not available
        //    userInput.objects_in_view = new List<string>();
        //}

        // Add user input to the message list
        string userInputJson = JsonUtility.ToJson(userInput, true);
        Message userMessage = new Message(Role.User, userInputJson);
        AddMessage(userMessage);
        onUserMessageSent?.Invoke(userMessage, startRecordingTime);
        Debug.Log($"User: {userInputJson}");

        // Create a chat request and send it to OpenAI, wait until get response
        var (jsonObjResponse, response) = await GetChatCompletionGenericAsync<AIResponse>(GetAllMessages(), llmModel);

        // Handle response
        HandleAIResponse(jsonObjResponse, response);
    }

    public async void HandleAIResponse(AIResponse jsonObjResponse, ChatResponse rawResponse)
    {
        if (rawResponse == null)
        {
            HandleInvalidResponse();
            return;
        }

        Debug.Log("Raw Chat Response: " + rawResponse);

        // Add response to message list
        Message avatarMessage = new Message(Role.Assistant, rawResponse);
        AddMessage(avatarMessage);
        onAIResponseReceived?.Invoke(avatarMessage);

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
                        GatherItemManager.Instance.ExcuteShowWrongItem(gatherItemObject);
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
                GatherItemManager.Instance.PlayCollectWrongSound();
            }
            else
            {
                Debug.Log("Avatar successfully confirmed and handed over the object.");
                GatherItemManager.Instance.PlayCollectCorrectSound();
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

        //[JsonProperty("objects_in_view")]
        //public List<string> objects_in_view;

        //[JsonProperty("objects_info")]
        //public List<GatherItemObjectInfo> objects_info;
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
