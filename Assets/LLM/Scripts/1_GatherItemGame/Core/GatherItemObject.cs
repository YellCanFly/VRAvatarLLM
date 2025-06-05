using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractObject))]
public class GatherItemObject : MonoBehaviour
{
    public InteractObject interactObject;

    private void Awake()
    {
        interactObject = GetComponent<InteractObject>();
    }

    public void SetItemToRenderLayer()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.gameObject.layer = LayerMask.NameToLayer("RenderTarget"); // Set the layer for rendering
        }
    }

    public void SetItemToDefaultLayer()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.gameObject.layer = LayerMask.NameToLayer("Default"); // Set the layer back to default
        }
    }

    public GatherItemObjectInfo GetGrabItemInfo()
    {
        GatherItemObjectInfo result = new();
        result.object_name = interactObject.objectName;
        result.object_description = interactObject.objectDescription;
        //result.object_relative_position_to_uesr = interactObject.GetRelativePositionToCamera(Camera.main.transform);
        //result.object_relative_position_to_avatar = interactObject.GetRelativePositionToCamera(GatherItemManager.Instance.GetActivateAvatarHeadTransform());

        result.object_relative_position_to_uesr = interactObject.GetRelativePositionToCamera(GatherItemManager.Instance.gazeSphereDetector.transform);
        result.object_relative_position_to_avatar = interactObject.GetRelativePositionToCamera(GatherItemManager.Instance.avatarAcitivate.transform);
        result.object_size = interactObject.GetObjectBounds().size;

        return result;
    }

    public GatherItemObjectInfoInt GetGrabItemInfoInt()
    {
        GatherItemObjectInfoInt result = new();
        result.object_name = interactObject.objectName;
        result.object_description = interactObject.objectDescription;
        //result.object_relative_position_to_uesr = interactObject.GetRelativePositionToCamera(Camera.main.transform);
        //result.object_relative_position_to_avatar = interactObject.GetRelativePositionToCamera(GatherItemManager.Instance.GetActivateAvatarHeadTransform());

        var rel_pos_to_user = interactObject.GetRelativePositionToCamera(GatherItemManager.Instance.gazeSphereDetector.transform);
        result.object_relative_position_to_uesr = new Vector3Int()
        {
            x = (int)(rel_pos_to_user.x * 100),
            y = (int)(rel_pos_to_user.y * 100),
            z = (int)(rel_pos_to_user.z * 100)
        };
        var rel_pos_to_avatar = interactObject.GetRelativePositionToCamera(GatherItemManager.Instance.avatarAcitivate.transform);
        result.object_relative_position_to_avatar = new Vector3Int()
        {
            x = (int)(rel_pos_to_avatar.x * 100),
            y = (int)(rel_pos_to_avatar.y * 100),
            z = (int)(rel_pos_to_avatar.z * 100)
        };
        var obj_size_m = interactObject.GetObjectBounds().size;
        result.object_size = new Vector3Int()
        {
            x = (int)(obj_size_m.x * 100),
            y = (int)(obj_size_m.y * 100),
            z = (int)(obj_size_m.z * 100)
        };

        return result;
    }
}

[System.Serializable]
public class GatherItemObjectInfo
{
    
    [JsonProperty("object_name")]
    public string object_name;

    [JsonProperty("object_description")]
    public string object_description;

    [JsonProperty("object_relative_position_to_user")]
    public RelativePosition object_relative_position_to_uesr;

    [JsonProperty("object_relative_position_to_avatar")]
    public RelativePosition object_relative_position_to_avatar;

    [JsonProperty("object_size")]
    public Vector3 object_size;

}

[System.Serializable]
public class GatherItemObjectInfoInt
{

    [JsonProperty("object_name")]
    public string object_name;

    [JsonProperty("object_description")]
    public string object_description;

    [JsonProperty("object_relative_position_to_user")]
    public Vector3Int object_relative_position_to_uesr;

    [JsonProperty("object_relative_position_to_avatar")]
    public Vector3Int object_relative_position_to_avatar;

    [JsonProperty("object_size")]
    public Vector3Int object_size;

}

[System.Serializable]
public class ObjectsInfo
{
    [JsonProperty("objects_info")]
    public List<GatherItemObjectInfoInt> objects_info = new();
}
