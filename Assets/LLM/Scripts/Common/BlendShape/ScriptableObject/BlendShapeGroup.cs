using System;
using System.Collections.Generic;

[Serializable]
public class BlendShapeGroup
{
    public string Name;
    public List<BlendShapeDetail> Targets = new List<BlendShapeDetail>();

    [Serializable]
    public class BlendShapeDetail
    {
        public int Index;
        public float TargetValue = 100f;
    }
}
