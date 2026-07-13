using System.Collections.Generic;

namespace Skill.Data
{
    public interface IEffectValueModifier
    {
        public IReadOnlyList<EffectStatDefinition> GetEnhanceableStats();
        void AddStat(string key, float value);
    }
}
