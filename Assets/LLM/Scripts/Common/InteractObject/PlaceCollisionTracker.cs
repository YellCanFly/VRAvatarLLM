using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BlockPuzzleGame{
    public class PlaceCollisionTracker : MonoBehaviour
    {
        public float detectionHeight = 0.2f;
        public LayerMask detectableLayer;
        public List<GameObject> objectsOnPlane = new List<GameObject>();
        private List<GameObject> objectsOnPlaneRecord = new List<GameObject>();

        private MeshCollider meshCollider;

        public UnityAction<GameObject> onObjectPlaced;

        void Start()
        {
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError("Need MeshCollider component on the GameObject.");
            }
        }

        void Update()
        {
            UpdateObjectsOnPlane();
        }

        void UpdateObjectsOnPlane()
        {
            objectsOnPlane.Clear();

            Bounds bounds = meshCollider.bounds;

            Vector3 center = bounds.center + Vector3.up * (detectionHeight / 2);
            Vector3 halfExtents = new Vector3(bounds.extents.x, detectionHeight / 2, bounds.extents.z);

            Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, detectableLayer);

            foreach (var col in colliders)
            {
                if (!objectsOnPlane.Contains(col.gameObject))
                {
                    objectsOnPlane.Add(col.gameObject);
                }

                if (!objectsOnPlaneRecord.Contains(col.gameObject))
                {
                    onObjectPlaced?.Invoke(col.gameObject);
                }
            }

            objectsOnPlaneRecord.Clear();
            foreach (var obj in objectsOnPlane)
            {
                objectsOnPlaneRecord.Add(obj);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (meshCollider == null) return;

            Bounds bounds = meshCollider.bounds;
            Vector3 center = bounds.center + Vector3.up * (detectionHeight / 2);
            Vector3 size = new Vector3(bounds.size.x, detectionHeight, bounds.size.z);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
