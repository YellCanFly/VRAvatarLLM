using UnityEngine;

public class GatherItemIconManager : MonoBehaviour
{
    public GameObject gatherItemIconPrefab; // Prefab for the gather item icon

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GatherItemIcon CreateIcon(Sprite itemIcon)
    {
        // Create a new GameObject for the icon
        GameObject img = Instantiate(gatherItemIconPrefab, transform);
        GatherItemIcon icon = img.GetComponent<GatherItemIcon>();
        if (icon != null)
        {
            // Set the icon image to the provided item icon
            icon.SetIconImage(itemIcon);
            return icon;
        }
        else
        {
            Debug.LogError("GatherItemIcon component not found on the instantiated prefab.");
            return null;
        }

    }
}
