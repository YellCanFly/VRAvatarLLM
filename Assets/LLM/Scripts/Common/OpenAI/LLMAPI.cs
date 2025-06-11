using UnityEngine;
using UnityEngine.Events;

using Utilities.Audio;
using Utilities.Encoding.Wav;

using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using OpenAI;
using OpenAI.Models;
using OpenAI.Chat;
using OpenAI.Audio;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

[RequireComponent(typeof(AudioSource))]
public class LLMAPI : MonoBehaviour
{
    // OpenAI Variables
    [Header("OpenAI Settings")]
    public bool enableDebug = false;
    protected OpenAIClient openAI;
    protected Model llmModel = new Model("o4-mini", "openai");
    protected Model ttsModel = new Model("tts-1", "openai");
    protected Queue<Message> messageQueue = new Queue<Message>();
    protected Message systemMessage;
    public int maxMessageCount = 10;

    // System prompt for the assistant
    [Header("Prompt Settings")]
    public TextAsset systemPromptAsset;
    public string systemPrompt;

    [Header("Interaction Settings")]
    public bool isAvatarEmbodied = true;
    public GazeSphereDetector gazeSphereDetector;
    public AvatarController avatarController;
    public RandomVoicePlay randomWaitVoicePlayer;
    public RandomVoicePlay randomAskRepeatVoicePlayer;
    protected string currentInteractObject = "";

    [Header("Voice Recording Settings")]
    public bool isRecording;
    public GameObject RecordingIcon;
    private AudioSource audioSource;
    private MemoryStream transcriptionStream;
    private CancellationTokenSource recordingCts;
    protected float startRecordingTime;

    public UnityAction<Message, float> onUserMessageSent; // Message, Start recording time
    public UnityAction<Message> onAIResponseReceived; // AI response message
    public UnityAction<float> onAvatarStartSpeak;

    public event Action onTranscriptionTimeout;
    public event Action onChatCompletionTimeout;
    public event Action onTextToSpeechTimeout;
    public event Action onLLMAPIProcessWentWrong;

    // Debug Vars
    protected float debugTime;

    private void Awake()
    {
        LoadPromptsFromFile();
        Init();
    }

    /// <summary>
    /// Initialize OpenAI real-time session, configure authentication, start receiving events.
    /// </summary>
    protected virtual void Init()
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

        audioSource = GetComponent<AudioSource>();

        onAvatarStartSpeak += AvatarAnimationWhileSpeaking;

        onTranscriptionTimeout += OnTranscriptionTimeout;
        onChatCompletionTimeout += OnChatCompletionTimeout;
        onTextToSpeechTimeout += OnTextToSpeechTimeout;
        onLLMAPIProcessWentWrong += OnLLMAPIProcessWentWrong;
    }

    /// <summary>
    /// Update function to check for user input (VR or Keyboard) to start/stop recording.
    /// </summary>
    protected virtual void Update()
    {
        bool isVRConnected = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);

        if (
            (isVRConnected && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch)) ||
            (!isVRConnected && Input.GetKeyDown(KeyCode.Return))
        )
        {
            Debug.Log("Start recording triggered");
            StartMicRecording();
        }

        if (
            (isVRConnected && OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch)) ||
            (!isVRConnected && Input.GetKeyUp(KeyCode.Return))
        )
        {
            Debug.Log("Stop recording triggered");
            StopMicRecording();
        }
    }


    /// <summary>
    /// Virtual function to handle user's input (Implemented in child classses)
    /// </summary>
    /// <param name="userContent"></param>
    public virtual async void UserChatInput(string userContent)
    {
        Debug.Log("User input: " + userContent);
        //PlayRandomWaitVoice();
        //avatarController.TriggerThinkingAnimation();
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

    /// <summary>
    /// 1. STT: Sends an audio transcription request to the OpenAI Whisper API.
    /// </summary>
    /// <param name="audioStream">The audio data as a MemoryStream.</param>
    /// <param name="audioFileName">The name of the audio file (e.g., "mic_audio.wav").</param>
    /// <param name="languageCode">The language of the audio (e.g., "en", "es").</param>
    /// <returns>The transcribed text if successful, otherwise null.</returns>
    public async Task<string> TranscribeAudioAsync(
        MemoryStream audioStream,
        string audioFileName,
        string languageCode = "en",
        int timeoutSeconds = 15)
    {
        if (openAI == null)
        {
            Debug.LogError("OpenAI API instance is not initialized. Cannot transcribe audio.");
            await audioStream.DisposeAsync(); // Ensure stream is disposed even if API is not ready
            return null;
        }

        audioStream.Position = 0;

        var audioRequest = new AudioTranscriptionRequest(
            audio: audioStream,
            audioName: audioFileName,
            model: Model.Whisper1,
            language: languageCode
        );

        string transcriptionResult = null;
        float transcriptionStartTime = Time.time;

        // Setting maximun waiting time
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        bool hasTimedOut = false;
        try
        {
            Task<string> transcriptionTask = openAI.AudioEndpoint.CreateTranscriptionTextAsync(audioRequest, cts.Token);
            Task completedTask = await Task.WhenAny(transcriptionTask, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token));

            if (completedTask == transcriptionTask)
            {
                transcriptionResult = await transcriptionTask;
                float transcriptionTime = Time.time - transcriptionStartTime;
                Debug.Log("Transcription result: " + transcriptionResult);
                Debug.Log("Transcription Time = " + transcriptionTime);
            }
            else
            {
                Debug.LogWarning($"Transcription request timed out after {timeoutSeconds} seconds.");
                if (!hasTimedOut)
                {
                    onTranscriptionTimeout?.Invoke();
                    hasTimedOut = true;
                }
                cts.Cancel();
                transcriptionResult = null;
            }
        }
        catch (OperationCanceledException)
        {
            // 这会捕获由于 cts.Cancel() 或 Task.Delay 超时导致的取消
            Debug.LogWarning("Transcription request was explicitly cancelled or timed out.");
            if (!hasTimedOut)
            {
                onTranscriptionTimeout?.Invoke();
                hasTimedOut = true;
            }
            transcriptionResult = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Transcription failed: {e.Message}");
            onLLMAPIProcessWentWrong?.Invoke();
            transcriptionResult = null;
        }
        finally
        {
            // Dispose the stream after use to release resources
            await audioStream.DisposeAsync();
            cts.Dispose();
        }
        return transcriptionResult;
    }

    /// <summary>
    /// 2. LLM: Makes a chat completion request to OpenAI and deserializes the response
    /// into a specified generic type.
    /// </summary>
    /// <typeparam name="T">The desired type of the return structure (e.g., AIResponse, MyCustomData).</typeparam>
    /// <param name="messages">The list of chat messages to send.</param>
    /// <param name="llmModel">The language model to use (e.g., "gpt-4", "gpt-3.5-turbo").</param>
    /// <returns>A tuple containing the deserialized object of type T and the raw API response string.</returns>
    public async Task<(T parsedResponse, ChatResponse rawResponse)> GetChatCompletionGenericAsync<T>(
        List<Message> messages,
        Model llmModel,
        int timeoutSeconds =30) where T : new() // T must have a parameterless constructor
    {
        float llmStartTime = Time.time; // Start timing for this request    

        if (openAI == null)
        {
            Debug.LogError("OpenAI API instance is not initialized. Please assign it in the Inspector or initialize it in code.");
            return (default(T), null); // Return default values if API is not initialized
        }

        var chatRequest = new ChatRequest(messages, llmModel);

        T jsonObjResponse = default(T);
        ChatResponse rawResponse = null;

        CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        bool hasTimedOut = false;
        try
        {
            // The core of the generic approach with cancellation token:
            // GetCompletionAsync<T> directly handles deserialization into T
            // Ensure your OpenAI_API library version supports passing CancellationToken to GetCompletionAsync.
            Task<(T, ChatResponse)> completionTask = openAI.ChatEndpoint.GetCompletionAsync<T>(chatRequest, cts.Token);
            Task completedTask = await Task.WhenAny(completionTask, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token));

            if (completedTask == completionTask)
            {
                (jsonObjResponse, rawResponse) = await completionTask;
                Debug.Log("Chat completion successful.");
                float llmTime = Time.time - llmStartTime;
                Debug.Log("LLM Time = " + llmTime + " s");
            }
            else // completedTask 是 Task.Delay，表示超时了
            {
                Debug.LogWarning($"Chat completion request timed out after {timeoutSeconds} seconds. Cancelling operation.");
                if (!hasTimedOut)
                {
                    hasTimedOut = true;
                    onChatCompletionTimeout?.Invoke();
                }
                cts.Cancel();
                jsonObjResponse = default(T);
                rawResponse = null;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("Chat completion request was explicitly cancelled or timed out (OperationCanceledException).");
            if (!hasTimedOut)
            {
                hasTimedOut = true;
                onChatCompletionTimeout?.Invoke();
            }
            jsonObjResponse = default(T);
            rawResponse = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting chat completion: {e.Message}");
            onLLMAPIProcessWentWrong?.Invoke();
            jsonObjResponse = default(T);
            rawResponse = null;
        }
        finally
        {
            cts.Dispose();
        }

        return (jsonObjResponse, rawResponse);
    }

    /// <summary>
    /// 3. TTS: Convert text response to speech and play it.
    /// Includes a timeout mechanism for the API call.
    /// </summary>
    /// <param name="speechContent">The text content to convert to speech.</param>
    /// <param name="timeoutSeconds">The maximum time to wait for the TTS response in seconds. Defaults to 30.</param>
    protected async Task TextToSpeechRequest(
        string speechContent, 
        int timeoutSeconds = 30) // Added timeoutSeconds parameter
    {
        float ttsStartTime = Time.time; // Start timing for this request
        Debug.Log("Start to request TTS: " + speechContent);

        if (openAI == null)
        {
            Debug.LogError("OpenAI API instance is not initialized. Please assign it in the Inspector or initialize it in code.");
            return;
        }
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is not assigned. Cannot play speech.");
            return;
        }

        var speechRequest = new SpeechRequest(
            model: ttsModel,
            input: speechContent,
            voice: Voice.Echo,
            responseFormat: SpeechResponseFormat.PCM);

        AudioClip speechClip = null;

        // Create CancellationTokenSource for timeout and cancellation
        CancellationTokenSource cts = new CancellationTokenSource();
        // Set timeout duration
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        // Flag to prevent multiple timeout event invokes
        bool hasTimedOut = false;

        try
        {
            // The TTS API call task, ensuring it supports cancellation tokens.
            // Check your OpenAI_API library if GetSpeechAsync has an overload that takes CancellationToken.
            Task<SpeechClip> speechTask = openAI.AudioEndpoint.GetSpeechAsync(speechRequest, partialClipCallback:null, cancellationToken: cts.Token);
            // Race the API call against the timeout task
            Task completedTask = await Task.WhenAny(speechTask, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token));

            if (completedTask == speechTask)
            {
                // TTS API call completed successfully within the timeout
                speechClip = await speechTask;
                float ttsTime = Time.time - debugTime;
                Debug.Log("TTS request successful.");
                Debug.Log("TTS Time = " + ttsTime);
            }
            else // completedTask is Task.Delay, indicating a timeout
            {
                Debug.LogWarning($"Text-to-Speech request timed out after {timeoutSeconds} seconds. Cancelling operation.");
                if (!hasTimedOut) // Only invoke if not already timed out
                {
                    onTextToSpeechTimeout?.Invoke(); // Trigger the timeout event
                    hasTimedOut = true;
                }
                cts.Cancel(); // Signal cancellation to the ongoing speechTask
                speechClip = null; // Explicitly set to null for clarity
            }
        }
        catch (OperationCanceledException)
        {
            // This catches cancellations, potentially from cts.Cancel() or Task.Delay timeout
            Debug.LogWarning("Text-to-Speech request was explicitly cancelled or timed out (OperationCanceledException).");
            if (!hasTimedOut) // Only invoke if not already timed out
            {
                onTextToSpeechTimeout?.Invoke(); // Trigger the timeout event
                hasTimedOut = true;
            }
            speechClip = null; // Ensure null if cancelled
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during Text-to-Speech API request: {e.Message}");
            // Handle error gracefully, e.g., show a message to the user
            onLLMAPIProcessWentWrong?.Invoke();
            speechClip = null; // Ensure null on error
        }
        finally
        {
            // Dispose the CancellationTokenSource resources
            cts.Dispose();
        }

        // If speechClip is null at this point, it means the API request failed, timed out, or was cancelled
        if (speechClip == null)
        {
            Debug.LogError("Speech clip was null after API request (failed, timed out, or cancelled). Cannot play audio.");
            return; // Exit the function early if no clip
        }

        // --- Audio Playback Logic (Only proceeds if speechClip is not null) ---
        while (audioSource.isPlaying)
        {
            Debug.Log("AudioSource is currently playing. Waiting...");
            await Task.Delay(2000); // 不阻塞主线程
        }

        audioSource.clip = speechClip;
        audioSource.Play();
        Debug.Log("Clip Length = " + speechClip.length);
        onAvatarStartSpeak?.Invoke(speechClip.length);

        // Debug output
        Debug.Log(speechClip); // This logs the AudioClip object itself, not its content
    }

    public void OnTranscriptionTimeout()
    {
        onLLMAPIProcessWentWrong?.Invoke();
    }

    public void OnChatCompletionTimeout()
    {
        onLLMAPIProcessWentWrong?.Invoke();
    }

    public void OnTextToSpeechTimeout()
    {
        onLLMAPIProcessWentWrong?.Invoke();
    }

    public void OnLLMAPIProcessWentWrong()
    {
        avatarController.TriggerThinkingAnimation();
        PlayRandomAskRepeatVoice();
    }

    protected void HandleInvalidResponse()
    {
        Message errorAvatarMessage = new Message(Role.Assistant, "Sorry. Can you repeat it again?");
        AddMessage(errorAvatarMessage); // add or not?
        onAIResponseReceived?.Invoke(errorAvatarMessage);
        Debug.LogError("Error: Response is null!");
    }

    protected void PlayRandomWaitVoice()
    {
        if (randomWaitVoicePlayer == null)
        {
            Debug.LogWarning("Random wait voice player is not assigned. Please check it.");
            return;
        }
        randomWaitVoicePlayer.audioSource = audioSource;
        randomWaitVoicePlayer.PlayRandomAudio();
    }

    protected void PlayRandomAskRepeatVoice()
    {
        if (randomWaitVoicePlayer == null)
        {
            Debug.LogWarning("Random ask repeat voice player is not assigned. Please check it.");
            return;
        }
        randomAskRepeatVoicePlayer.audioSource = audioSource;
        randomAskRepeatVoicePlayer.PlayRandomAudio();
    }

    [ContextMenu("Test Avatar React After User Speaking")]
    protected void AvatarReactAfterUserSpeaking()
    {
        Debug.Log("Avatar reacts after user speaking.");
        PlayRandomWaitVoice();
        avatarController.TriggerThinkingAnimation();
    }


    #region Message Management
    public virtual void AddObjectInfoToSystemPrompot()
    {

    }

    public void ResetMessages()
    {
        systemMessage = new Message(Role.System, systemPrompt);
        messageQueue.Clear();
    }

    public void AddMessage(Message newMessage)
    {
        while (messageQueue.Count >= maxMessageCount)
        {
            messageQueue.Dequeue();
        }

        messageQueue.Enqueue(newMessage);
    }

    public List<Message> GetAllMessages()
    {
        var all = new List<Message> { systemMessage };
        all.AddRange(messageQueue);
        return all;
    }
    #endregion

    #region Microphone Record
    /// <summary>
    /// Start microphone recording and stream to memory.
    /// </summary>
    protected void StartMicRecording()
    {
        Debug.Log("Recording started...");
        startRecordingTime = Time.time;
        debugTime = Time.time;
        isRecording = true;
        recordingCts = new CancellationTokenSource();
        transcriptionStream = new MemoryStream();

        RecordingManager.StartRecordingStream<WavEncoder>(async (buffer) =>
        {
            if (!recordingCts.Token.IsCancellationRequested)
            {
                if (RecordingIcon != null)
                {
                    RecordingIcon.SetActive(true);
                }
                await transcriptionStream.WriteAsync(buffer, CancellationToken.None);
            }
        }, 24000, recordingCts.Token);
    }

    /// <summary>
    /// Stop recording and send audio for transcription.
    /// </summary>
    protected async void StopMicRecording()
    {
        float speakingTime = Time.time - debugTime;
        debugTime = Time.time;
        Debug.Log("Stopping recording... ");
        Debug.Log("Speaking Time = " + speakingTime);

        isRecording = false;
        recordingCts?.Cancel();
        if (recordingCts != null)
        {
            RecordingIcon.SetActive(false);
        }

        AvatarReactAfterUserSpeaking();

        // Call the new encapsulated function for transcription
        string result = await TranscribeAudioAsync(transcriptionStream, "mic_audio.wav", "en");

        // Handle the transcription result
        if (!string.IsNullOrEmpty(result))
        {
            UserChatInput(result); // Pass the result to your chat input handler
        }
        else
        {
            Debug.LogWarning("Transcription returned empty or failed.");
            // Optionally, provide user feedback that transcription failed
        }
    }
    #endregion

    #region Avatar Animation
    protected virtual void AvatarAnimationWhileSpeaking(float speechDuration)
    {
        Debug.Log("Avatar is speaking while pointing an object.");
        AvatarStartPointingByName(currentInteractObject, speechDuration);
    }

    //// DEBUG
    //private void LateUpdate()
    //{
    //    FakeAvatarStartPointing();
    //}

    //// DEBUG
    //public void FakeAvatarStartPointing()
    //{
    //    if (Keyboard.current.gKey.wasPressedThisFrame)
    //    {
    //        AvatarStartPointingByName("InteractObject_PottedPlant (1)", 10.0f);
    //    }
    //}

    protected void AvatarStartPointingByName(string objectName, float pointDuration)
    {
        var gazeObject = InteractObjectManager.Instance?.GetObjectByName(objectName);
        if (gazeObject != null)
        {
            currentInteractObject = objectName;
            Vector3 gazeObjectPosition = gazeObject.transform.position;
            // Todo: Add gaze and point logic here
            if (avatarController != null)
            {
                avatarController.StartPointing(gazeObjectPosition);
                if (pointDuration < 5f)
                    pointDuration = 5f; // Ensure minimum pointing duration
                StartCoroutine(AvatarKeepPointingInDuration(pointDuration));
            }
        }
        else
        {
            Debug.LogWarning($"Gaze object '{objectName}' not found.");
        }
    }

    /// <summary>
    /// Keep pointing for a duration then stop pointing (Back to normal gesture)
    /// </summary>
    /// <param name="pointDuration"></param>
    /// <returns></returns>
    IEnumerator AvatarKeepPointingInDuration(float pointDuration)
    {
        yield return new WaitForSeconds(pointDuration);
        avatarController.StopPointing();
        currentInteractObject = "";
    }
    #endregion
}
public enum InteractCondition
{
    Baseline = 0,
    UniDirectional_Input = 1,
    UniDirectional_Output = 2,
    BiDirectional = 3,
}
