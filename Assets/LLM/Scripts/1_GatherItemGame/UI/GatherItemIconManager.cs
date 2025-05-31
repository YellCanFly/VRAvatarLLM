using UnityEngine;
using UnityEngine.UI;
using VRUIP;

public class GatherItemIconManager : MonoBehaviour
{
    public static GatherItemIconManager Instance { get; private set; }

    public GameObject gatherItemIconPrefab; // Prefab for the gather item icon

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

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

        RectTransform imgCanvas = img.GetComponent<RectTransform>();
        if (imgCanvas != null)
        {
            imgCanvas.anchorMin = new Vector2(0.5f, 0.5f); // Set anchor to center
            imgCanvas.anchorMax = new Vector2(0.5f, 0.5f); // Set anchor to center
            imgCanvas.pivot = new Vector2(0.5f, 0.5f); // Set the pivot to center
            imgCanvas.sizeDelta = new Vector2(150, 150); // Set a default size for the icon
        }
        else
        {
            Debug.LogError("Image component not found on the instantiated prefab.");
        }

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
