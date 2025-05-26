using System.Collections;
using UnityEngine;

public class InteractObject : MonoBehaviour
{
    public string objectName = "Object"; // Name of the object
    public string objectDescription = "Description"; // Description of the object

    private Material mat;
    private Color baseColor;
    public Color gazeHightlightColor = Color.red;
    bool isGazed = false; // Flag to check if the object is gazed at
    public bool IsGazed => isGazed; // Public property to access the gazed state

    private void OnDisable()
    {
        InteractObjectManager.Instance?.Unregister(this); // Unregister this object from the manager
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectName = gameObject.name;
        InteractObjectManager.Instance?.Register(this); // Register this object with the manager
        InitDefaultColor();
    }

    private void InitDefaultColor()
    {
        mat = GetComponentInChildren<Renderer>().material;
        if (mat.HasProperty("_BaseColor"))
            baseColor = mat.GetColor("_BaseColor");
        else if (mat.HasProperty("_Color"))
            baseColor = mat.GetColor("_Color");
        else
            baseColor = Color.white; // fallback
    }

    public void ChangeColorForDuration(Color color, float duration)
    {
        // Set the flag to true when the object is gazed at
        isGazed = true;
        // Change the color of the object
        mat.color = baseColor * gazeHightlightColor;
        // Start a coroutine to reset the color after the duration
        StartCoroutine(ResetColorAfterDuration(duration, baseColor));
    }

    IEnumerator ResetColorAfterDuration(float duration, Color defaultColor)
    {
        // Wait for the specified duration
        yield return new WaitForSeconds(duration);
        // Reset the color to white
        mat.color = defaultColor;
        // Reset the flag to false after the duration
        isGazed = false; 
    }

    public RelativePosition GetRelativePositionToCamera(Transform camTransform)
    {
        // Get the main camera's position
        Vector3 cameraPosition = camTransform.position;
        // Calculate the relative position of this object to the camera
        Vector3 relativePosition = transform.position - cameraPosition;

        RelativePosition result = new RelativePosition(
            x: Vector3.Dot(relativePosition, camTransform.right),
            y: Vector3.Dot(relativePosition, camTransform.up),
            z: Vector3.Dot(relativePosition, camTransform.forward)
            );

        return result;
    }
}

[System.Serializable]
public struct RelativePosition
{
    public float x;
    public float y;
    public float z;
    public RelativePosition(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}
