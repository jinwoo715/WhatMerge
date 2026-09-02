using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Skill/Effect/DamageEffect", order = 0)]

    public class DamageEffect : NormalEffect
    {
        public const string DamageRatioStat = "DamageRatio";
        public const string ArmorIgnoreChanceStat = "ArmorIgnoreChance";
        public const string ArmorIgnoreRatioStat = "ArmorIgnoreRatio";

        protected static readonly EffectStatDefinition[] EnhanceableStats =
        {
            new EffectStatDefinition(ChanceKey, "발동확률"),
            new EffectStatDefinition(DamageRatioStat, "데미지 적용 비율"),
            new EffectStatDefinition(ArmorIgnoreChanceStat, "방어력 무시 확률"),
            new EffectStatDefinition(ArmorIgnoreRatioStat, "방어력 무시 비율")
        };

        public float DamageRatio;
        public ElementType Attribute;
        [Range(0f, 1f)] public float ArmorIgnoreChance;
        [Range(0f, 1f)] public float ArmorIgnoreRatio = 0f;

        public override void AddStat(string key, float value)
        {
            base.AddStat(key, value);

            if (key == DamageRatioStat)
                DamageRatio += value;
            else if (key == ArmorIgnoreChanceStat)
                ArmorIgnoreChance = Mathf.Clamp01(ArmorIgnoreChance + value);
            else if (key == ArmorIgnoreRatioStat)
                ArmorIgnoreRatio = Mathf.Clamp01(ArmorIgnoreRatio + value);
        }

        public override IReadOnlyList<EffectStatDefinition> GetEnhanceableStats()
        {
            return EnhanceableStats;
        }
    }
}
