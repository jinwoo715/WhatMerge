using UnityEngine;
using UnityEngine.Serialization;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "TimedBuffEffect", menuName = "Skill/Effect/TimedBuffEffect", order = 0)]
    public class BuffEffect : EffectBase
    {
        public BuffData BuffData;
        public float Duration;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}