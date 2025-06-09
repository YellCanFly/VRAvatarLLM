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
        // Calculate the relative position of this object to the camera
        Vector3 relativePosition = transform.position - cameraPosition;

        RelativePosition result = new RelativePosition(
            x: Vector3.Dot(relativePosition, camTransform.right),
            y: Vector3.Dot(relativePosition, camTransform.up),
            z: Vector3.Dot(relativePosition, camTransform.forward)
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
    public float y;
    public float z;
    public RelativePosition(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}
