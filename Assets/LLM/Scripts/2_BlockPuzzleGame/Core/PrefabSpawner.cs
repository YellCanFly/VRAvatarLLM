using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TMPro;

namespace BlockPuzzleGame
{
    public class PrefabSpawner : MonoBehaviour
    {
        public GameObject prefab;
        public List<Transform> parentObjects = new List<Transform>();
        public int numberPerParent = 5;

        public float xSpacing = 2f;
        public float ySpacing = 0f;
        public float zSpacing = 0f;

        public List<Transform> positionLists = new List<Transform>();

        [System.Serializable]
        public class NamedColor
        {
            public string colorName;
            public Color color;
        }

        public List<NamedColor> randomColors = new List<NamedColor>();

        [HideInInspector]
        public List<GameObject> spawnedObjects = new List<GameObject>();

        [ContextMenu("Spawn Prefabs")]
        public void Spawn()
        {
#if UNITY_EDITOR
            Clear();

            List<int> shuffledIndices = new List<int>();

            if (positionLists != null && positionLists.Count > 0)
            {
                for (int i = 1; i <= positionLists.Count; i++) shuffledIndices.Add(i);
                Shuffle(shuffledIndices);

                for (int i = 0; i < positionLists.Count; i++)
                {
                    Transform targetPos = positionLists[i];
                    GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (obj == null || targetPos == null) continue;

                    obj.transform.SetParent(targetPos, false);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;

                    int targetNumber = shuffledIndices[i];
                    obj.name = $"Target{targetNumber}";

                    // AnswerTracker があれば targetName を設定
                    AnswerTracker tracker = targetPos.GetComponent<AnswerTracker>();
                    if (tracker != null)
                    {
                        tracker.locationName = obj.name;
                    }

                    TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null) tmp.text = $"{targetNumber}";

                    ApplyRandomColor(obj);
                    spawnedObjects.Add(obj);
                }
            }
            else
            {
                int totalCount = parentObjects.Count * numberPerParent;
                for (int i = 1; i <= totalCount; i++) shuffledIndices.Add(i);
                Shuffle(shuffledIndices);

                int index = 0;
                foreach (Transform parent in parentObjects)
                {
                    for (int i = 0; i < numberPerParent; i++)
                    {
                        GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (obj == null) continue;

                        obj.transform.SetParent(parent, false);

                        float offset = (numberPerParent - 1) * 0.5f;
                        float x = (i - offset) * xSpacing;
                        float y = (i - offset) * ySpacing;
                        float z = (i - offset) * zSpacing;

                        obj.transform.localPosition = new Vector3(x, y, z);

                        int targetNumber = shuffledIndices[index++];
                        obj.name = $"Target{targetNumber}";

                        // AnswerTracker があれば targetName を設定
                        AnswerTracker tracker = parent.GetComponent<AnswerTracker>();
                        if (tracker != null)
                        {
                            tracker.locationName = obj.name;
                        }

                        TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (tmp != null) tmp.text = $"{targetNumber}";

                        ApplyRandomColor(obj);
                        spawnedObjects.Add(obj);
                    }
                }
            }
#endif
        }

        void ApplyRandomColor(GameObject obj)
        {
            if (randomColors == null || randomColors.Count == 0) return;

            NamedColor selected = randomColors[Random.Range(0, randomColors.Count)];

            Renderer rend = obj.GetComponentInChildren<Renderer>(true);
            if (rend != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Material copiedMat = new Material(rend.sharedMaterial);
                    copiedMat.color = selected.color;
                    rend.sharedMaterial = copiedMat;
                }
                else
#endif
                {
                    rend.material.color = selected.color;
                }
            }
            ObjectInfo info = obj.GetComponent<ObjectInfo>();
            if (info != null)
            {
                info.SetColor(selected.colorName);
            }
        }

        void Shuffle(List<int> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = Random.Range(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        [ContextMenu("Clear Spawned Prefabs")]
        public void Clear()
        {
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }
            spawnedObjects.Clear();
        }
    }
}
