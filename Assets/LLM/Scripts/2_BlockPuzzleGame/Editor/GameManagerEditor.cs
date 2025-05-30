#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlockPuzzleGame
{
    [CustomEditor(typeof(GameManager))]
    public class GameManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GameManager gameManager = (GameManager)target;

            EditorGUILayout.Space(10);

            CenteredButton("Create Grabbable Box", () => gameManager.CreateGrabbableBox());
            CenteredButton("Create Goal", () => gameManager.CreateGoal());
            CenteredButton("Check Task Progress", () => gameManager.ShowTaskProgress());
        }

        private void CenteredButton(string label, System.Action onClick)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(label, GUILayout.Width(200), GUILayout.Height(30)))
            {
                onClick?.Invoke();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}
#endif
