using UnityEngine;

namespace Skill.Data
{
    public class NomalEffect : EffectBase
    {
        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }

    public class GoldEffect : NomalEffect
    {
        public int Gold;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}
