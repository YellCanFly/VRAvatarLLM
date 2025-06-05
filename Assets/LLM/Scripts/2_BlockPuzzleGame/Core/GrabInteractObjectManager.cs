using UnityEngine;
using UnityEngine.Events;

namespace BlockPuzzleGame
{
    public class GrabInteractObjectManager : MonoBehaviour
    {
        public static GrabInteractObjectManager Instance { get; private set; }

        public ObjectInfo CurrentHeldObject;

        public UnityAction onHeldObject;
        public UnityAction onDropedObject;

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

        public void SetHeldObject(ObjectInfo obj)
        {
            if (obj == null)
            {
                ClearHeldObject();
                return;
            }

            CurrentHeldObject = obj;
            onHeldObject?.Invoke();

            // Log the name of the held object for debugging
            Debug.Log($"Held Object Set: {obj.ObjectName}");
        }

        public void ClearHeldObject()
        {
            if (CurrentHeldObject != null)
            {
                Debug.Log($"Clearing Held Object: {CurrentHeldObject.ObjectName}");
                onDropedObject?.Invoke();
            }
            CurrentHeldObject = null;
        }
    }
}
