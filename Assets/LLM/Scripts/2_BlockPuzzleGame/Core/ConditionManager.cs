using UnityEngine;

namespace BlockPuzzleGame
{
    [ExecuteAlways]
    public class ConditionManager : MonoBehaviour
    {
        public InteractCondition selectedMode;

        public GameObject baselineObject;
        public GameObject unidirecInputObject;
        public GameObject unidirecOutputObject;
        public GameObject bidirectionalObject;
        public GameObject activateAvatar;

        public void SetMode(InteractCondition mode)
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

            if (baselineObject != null) baselineObject.SetActive(selectedMode == InteractCondition.Baseline);
            if (unidirecInputObject != null) unidirecInputObject.SetActive(selectedMode == InteractCondition.UniDirectional_Input);
            if (unidirecOutputObject != null) unidirecOutputObject.SetActive(selectedMode == InteractCondition.UniDirectional_Output);
            if (bidirectionalObject != null) bidirectionalObject.SetActive(selectedMode == InteractCondition.BiDirectional);

            switch (selectedMode)
            {
                case InteractCondition.Baseline:
                    activateAvatar = baselineObject;
                    break;
                case InteractCondition.UniDirectional_Input:
                    activateAvatar = unidirecInputObject;
                    break;
                case InteractCondition.UniDirectional_Output:
                    activateAvatar = unidirecOutputObject;
                    break;
                case InteractCondition.BiDirectional:
                    activateAvatar = bidirectionalObject;
                    break;
            }

        }
    }
}