using System.Collections;
using UnityEngine;

public class InteractObject : MonoBehaviour
{
    public string objectName = "Object"; // Name of the object
    public string objectDescription = "Description"; // Description of the object

    private Material mat;
    private Color baseColor;
    public Color gazeHightlightColor = Color.red;
    public float beGazedDuration = 0f;

    public ObjectColliderRange colliderRange;

    private void OnEnable()
    {
        InteractObjectManager.Instance?.Register(this); // Register this object with the manager
    }

    private void OnDisable()
    {
        InteractObjectManager.Instance?.Unregister(this); // Unregister this object from the manager
    }

    private void Awake()
    {
        objectName = gameObject.name;
        colliderRange = GetComponentInChildren<ObjectColliderRange>();

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

    public void SetObjectToHeightLightColor()
    {
        // Change the color of the object
        mat.color = baseColor * gazeHightlightColor;
    }

    public void  ResetObjectToDefaultColor()
    {
        // Reset the color to white
        mat.color = baseColor;
    }

    public RelativePosition GetRelativePositionToCamera(Transform camTransform)
    {
        // Get the main camera's position
        Vector3 cameraPosition = camTransform.position;
        cameraPosition.y = 0.0f;
        // Calculate the relative position of this object to the camera
        Vector3 objPos = transform.position;
        objPos.y = 0.0f; // Ignore the y-axis for horizontal plane calculation
        Vector3 relativePosition = objPos - cameraPosition;

        Vector3 right = camTransform.right;
        Vector3 forward = camTransform.forward;
        right.y = 0.0f; // Ignore the y-axis for horizontal plane calculation
        forward.y = 0.0f; // Ignore the y-axis for horizontal plane calculation

        RelativePosition result = new RelativePosition(
            x: Vector3.Dot(relativePosition, right),
            height: transform.position.y, // Use the object's y position as height
            z: Vector3.Dot(relativePosition, forward)
            );

        return result;
    }

    public Bounds GetObjectBounds()
    {
        if (colliderRange != null)
        {
            return colliderRange.bounds;
        }
        else
        {
            Debug.LogWarning("ObjectColliderRange is not set for " + gameObject.name);
            return new Bounds(transform.position, Vector3.one); // Return a default bounds if not set
        }
    }
}

[System.Serializable]
public struct RelativePosition
{
    public float x;
    public float height;
    public float z;
    public RelativePosition(float x, float height, float z)
    {
        const float pivotOffset = 0.2f; // Offset to adjust the height
        this.x = x;
        this.height = height - pivotOffset; // height only used in task 2
        this.z = z;
    }
}
