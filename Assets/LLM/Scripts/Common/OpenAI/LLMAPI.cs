using OpenAI;
using OpenAI.Realtime;
using OpenAI.Models;
using OpenAI.Audio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Threading;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;

using Utilities.Audio;
using Utilities.Encoding.Wav;

using TMPro;
using Newtonsoft.Json;
using OpenAI.Chat;
using Unity.XR.CoreUtils.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class LLMAPI : MonoBehaviour
{
    // OpenAI Variables
    [Header("OpenAI Settings")]
    public bool enableDebug = false;
    private OpenAIClient openAI;
    private Model llmModel = new Model("o4-mini", "openai");
    private List<Message> messages = new List<Message>();
    private JsonSchema outputSchema;

    // System prompt for the assistant
    [Header("Prompt Settings")]
    public TextAsset systemPromptAsset;
    public string systemPrompt;

    [Header("Interaction Settings")]
    public GazeSphereDetector gazeSphereDetector;
    public AvatarController avatarController;

    // UI references
    public TMP_InputField inputField;
    public TextMeshProUGUI responseText;

    // Debug Vars
    private float debugTime;

    private void Awake()
    {
        LoadPromptsFromFile();
        Init();
    }

    private void Start()
    {

    }

    /// <summary>
    /// Initialize OpenAI real-time session, configure authentication, start receiving events.
    /// </summary>
    public void Init()
    {
        // Load authentication from file
        string authPath;
#if UNITY_ANDROID && !UNITY_EDITOR
        authPath = Path.Combine(Application.persistentDataPath, "auth.json");
#else
        var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        authPath = Path.Combine(userPath, ".openai", "auth.json");
#endif

        // Create OpenAI client
        openAI = new OpenAIClient(new OpenAIAuthentication().LoadFromPath(authPath))
        {
            EnableDebug = enableDebug
        };
        RecordingManager.EnableDebug = enableDebug;
        ResetMessages();

    }

    public void ResetMessages()
    {
        messages.Clear();
        messages.Add(new Message(Role.System, systemPrompt));
    }

    public async void UserChatInput(string userContent)
    {
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
                    objRelativePosDict.Add(obj, interactObj.GetRelativePositionToMainCameraDirection());
                }
            }
            foreach (var obj in gazeObjectNameList)
            {
                var interactObj = InteractObjectManager.Instance?.GetObjectByName(obj);
                if (interactObj != null && !objRelativePosDict.ContainsKey(obj))
                {
                    objRelativePosDict.Add(obj, interactObj.GetRelativePositionToMainCameraDirection());
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

        // Create a chat request and send it to OpenAI
        var chatRequest = new ChatRequest(messages, llmModel);
        var (jsonObjResponse, response) = await openAI.ChatEndpoint.GetCompletionAsync<AIResponse>(chatRequest);
        //var response = await openAI.ChatEndpoint.GetCompletionAsync(chatRequest);


        responseText.text = response;
        // If the response is not null, update the UI and log the response
        if (jsonObjResponse != null)
        {
            messages.Add(new Message(Role.Assistant, response)); // Add assistant response to the message list
            responseText.text = jsonObjResponse.answer;
            Debug.Log($" AI: {jsonObjResponse.answer} | Pointing: {jsonObjResponse.gaze_and_pointing_object}");
        }
        else
        {
            Debug.LogError("Response is null!");
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

    /// <summary>
    /// Load system prompt and template from files.
    /// </summary>
    private void LoadPromptsFromFile()
    {
        if (systemPromptAsset != null)
            systemPrompt = systemPromptAsset.text;
        else
            Debug.LogWarning("systemPromptAsset is not valid!");
    }

    [ContextMenu("Test")]
    public async void Test()
    {
        messages = new List<Message>
        {
            new Message(Role.System, "You are a helpful assistant."),
            new Message(Role.User, "Who won the world series in 2020?"),
            new Message(Role.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
            new Message(Role.User, "Where was it played?"),
        };

        Debug.Log("Test");
        var chatRequest = new ChatRequest(messages, llmModel);
        var response = await openAI.ChatEndpoint.GetCompletionAsync(chatRequest);
        Debug.Log("Choice Count = " + response.Choices.Count);
        var choice = response.FirstChoice;
        Debug.Log($"[{choice.Index}] {choice.Message.Role}: {choice.Message} | Finish Reason: {choice.FinishReason}");
    }

    [ContextMenu("Test2")]
    public async void Test2()
    {
        messages = new List<Message>
        {
            new(Role.System, "You are a helpful math tutor. Guide the user through the solution step by step."),
            new(Role.User, "how can I solve 8x + 7 = -23")
        };

        //var chatRequest = new ChatRequest(messages, model: "gpt-4o-2024-08-06", jsonSchema: outputSchema);
        //var response = await openAI.ChatEndpoint.GetCompletionAsync(chatRequest);
        //Debug.Log("Response: " + response);

        var chatRequest = new ChatRequest(messages, model: "gpt-4o-2024-08-06");
        var (mathResponse, chatResponse) = await openAI.ChatEndpoint.GetCompletionAsync<MathResponse>(chatRequest);

        for (var i = 0; i < mathResponse.Steps.Count; i++)
        {
            var step = mathResponse.Steps[i];
            Debug.Log($"Step {i}: {step.Explanation}");
            Debug.Log($"Result: {step.Output}");
        }

        Debug.Log($"Final Answer: {mathResponse.FinalAnswer}");
        chatResponse.GetUsage();
    }


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

    public class MathResponse
    {
        [JsonProperty("steps")]
        public IReadOnlyList<MathStep> Steps { get; private set; }

        [JsonProperty("final_answer")]
        public string FinalAnswer { get; private set; }
    }

    public class MathStep
    {
        [JsonProperty("explanation")]
        public string Explanation { get; private set; }

        [JsonProperty("output")]
        public string Output { get; private set; }
    }
}