using UnityEngine;

namespace Skill.Data
{
    public class SkillEffect : ScriptableObject
    {
        public int EffectUID;

        [Header("적용 효과 아이콘")]
        public SkillVisualSystem VFX;
    }
}
