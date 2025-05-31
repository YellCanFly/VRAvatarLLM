using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;


public class ParticipantInitInput : MonoBehaviour
{
    [Header("Canvas Settings")]
    public GameObject virtualKeyboard;
    public GameObject idInputCanvas;
    public GameObject idConfirmedCanvas;
    public GameObject idInvalidCanvas;

    
    [Header("Interact Items Settings")]
    public TMP_InputField idInputField;
    public Button confirmButton;

    private ExperimentManager experimentManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize the experiment manager
        experimentManager = FindFirstObjectByType<ExperimentManager>();
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found in the scene. Please ensure it is present.");
            return;
        }

        // Initialize the participant ID input canvas and virtual keyboard
        virtualKeyboard.SetActive(true);

        // Bind the confirm button click event
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    public bool CheckParticipantIDInputValid()
    {
        bool result = false;
        if (int.TryParse(idInputField.text, out int participantID))
        {
            if (participantID >= 1 && participantID <= ExperimentManager.participantCount)
            {
                result = true;
            }
            else
            {
                Debug.LogWarning("Participant ID must be between 0 and " + ExperimentManager.participantCount);
            }
        }
        else
        {
            Debug.LogWarning("Invalid Participant ID input. Please enter a valid integer.");
        }

        return result;
    }

    public void OnConfirmButtonClicked()
    {
        if (CheckParticipantIDInputValid())
        {
            
            int participantID = int.Parse(idInputField.text);
            ExperimentManager.participantID = participantID;
            experimentManager.onParticipantIDConfirmed?.Invoke();


            StartCoroutine(ShowIDConfirmedCanvasForDuration(1f));
            Debug.Log("Participant ID set to: " + idInputField.text);
        }
        else
        {
            Debug.Log("Invalid Participant ID input: " + idInputField.text);
            idInputField.text = ""; // Clear the input field
            StartCoroutine(ShowIDInvalidCanvasForDuration(1f));
            Debug.LogError("Please enter a valid Participant ID.");
        }

        Debug.Log("Confirmed button is clicked");
    }

    IEnumerator ShowIDConfirmedCanvasForDuration(float duration)
    {
        idConfirmedCanvas.SetActive(true);
        idInputCanvas.SetActive(false);
        virtualKeyboard.SetActive(false);

        yield return new WaitForSeconds(duration);

        idConfirmedCanvas.SetActive(false);
    }

    IEnumerator ShowIDInvalidCanvasForDuration(float duration)
    {
        idInvalidCanvas.SetActive(true);
        idInputField.interactable = false;
        confirmButton.interactable = false;

        yield return new WaitForSeconds(duration);

        idInvalidCanvas.SetActive(false);
        idInputField.interactable = true;
        confirmButton.interactable = true;
    }
}
