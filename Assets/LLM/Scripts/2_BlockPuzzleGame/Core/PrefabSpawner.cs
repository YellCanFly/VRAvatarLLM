using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabSpawner : MonoBehaviour
{
    public GameObject prefab;
    public List<Transform> parentObjects = new List<Transform>();
    public int numberPerParent = 5;

    public float xSpacing = 2f;
    public float ySpacing = 0f; // Optional
    public float zSpacing = 0f; // Optional

    [HideInInspector]
    public List<GameObject> spawnedObjects = new List<GameObject>();

    [ContextMenu("Spawn Prefabs")]
    public void Spawn()
    {
        Clear();

        int totalCount = parentObjects.Count * numberPerParent;
        List<int> shuffledIndices = new List<int>();
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
                obj.name = $"Target{shuffledIndices[index++]}";
                spawnedObjects.Add(obj);
            }
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
