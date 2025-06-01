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
}
