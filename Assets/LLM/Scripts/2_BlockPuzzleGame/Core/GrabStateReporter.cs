using UnityEngine;
using Oculus.Interaction;

public class GrabbableReporter : MonoBehaviour
{
    private Grabbable _grabbable;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            GrabInteractObjectManager.Instance?.SetHeldObject(gameObject.transform.parent.gameObject);
        }
        else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel)
        {
            GrabInteractObjectManager.Instance?.ClearHeldObject();
        }
    }
}
