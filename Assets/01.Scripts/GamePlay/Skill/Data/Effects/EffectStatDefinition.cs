using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class EffectStatDefinition
    {
        public string Key;
        public string Label;

        public EffectStatDefinition(string key, string label)
        {
            Key = key;
            Label = label;
        }
    }
}