using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "VFX", menuName = "Skill/VFX", order = 0)]
    public class SkillVisualSystem : ScriptableObject
    {
        public string VFXName;
        public float Speed;
        public float Duration;
        public EVFXPositionType Position;
    }

    public enum EVFXPositionType
    {
        Target, //Å¸°Ù À§
        OnHero, //¿µ¿õ À§
        Middle, //Å¸°Ù°ú ¿µ¿õÀÇ Áß°£
        ScreenCenter    //È­¸é Áß¾Ó
    }


}
