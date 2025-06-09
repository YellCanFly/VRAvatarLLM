using BlockPuzzleGame;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("Core Components")]
    public ExperimentManager experimentManager;

    [Header("Participant Input")]
    public ParticipantInitInput participantInput;

    [Header("Talking Guidance Settings")]
    public GameObject talkingGuidance;
    public GameObject talkingAvatar;
    public GameObject talkingFinish;

    [Header("Grab item Guidance Settings")]
    public GameObject grabGuadanceCanvas;
    public GameObject grabItemExample;
    private Tutorial_GrabItemCheck grabItemCheck;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip correctSound; // Sound to play when an item is collected correctly

    [Header("Tutorial Ending")]
    public GameObject tutorialEndingUI; // UI to show at the end of the tutorial
    private Button startExperimentButton;



    [Header("Delay time settings")]
    public float waitShowTalkingGuidanceDuration = 1f; // Delay before showing the talking guidance
    public float showTalkingGuidanceDuration = 1f; // Duration to show the talking guidance


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        experimentManager.onParticipantIDConfirmed += DelayShowTalkingGuidance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Show the talking guidance after a delay
    void DelayShowTalkingGuidance()
    {
        StartCoroutine(WaitShowTalkingGuidance(waitShowTalkingGuidanceDuration));
    }

    IEnumerator WaitShowTalkingGuidance(float delayDuration)
    {
        yield return new WaitForSeconds(delayDuration);
        talkingGuidance.SetActive(true);
        talkingAvatar.SetActive(true);
        DelayHideTalkingGuidance();
    }

    // Hide the talking guidance after a delay
    void DelayHideTalkingGuidance()
    {
        StartCoroutine(WaitHideTalkingGuidance(showTalkingGuidanceDuration));
    }

    IEnumerator WaitHideTalkingGuidance(float delayDuration)
    {
        yield return new WaitForSeconds(delayDuration);
        talkingGuidance.SetActive(false);

        talkingFinish.SetActive(true); // Show the finish talking guidance
        talkingFinish.GetComponentInChildren<Button>().onClick.AddListener(OnNextStepButtonClicked); // Bind the button click event
    }

    public void OnNextStepButtonClicked()
    {
        // Logic to handle the next step in the tutorial
        Debug.Log("Next step button clicked. Proceeding to the next part of the tutorial.");
        
        talkingAvatar.SetActive(false); // Hide the talking avatar
        talkingFinish.SetActive(false); // Hide the finish talking guidance
        ShowGrabExample();
    }

    public void ShowGrabExample()
    {
        ShowGrabGuidanceForDuration(10f);
        grabItemExample.SetActive(true);
        grabItemCheck = grabItemExample.GetComponentInChildren<Tutorial_GrabItemCheck>();
        GrabInteractObjectManager.Instance.onDropedObject += OnObjectDroped; // Subscribe to the drop event

    }

    public void ShowGrabGuidanceForDuration(float duration)
    {
        grabGuadanceCanvas.SetActive(true);
        StartCoroutine(DelayHideGrabGuidance(duration));
    }

    IEnumerator DelayHideGrabGuidance(float duration)
    {
        yield return new WaitForSeconds(duration);
        grabGuadanceCanvas.SetActive(false);
    }

    private void OnObjectDroped()
    {
        Debug.Log("Object has been dropped. Checking if the grab item task is finished.");
        if (grabItemCheck.CheckIsFinished())
        {
            Debug.Log("Grab item task is finished.");
            if (audioSource != null && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
            OnGrabItemTaskFinished();
        }
        else
        {
            Debug.Log("Grab item task is not finished yet.");
        }
    }

    private void OnGrabItemTaskFinished()
    {
        Debug.Log("Grab item task finished.");
        grabItemExample.SetActive(false); // Hide the grab item example
        GrabInteractObjectManager.Instance.onDropedObject -= OnObjectDroped; // Unsubscribe from the drop event

        // Show ending UI
        tutorialEndingUI.SetActive(true);
        startExperimentButton = tutorialEndingUI.GetComponentInChildren<Button>();
        startExperimentButton.onClick.AddListener(OnStartExperimentButtonClicked); // Bind the button click event
    }

    private void OnStartExperimentButtonClicked()
    {
        Debug.Log("Start Experiment button clicked. Proceeding to the experiment.");
        experimentManager.TurnToSceneByName("L_LLMScene_1_GatherItem"); // Replace with your actual scene name
    }


}
