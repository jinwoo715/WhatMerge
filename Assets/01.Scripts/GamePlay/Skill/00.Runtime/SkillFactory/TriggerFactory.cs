using Skill.Data;
using System;

namespace Skill
{
    public class TriggerFactory
    {
        public static ITrigger CreateTrigger(TriggerData data)
        {
            return data switch
            {
                NoneTriggerData => new NoneTrigger(),
                HitCountTriggerData hitCount => new HitCountTrigger(hitCount.HitCount),
                ManaTriggerData mana => new ManaTrigger(mana.Mana),
                _ => throw new InvalidOperationException($"Unsupported TriggerData: {data?.name ?? "null"}"),
            };
        }
    }
}
