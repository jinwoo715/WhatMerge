using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "HitCountTrigger", menuName = "Skill/Trigger/HitCountTrigger", order = 0)]
    public class HitCountTriggerData : TriggerData
    {
        public int HitCount;
    }
}
