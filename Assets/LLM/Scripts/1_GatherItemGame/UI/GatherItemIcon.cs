using UnityEngine;
using UnityEngine.UI;

public class GatherItemIcon : MonoBehaviour
{
    public Image iconImageWidget; // Reference to the UI Image component for the icon
    public Sprite itemIconSpite; // Reference to the icon sprite

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetIconImage(Sprite itemIcon)
    {
        itemIconSpite = itemIcon; // Set the icon sprite
        iconImageWidget.sprite = itemIconSpite; // Update the UI Image component with the new sprite
        iconImageWidget.color = Color.white; // Reset the icon color to white
    }

    public void SetIconToGathered()
    {
        iconImageWidget.color = Color.gray; // Change the icon color to gray to indicate it has been gathered
    }
}
