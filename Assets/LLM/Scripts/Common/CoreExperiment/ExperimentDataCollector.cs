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

    public static void SaveTaskDataToJson(TaskData_CollectItem taskData, string fileName)
    {
        // 1. 序列化为格式化 JSON 字符串
        string json = JsonConvert.SerializeObject(taskData, Formatting.Indented);

        // 2. 构造保存路径（你可以替换为其他路径）
        string path = Path.Combine(Application.persistentDataPath, fileName);

        // 3. 写入文件
        File.WriteAllText(path, json);

        Debug.Log($"Task data saved to {path}");
    }

    public static async Task SaveTaskDataToJsonAsync(TaskData_CollectItem taskData, string fileName)
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

    [ContextMenu("Test Save TaskData")]
    public void TestSaveTaskData()
    {
        TaskData_CollectItem testData = new TaskData_CollectItem();
        testData.participantID = 1;
        testData.condition = InteractCondition.Baseline;

        // 添加一些行为帧数据
        for (int i = 0; i < 5; i++)
        {
            UserData_BehaviorFrame frame = new UserData_BehaviorFrame
            {
                timeStamp = Time.time + i,
                headPosition = new SerializableVector3(new Vector3(i, i + 0.5f, i + 1f)),
                headRotation = new SerializableVector3(new Vector3(0, 45 * i, 0)),
                leftHandPosition = new SerializableVector3(new Vector3(i, 0, 0)),
                leftHandRotation = new SerializableVector3(new Vector3(0, 0, i * 10)),
                rightHandPosition = new SerializableVector3(new Vector3(0, i, 0)),
                rightHandRotation = new SerializableVector3(new Vector3(0, i * 5, 0)),
                eyeTrackingRotation = new SerializableQuaternion(Quaternion.Euler(0, i * 10, 0))
            };
            testData.behaviorFrames.Add(frame);
        }


        // 添加对话帧数据
        for (int i = 0; i < 3; i++)
        {
            ConversationData_MessageFrame msgFrame = new ConversationData_MessageFrame
            {
                sentTime = Time.time + i,
                startRecordingTime = Time.time + i - 0.5f,
                message = new Message(Role.User, $"这是测试消息 {i}")
            };
            testData.conversationFrames.Add(msgFrame);
        }

        // 保存
        SaveTaskDataToJson(testData, "test_task_data.json");
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

    [JsonProperty("behavior_frames")]
    public List<UserData_BehaviorFrame> behaviorFrames = new List<UserData_BehaviorFrame>();

    [JsonProperty("conversation_frames")]
    public List<ConversationData_MessageFrame> conversationFrames = new List<ConversationData_MessageFrame>();
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



