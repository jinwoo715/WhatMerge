using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "VFX", menuName = "Skill/VFX", order = 0)]
    public class VFXData : ScriptableObject
    {
        public string VFXName;
        public float LifeTime;
    }
}
