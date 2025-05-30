using UnityEditor;
using UnityEngine;

namespace BlockPuzzleGame{
    [CustomEditor(typeof(PrefabSpawner))]
    public class PrefabSpawnerEditor : Editor
    {
        SerializedProperty prefab;
        SerializedProperty parentObjects;
        SerializedProperty numberPerParent;
        SerializedProperty xSpacing;
        SerializedProperty ySpacing;
        SerializedProperty zSpacing;
        SerializedProperty positionLists;
        SerializedProperty randomColors;

        bool showParentList = true;
        bool showPositionList = true;
        bool showColorList = true;

        void OnEnable()
        {
            prefab = serializedObject.FindProperty("prefab");
            parentObjects = serializedObject.FindProperty("parentObjects");
            numberPerParent = serializedObject.FindProperty("numberPerParent");
            xSpacing = serializedObject.FindProperty("xSpacing");
            ySpacing = serializedObject.FindProperty("ySpacing");
            zSpacing = serializedObject.FindProperty("zSpacing");
            positionLists = serializedObject.FindProperty("positionLists");
            randomColors = serializedObject.FindProperty("randomColors");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Prefab Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prefab);
            EditorGUILayout.PropertyField(numberPerParent);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Spacing Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(xSpacing);
            EditorGUILayout.PropertyField(ySpacing);
            EditorGUILayout.PropertyField(zSpacing);

            EditorGUILayout.Space(5);
            showParentList = EditorGUILayout.Foldout(showParentList, "Parent Objects (Grid Spawn)");
            if (showParentList)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(parentObjects, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            showPositionList = EditorGUILayout.Foldout(showPositionList, "Position Lists (Direct Placement)");
            if (showPositionList)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(positionLists, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            showColorList = EditorGUILayout.Foldout(showColorList, "Random Color Options");
            if (showColorList)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(randomColors, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Spawn Prefabs"))
            {
                ((PrefabSpawner)target).Spawn();
            }

            if (GUILayout.Button("Clear Spawned Prefabs"))
            {
                ((PrefabSpawner)target).Clear();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
