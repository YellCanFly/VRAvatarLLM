using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI;


public class ExperimentDataCollector : MonoBehaviour
{
    public GameObject avatarHeadTransform;
    public GameObject avatarLeftHandTransform;
    public GameObject avatarRightHandTransform;
    public GameObject avatarGazeTransform;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static async Task SaveTaskDataToJsonAsync<T>(T taskData, string fileName)
    {
        // 1. 序列化为格式化 JSON 字符串（此处仍是同步处理）
        string json = JsonConvert.SerializeObject(taskData, Formatting.Indented);

        // 2. 构造保存路径
        string path = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            // 3. 异步写入文件
            await File.WriteAllTextAsync(path, json);

            Debug.Log($"Task data saved to {path}");
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to save task data: {e.Message}");
        }
    }

    public UserData_BehaviorFrame GetCurrentUserBehaviorFrame()
    {
        return new UserData_BehaviorFrame
        {
            timeStamp = Time.time,
            headPosition = new SerializableVector3(avatarHeadTransform.transform.position),
            headRotation = new SerializableVector3(avatarHeadTransform.transform.rotation.eulerAngles),
            leftHandPosition = new SerializableVector3(avatarLeftHandTransform.transform.position),
            leftHandRotation = new SerializableVector3(avatarLeftHandTransform.transform.rotation.eulerAngles),
            rightHandPosition = new SerializableVector3(avatarRightHandTransform.transform.position),
            rightHandRotation = new SerializableVector3(avatarRightHandTransform.transform.rotation.eulerAngles),
            eyeTrackingRotation = new SerializableQuaternion(avatarGazeTransform.transform.rotation)
        };
    }
}

[System.Serializable]
public class TaskData_CollectItem
{
    [JsonProperty("participant_id")]
    public int participantID;

    [JsonProperty("condition")]
    public InteractCondition condition;

    [JsonProperty("system_prompt")]
    public string systemPrompt;

    [JsonProperty("behavior_frames")]
    public List<UserData_BehaviorFrame> behaviorFrames = new List<UserData_BehaviorFrame>();

    [JsonProperty("current_target_records")]
    public List<CollectItemData_CurrentTargetRecords> currentTargetRecords = new List<CollectItemData_CurrentTargetRecords>();

    [JsonProperty("conversation_frames")]
    public List<ConversationData_MessageFrame> conversationFrames = new List<ConversationData_MessageFrame>();
}

[System.Serializable]
public class CollectItemData_CurrentTargetRecords
{
    [JsonProperty("time_stamp")]
    public float timeStamp;

    [JsonProperty("target_object_name")]
    public string targetObjectName;
}

[System.Serializable]
public class UserData_BehaviorFrame
{
    [JsonProperty("time_stamp")]
    public float timeStamp;

    [JsonProperty("head_position")]
    public SerializableVector3 headPosition;

    [JsonProperty("head_rotation")]
    public SerializableVector3 headRotation;

    [JsonProperty("left_hand_position")]
    public SerializableVector3 leftHandPosition;

    [JsonProperty("left_hand_rotation")]
    public SerializableVector3 leftHandRotation;

    [JsonProperty("right_hand_position")]
    public SerializableVector3 rightHandPosition;

    [JsonProperty("right_hand_rotation")]
    public SerializableVector3 rightHandRotation;

    [JsonProperty("eye_tracking_rotation")]
    public SerializableQuaternion eyeTrackingRotation;
}

[System.Serializable]
public class ConversationData_MessageFrame
{
    [JsonProperty("start_recording_time")]
    public float startRecordingTime;

    [JsonProperty("sent_time")]
    public float sentTime;

    [JsonProperty("message")]
    public Message message;
}

[System.Serializable]
public class TaskData_BlockPuzzle
{
    [JsonProperty("participant_id")]
    public int participantID;

    [JsonProperty("condition")]
    public InteractCondition condition;

    [JsonProperty("system_prompt")]
    public string systemPrompt;

    [JsonProperty("target_places_info")]
    public List<BlockPuzzleData_TargetPlaceInfo> targetPlacesInfo = new List<BlockPuzzleData_TargetPlaceInfo>();

    [JsonProperty("behavior_frames")]
    public List<UserData_BehaviorFrame> behaviorFrames = new List<UserData_BehaviorFrame>();

    [JsonProperty("block_puzzle_place_records")]
    public List<BlockPuzzleData_PlaceRecord> blockPuzzlePlaceRecords = new List<BlockPuzzleData_PlaceRecord>();

    [JsonProperty("conversation_frames")]
    public List<ConversationData_MessageFrame> conversationFrames = new List<ConversationData_MessageFrame>();
}

[System.Serializable]
public class BlockPuzzleData_PlaceRecord
{
    [JsonProperty("time_stamp")]
    public float time;

    [JsonProperty("is_correct")]
    public bool isCorrect;

    [JsonProperty("place_name")]
    public string placeName;

    [JsonProperty("object_name")]
    public string objectName;
}

[System.Serializable]
public class BlockPuzzleData_TargetPlaceInfo
{
    [JsonProperty("place_name")]
    public string placeName;

    [JsonProperty("target_position")]
    public SerializableVector3 targetPosition;
}

[System.Serializable]
public struct SerializableVector3
{
    public float x, y, z;
    public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[System.Serializable]
public struct SerializableQuaternion
{
    public float x, y, z, w;
    public SerializableQuaternion(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
}



