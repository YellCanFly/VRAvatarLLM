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


namespace BlockPuzzleGame{
    public class LMAPI_BlockPuzzle_Baseline : LLMAPI
    {
        [Header("UI Settings")]
        // UI references
        public TMP_InputField inputField;
        public TextMeshProUGUI responseText;
        public Button sendButton;

        [Header("Knowledge")]
        public AnswerCheckerManager answerCheckerManager;

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
                // var allPlacesInEyeFieldList = gazeSphereDetector.GetAllObjectInEyeFieldList(); // List<string> For all objects in eye field
                var allPlaces = InteractObjectManager.Instance?.GetAllObjects().Select(obj => obj.name).ToList() ?? new List<string>();

                Dictionary<string, RelativePosition> objRelativePosDict = new();
                foreach (var obj in allPlaces)
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

                // userInput.current_gaze_place = gazePlaceName;
                // userInput.gaze_history = gazePlaceNameList;
                userInput.target_places_info = objRelativePosDict.Select(kvp => new PlaceInfo
                {
                    place_name = kvp.Key,
                    place_relative_position = kvp.Value
                }).ToList();
            }
            else
            {
                // Fallback to default values if GazeSphereDetector is not available
                // userInput.current_gaze_place = "null";
                // userInput.gaze_history = new List<string>();
                userInput.target_places_info = new List<PlaceInfo>();
            }

            // If the user is holding an object, add it to the user input
            var currentGrabbedObject = GrabInteractObjectManager.Instance?.CurrentHeldObject;
            
            userInput.current_grabbed_object_info = new GrabbedObjectInfo
            {
                current_grabbed_object_name = currentGrabbedObject != null ? currentGrabbedObject.ObjectName : "null",
                current_grabbed_object_color = currentGrabbedObject != null ? currentGrabbedObject.ObjectColorName : "null"
            };

            // If the user has placed objects, add them to the user input
            var placedObjects = answerCheckerManager.GetAllResults();
            userInput.placementEvaluations = placedObjects.Select(result => new PlacementEvaluation
            {
                isCorrect = result.isCorrect,
                place_name = result.locationName,
                placedObjects = result.placedObjects.Select(obj => obj != null ? obj.name : "null").ToList()
            }).ToList();


            // Add user input to the message list
            string userInputJson = JsonUtility.ToJson(userInput, true);
            Message userMessage = new Message(Role.User, userInputJson);
            AddMessage(userMessage);
            onUserMessageSent?.Invoke(userMessage, startRecordingTime);
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
            Message avatarMessage = new Message(Role.Assistant, rawResponse);
            AddMessage(avatarMessage);
            onAIResponseReceived?.Invoke(avatarMessage);

            // Update UI textg
            responseText.text = jsonObjResponse.answer;

            // Update current point object
            // currentInteractObject = jsonObjResponse.gaze_and_pointing_object;

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

            // [JsonProperty("current_gaze_place")]
            // public string current_gaze_place;

            // [JsonProperty("gaze_history")]
            // public List<string> gaze_history;

            [JsonProperty("target_places_info")]
            public List<PlaceInfo> target_places_info;

            [JsonProperty("current_grabbed_object_info")]
            public GrabbedObjectInfo current_grabbed_object_info;

            [JsonProperty("placement_evaluations")]
            public List<PlacementEvaluation> placementEvaluations;
        }

        [System.Serializable]
        public class PlaceInfo
        {
            [JsonProperty("place_name")]
            public string place_name;

            [JsonProperty("place_relative_position")]
            public RelativePosition place_relative_position;
        }

        [System.Serializable]
        public class GrabbedObjectInfo
        {
            [JsonProperty("current_grabbed_object_name")]
            public string current_grabbed_object_name;

            [JsonProperty("current_grabbed_object_color")]
            public string current_grabbed_object_color;
        }

        [System.Serializable]
        public class PlacementEvaluation
        {
            [JsonProperty("is_correct")]
            public bool isCorrect;
            [JsonProperty("place_name")]
            public string place_name;
            [JsonProperty("placed_objects")]
            public List<string> placedObjects;
        }


        /// <summary>
        /// Serializable response class for JSON parsing.
        /// </summary>
        [System.Serializable]
        public class AIResponse
        {
            [JsonProperty("answer")]
            public string answer { get; private set; }

            // [JsonProperty("gaze_and_pointing_object")]
            // public string gaze_and_pointing_object { get; private set; }
        }
    }
}
