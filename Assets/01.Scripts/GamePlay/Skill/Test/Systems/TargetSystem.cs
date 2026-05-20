using Skill;
using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "Target", menuName = "Skill/Target", order = 0)]
    public class TargetSystem : ScriptableObject
    {
        public ESkillTargetType TargetType;
        public int Radius;
    }
}