using UnityEngine;

namespace BlockPuzzleGame
{
    [ExecuteAlways]
    public class ConditionManager : MonoBehaviour
    {
        public Condition selectedMode;

        public GameObject baselineObject;
        public GameObject unidirecInputObject;
        public GameObject unidirecOutputObject;
        public GameObject bidirectionalObject;

        public void SetMode(Condition mode)
        {
            selectedMode = mode;
            ApplyMode();
        }

        private void OnValidate()
        {
            ApplyMode();
        }

        private void ApplyMode()
        {
            if (!Application.isPlaying && !Application.isEditor)
                return;

            if (baselineObject != null) baselineObject.SetActive(selectedMode == Condition.Baseline);
            if (unidirecInputObject != null) unidirecInputObject.SetActive(selectedMode == Condition.UnidirecInput);
            if (unidirecOutputObject != null) unidirecOutputObject.SetActive(selectedMode == Condition.UnidirecOutput);
            if (bidirectionalObject != null) bidirectionalObject.SetActive(selectedMode == Condition.Bidirectional);
        }
    }
}