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
    public RandomVoicePlay randomVoicePlayer;
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
        PlayRandomWaitVoice();
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
            voice: Voice.Echo,
            responseFormat: SpeechResponseFormat.PCM);

        var speechClip = await openAI.AudioEndpoint.GetSpeechAsync(speechRequest);

        if (audioSource.isPlaying)
        {
            Debug.Log("AudioSource is currently playing. Waiting...");
            await Task.Run(() => {
                while (audioSource.isPlaying)
                {
                    Task.Delay(100).Wait(); // Every 100 ms check once
                }
            });
        }

        audioSource.clip = speechClip;
        audioSource.Play();
        Debug.Log("Clip Length = " + speechClip.Length);
        onAvatarStartSpeak?.Invoke(speechClip.Length);

        // Debug output
        float ttsTime = Time.time - debugTime;
        debugTime = Time.time;
        Debug.Log(speechClip);
        Debug.Log("TTS Time = " + ttsTime);
    }

    protected void PlayRandomWaitVoice()
    {
        if (randomVoicePlayer == null)
        {
            Debug.LogWarning("Random voice player is not assigned. Please check it.");
            return;
        }
        randomVoicePlayer.audioSource = audioSource;
        randomVoicePlayer.PlayRandomAudio();
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
        AvatarStartPointingByName(currentInteractObject, speechDuration);
    }

    // DEBUG
    //private void LateUpdate()
    //{
    //    FakeAvatarStartPointing();
    //}

    // DEBUG
    //[ContextMenu("Fake Avatar Start Pointing")]
    //public void FakeAvatarStartPointing()
    //{
    //    if (Keyboard.current.gKey.wasPressedThisFrame)
    //    {
    //        AvatarStartPointingByName("InteractObject_PottedPlant (1)", 5.0f);
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