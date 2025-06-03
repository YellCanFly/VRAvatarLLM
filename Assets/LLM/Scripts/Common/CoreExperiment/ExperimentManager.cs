using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExperimentManager : MonoBehaviour
{
    public static ExperimentManager Instance;
    public static int participantCount = 24; // Total number of participants in the experiment


    public int participantID; // Participant ID for the experiment
    public List<InteractCondition> conditionOrder;

    public UnityAction onParticipantIDConfirmed;


    private static readonly InteractCondition[] Conditions = {
        InteractCondition.Baseline,
        InteractCondition.UniDirectional_Input,
        InteractCondition.UniDirectional_Output,
        InteractCondition.BiDirectional
    };

    private static readonly int[][] latinSquare = {
        new int[] { 1, 2, 3, 4 },
        new int[] { 2, 1, 4, 3 },
        new int[] { 3, 4, 1, 2 },
        new int[] { 4, 3, 1, 1 }
    };

    public static List<InteractCondition> GetConditionOrder(int participantID)
    {
        if (participantID < 1 || participantID > participantCount)
        {
            Debug.LogError("Participant ID must be between 1 and " + participantCount);
            return null;
        }

        int baseIndex = (participantID - 1) % Conditions.Length;
        List<InteractCondition> order = new();

        for (int i = 0; i < Conditions.Length; i++)
        {
            int idx = latinSquare[baseIndex][i] - 1;
            order.Add(Conditions[idx]);
        }

        return order;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        onParticipantIDConfirmed += OnParticipantIDConfirmed;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }


    public void OnParticipantIDConfirmed()
    {
        onParticipantIDConfirmed -= OnParticipantIDConfirmed;
        conditionOrder = GetConditionOrder(participantID);
    }

    public void TurnToSceneByName(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }


}
