using UnityEngine;

namespace Skill.Data
{
    public interface IEffectModifier
    {
        void AddStat(float value);
        void AddChance(float value);
    }

    public class EffectBase : ScriptableObject, IEffectModifier
    {
        [Range(0, 1)]
        public float Chance = 1f;

        [Header("적용 효과 아이콘")]
        public VFXData VFX;

        public void AddChance(float value)
        {
            Chance += value;
        }

        public virtual void AddStat(float value)
        {
        }
    }
}
