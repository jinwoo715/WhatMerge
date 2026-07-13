using Skill.Data;

namespace WhatMerge.Combat
{
    public class DamageCalculator
    {
        public int CalculateFinalDamage(IDamageable target, AttackPayload payload, float multipleValue)
        {
            int armor = target.Armor;
            float reduceRatio = 1 - StatCalculator.GetDamageReductionRate(armor, payload.PercentPenetration, payload.FlatPenetration);

            bool critical = IsCritical(payload.CriticalChance);

            if (critical)
            {
                return StatCalculator.RoundInt(payload.AttackDamage * multipleValue * reduceRatio);
            }
            else
            {
                return StatCalculator.RoundInt(payload.AttackDamage * multipleValue * payload.CriticalMultiple * reduceRatio);
            }
        }

        private bool IsCritical(int chance)
        {
            int ran = UnityEngine.Random.Range(1, 101);
            return chance >= ran;
        }

        public int CalculateDotDamage(IDamageable damageable, DotEffect dot)
        {
            float damage = dot.ApplyType switch
            {
                DotDamageType.CurrentHPRatio => damageable.CurrentHP * dot.Value,
                DotDamageType.MaxHPRatio => damageable.MaxHP * dot.Value,
                _ => dot.Value
            };

            return StatCalculator.RoundInt(damage);
        }
    }
}
