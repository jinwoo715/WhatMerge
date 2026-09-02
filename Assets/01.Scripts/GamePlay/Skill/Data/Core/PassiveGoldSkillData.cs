using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "PeriodicGold", menuName = "Skill/Passive/Periodic Gold", order = 0)]
    public sealed class PassiveGoldSkillData : PassiveSkillData
    {
        [Min(0.01f)]
        public float IntervalTime = 10f;

        [Min(1)]
        public int GoldAmount;
    }
}
