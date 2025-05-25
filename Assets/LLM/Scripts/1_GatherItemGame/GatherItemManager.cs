using UnityEngine;

public class GatherItemManager : MonoBehaviour
{
    public GatherItemObject[] targetGatherObjects;
    public GatherItemIconManager iconManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (iconManager == null)
        {
            Debug.LogError("GatherItemIconManager is not assigned in GatherItemManager.");
            return;
        }

        foreach (var item in targetGatherObjects)
        {
            if (item != null)
            {
                // Create an icon for each gatherable item
                GatherItemIcon icon = iconManager.CreateIcon(item.itemIcon);
                item.gatherItemIcon = icon;
            }
            else
            {
                Debug.LogWarning("GatherItemObject component not found on " + item.name);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
