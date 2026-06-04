using Skill;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Target", menuName = "Skill/Target", order = 0)]
    public class TargetData : ScriptableObject
    {
        public ESkillTargetType TargetType;
        public float Radius;
    }
}