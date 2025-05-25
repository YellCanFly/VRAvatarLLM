using UnityEngine;

[RequireComponent(typeof(InteractObject))]
public class GatherItemObject : MonoBehaviour
{
    public Sprite itemIcon; // Reference to the icon sprite for the item
    public GatherItemIcon gatherItemIconWidget; // Reference to the GatherItemIcon component for displaying the item icon

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetItemIconGathered()
    {
        if (itemIcon != null)
        {
            // Set the icon to indicate that the item has been gathered
            gatherItemIconWidget.SetIconToGathered();
        }
        else
        {
            Debug.LogWarning("Item icon is not set for " + gameObject.name);
        }
    }
}
