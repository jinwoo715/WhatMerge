using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Trigger", menuName = "Skill/Trigger", order = 0)]
    public class TriggerSystem : ScriptableObject
    {
        public ESkillTriggerType Trigger;
        public int RequireValue;
    }
}
