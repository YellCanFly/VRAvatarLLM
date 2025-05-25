using UnityEngine;
using TMPro;

using Utilities.Audio;
using Utilities.Encoding.Wav;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using OpenAI;
using OpenAI.Models;
using OpenAI.Chat;
using OpenAI.Audio;
using System.Collections;
using UnityEngine.Events;
using UnityEditor.Experimental.GraphView;



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
    public GazeSphereDetector gazeSphereDetector;
    public AvatarController avatarController;
    protected string currentPointingObject = "";

    [Header("Voice Recording Settings")]
    public bool isRecording;
    private AudioSource audioSource;
    private MemoryStream transcriptionStream;
    private CancellationTokenSource recordingCts;
    protected UnityAction<float> onStartSpeak;
    protected UnityAction onFinishSpeak;

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
        onStartSpeak += AvatarAnimationWhileSpeaking;

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
    /// Convert text response to speech and play it.
    /// </summary>
    protected async Task TextToSpeechRequest(string speechContent)
    {
        debugTime = Time.time;
        var speechRequest = new SpeechRequest(
            model: ttsModel,
            input: speechContent,
            responseFormat: SpeechResponseFormat.PCM);

        var speechClip = await openAI.AudioEndpoint.GetSpeechAsync(speechRequest);
        audioSource.clip = speechClip;
        audioSource.Play();
        Debug.Log("Clip Length = " + speechClip.Length);
        onStartSpeak?.Invoke(speechClip.Length);

        // Debug output
        float ttsTime = Time.time - debugTime;
        debugTime = Time.time;
        Debug.Log(speechClip);
        Debug.Log("TTS Time = " + ttsTime);
    }

    #region Message Management
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
        debugTime = Time.time;
        isRecording = true;
        recordingCts = new CancellationTokenSource();
        transcriptionStream = new MemoryStream();

        RecordingManager.StartRecordingStream<WavEncoder>(async (buffer) =>
        {
            if (!recordingCts.Token.IsCancellationRequested)
            {
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

        transcriptionStream.Position = 0;

        var audioRequest = new AudioTranscriptionRequest(
            audio: transcriptionStream,
            audioName: "mic_audio.wav",
            model: Model.Whisper1,
            language: "en"
        );

        try
        {
            var result = await openAI.AudioEndpoint.CreateTranscriptionTextAsync(audioRequest);
            float transcriptionTime = Time.time - debugTime;
            debugTime = Time.time;
            Debug.Log("Transcription result: " + result);
            Debug.Log("Transcription Time = " + transcriptionTime);
            UserChatInput(result);
        }
        catch (Exception e)
        {
            Debug.LogError("Transcription failed: " + e);
        }
        finally
        {
            await transcriptionStream.DisposeAsync();
        }
    }
    #endregion

    #region Avatar Animation
    protected virtual void AvatarAnimationWhileSpeaking(float speechDuration)
    {
        Debug.Log("Avatar is speaking while pointing an object.");
        AvatarStartPointingByName(currentPointingObject, speechDuration);
    }

    protected void AvatarStartPointingByName(string objectName, float pointDuration)
    {
        var gazeObject = InteractObjectManager.Instance?.GetObjectByName(objectName);
        if (gazeObject != null)
        {
            Vector3 gazeObjectPosition = gazeObject.transform.position;
            // Todo: Add gaze and point logic here
            if (avatarController != null)
            {
                avatarController.LookAtTarget.transform.position = gazeObjectPosition;
                avatarController.StartPointing();
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
        currentPointingObject = "";
    }
    #endregion
}