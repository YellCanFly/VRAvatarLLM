#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(PrefabSpawner))]
public class PrefabSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PrefabSpawner spawner = (PrefabSpawner)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Spawn Prefabs in Editor"))
        {
            spawner.Spawn();
        }

        if (GUILayout.Button("Clear Spawned Prefabs"))
        {
            spawner.Clear();
        }
    }
}
#endif
