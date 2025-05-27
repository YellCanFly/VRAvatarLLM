using UnityEngine;

public class GrabInteractObjectManager : MonoBehaviour
{
    public static GrabInteractObjectManager Instance { get; private set; }

    public string CurrentHeldObjectName { get; private set; }

    [SerializeField, Tooltip("Name of the currently held object (for debug)")]
    private string _debugHeldObjectName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetHeldObject(GameObject obj)
    {
        CurrentHeldObjectName = obj.name;
        _debugHeldObjectName = obj.name;
    }

    public void ClearHeldObject()
    {
        CurrentHeldObjectName = null;
        _debugHeldObjectName = "";
    }
}
