using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "VFX", menuName = "Skill/VFX", order = 0)]
    public class VFXData : ScriptableObject
    {
        public string VFXName;
        public bool IsApplyDir = false;
        public VFXSpawnPositionTpye PositionType;
    }

    public enum VFXSpawnPositionTpye
    {
        Owner,
        Target,
        Middle,
        ScreenCenter
    }
}
