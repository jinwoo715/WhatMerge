using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class SkillBase : ScriptableObject
    {
        public ESkillType SkillType;

        [Header("Info")]
        public int UID;
        public string Name;
        public string Description;
    }
}