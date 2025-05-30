using UnityEngine;

namespace BlockPuzzleGame
{
    public class ObjectInfo : MonoBehaviour
    {
        [SerializeField] private string objectColorName;
        public string ObjectName => gameObject.name;
        public string ObjectColorName => objectColorName;

        public void SetColor(string colorName)
        {
            objectColorName = colorName;
        }

        public void PrintInfo()
        {
            Debug.Log($"Name: {ObjectName}, Color: {ObjectColorName}");
        }
    }
}
