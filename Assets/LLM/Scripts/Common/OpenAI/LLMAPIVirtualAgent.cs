using UnityEngine;
using UnityEngine.UI;

using OpenAI.Chat;
using OpenAI;

using System.Collections.Generic;
using System.Collections;
using System.Linq;

using Newtonsoft.Json;
using TMPro;


public class LLMAPIVirtualAgent : LLMAPI
{
    [Header("UI Settings")]
    // UI references
    public TMP_InputField inputField;
    public TextMeshProUGUI responseText;
    public Button sendButton;

    protected override void Init()
    {
        base.Init();
        if (sendButton != null )
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
        messages.Add(new Message(Role.User, userInputJson));
        Debug.Log($"User: {userInputJson}");

        // Create a chat request and send it to OpenAI, wait until get response
        var chatRequest = new ChatRequest(messages, llmModel);
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
        messages.Add(new Message(Role.Assistant, rawResponse));

        // Update UI textg
        responseText.text = jsonObjResponse.answer;

        // Update current point object
        currentPointingObject = jsonObjResponse.gaze_and_pointing_object;

        // Send TTS Request
        await TextToSpeechRequest(jsonObjResponse.answer);
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
    }
}
