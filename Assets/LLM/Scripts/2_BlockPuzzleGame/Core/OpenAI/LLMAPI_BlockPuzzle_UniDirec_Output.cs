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
    public class LLMAPI_BlockPuzzle_UniDirec_Output : LLMAPI
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

        override public void AddObjectInfoToSystemPrompot()
        {
            Debug.Log("Adding object info to system prompt...");
            // Add objects info to system prompt
            AllPlaceInfo target_places_info = new();
            foreach (var interactObj in InteractObjectManager.Instance.allInteractObjects)
            {
                PlaceInfo placeInfo = new();
                placeInfo.place_name = interactObj.name;
                placeInfo.place_relative_position = interactObj.GetRelativePositionToCamera(gazeSphereDetector.transform);
                target_places_info.target_places_info.Add(placeInfo);
                Debug.Log($"Object: {interactObj.name}, Relative Position: {placeInfo.place_relative_position.x}, {placeInfo.place_relative_position.z}, {placeInfo.place_relative_position.height}");
            }
            string objectsInfoJsonStr = JsonUtility.ToJson(target_places_info, false);
            systemPrompt += objectsInfoJsonStr;
            ResetMessages();
        }

        override public async void UserChatInput(string userContent)
        {
            base.UserChatInput(userContent);

            debugTime = Time.time;

            // Init user input place
            UserInput userInput = new();
            userInput.question = userContent;

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
            string userInputJson = JsonUtility.ToJson(userInput, false);
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

            [JsonProperty("current_grabbed_object_info")]
            public GrabbedObjectInfo current_grabbed_object_info;

            [JsonProperty("placement_evaluations")]
            public List<PlacementEvaluation> placementEvaluations;
        }

        [System.Serializable]
        public class AllPlaceInfo
        {
            [JsonProperty("target_places_info")]
            public List<PlaceInfo> target_places_info = new();
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

            [JsonProperty("gaze_and_pointing_object")]
            public string gaze_and_pointing_object { get; private set; }
        }
    }
}
