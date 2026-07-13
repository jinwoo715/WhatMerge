using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DecreaseAmour", menuName = "Skill/Effect/DecreaseAmour", order = 0)]
    public class ArmorReduction : DurationEffectBase
    {
        [Range(0,1)]
        public float Value;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}
