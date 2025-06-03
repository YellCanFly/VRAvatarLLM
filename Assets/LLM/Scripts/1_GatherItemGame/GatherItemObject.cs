using Newtonsoft.Json;
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
        result.object_relative_position_to_uesr = interactObject.GetRelativePositionToCamera(Camera.main.transform);
        result.object_relative_position_to_avatar = interactObject.GetRelativePositionToCamera(GatherItemManager.Instance.GetActivateAvatarHeadTransform());
        result.object_size = interactObject.GetObjectBounds().size;

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
