using UnityEngine;
using UnityEngine.UI;

using OpenAI.Chat;
using OpenAI;

using System.Collections.Generic;
using System.Collections;
using System.Linq;

using Newtonsoft.Json;
using TMPro;
using UnityEditor;


public class LLMAPI_BlockPuzzle_Embodied : LLMAPI
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

        // Init user input place
        UserInput userInput = new();
        userInput.question = userContent;

        // Parse gaze data from GazeSphereDetector
        if (gazeSphereDetector != null)
        {
            string gazePlaceName = gazeSphereDetector.GetLatestGazeObject(); // string For the latest gaze object
            var gazePlaceNameList = gazeSphereDetector.GetGazeObjectList(); // List<string> For gaze history
            var allPlacesInEyeFieldList = gazeSphereDetector.GetAllObjectInEyeFieldList(); // List<string> For all objects in eye field

            Dictionary<string, RelativePosition> objRelativePosDict = new();
            foreach (var obj in allPlacesInEyeFieldList)
            {
                Debug.Log($"Object in eye field: {obj}");
                var interactObj = InteractObjectManager.Instance?.GetObjectByName(obj);
                if (interactObj != null && !objRelativePosDict.ContainsKey(obj))
                {
                    objRelativePosDict.Add(obj, interactObj.GetRelativePositionToCamera(Camera.main.transform));
                }
            }
            foreach (var obj in gazePlaceNameList)
            {
                var interactObj = InteractObjectManager.Instance?.GetObjectByName(obj);
                if (interactObj != null && !objRelativePosDict.ContainsKey(obj))
                {
                    objRelativePosDict.Add(obj, interactObj.GetRelativePositionToCamera(Camera.main.transform));
                }
            }

            userInput.current_gaze_place = gazePlaceName;
            userInput.gaze_history = gazePlaceNameList;
            userInput.places_in_view = allPlacesInEyeFieldList;
            userInput.target_places_info = objRelativePosDict.Select(kvp => new PlaceInfo
            {
                place_name = kvp.Key,
                place_relative_position = kvp.Value
            }).ToList();
        }
        else
        {
            // Fallback to default values if GazeSphereDetector is not available
            userInput.current_gaze_place = "null";
            userInput.gaze_history = new List<string>();
            userInput.places_in_view = new List<string>();
            userInput.target_places_info = new List<PlaceInfo>();
        }

        // If the user is holding an object, add it to the user input
        var currentGrabbedObject = GrabInteractObjectManager.Instance?.CurrentHeldObjectName;
        if (!string.IsNullOrEmpty(currentGrabbedObject))
        {
            userInput.current_grabbed_object = currentGrabbedObject;
        }
        else
        {
            userInput.current_grabbed_object = "null";
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

        // Send TTS Request
        await TextToSpeechRequest(jsonObjResponse.answer);
    }

    protected override void AvatarAnimationWhileSpeaking(float speechDuration)
    {
        Debug.Log("Avatar is speaking while pointing an object.");
        AvatarStartPointingByName(currentInteractObject, speechDuration);
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

        [JsonProperty("current_gaze_place")]
        public string current_gaze_place;

        [JsonProperty("gaze_history")]
        public List<string> gaze_history;

        [JsonProperty("places_in_view")]
        public List<string> places_in_view;

        [JsonProperty("target_places_info")]
        public List<PlaceInfo> target_places_info;

        [JsonProperty("current_grabbed_object")]
        public string current_grabbed_object = "null";
    }

    [System.Serializable]
    public class PlaceInfo
    {
        [JsonProperty("place_name")]
        public string place_name;

        [JsonProperty("place_relative_position")]
        public RelativePosition place_relative_position;
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
