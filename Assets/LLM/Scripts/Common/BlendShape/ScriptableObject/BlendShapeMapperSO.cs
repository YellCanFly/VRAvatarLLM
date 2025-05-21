using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BlendShapeMapping", menuName = "BlendShape/BlendShapeMapping")]
public class BlendShapeMapperSO : ScriptableObject
{
    [Serializable]
    public class FacialAnimationMapping
    {
        public FaceAnimationType Type;
        public List<BlendShapeGroup> Groups = new List<BlendShapeGroup>();
    }

    public List<FacialAnimationMapping> Mappings = new List<FacialAnimationMapping>();

    public List<BlendShapeGroup> GetGroups(FaceAnimationType type)
    {
        foreach (var mapping in Mappings)
        {
            if (mapping.Type == type)
            {
                return mapping.Groups;
            }
        }
        return null;
    }
}