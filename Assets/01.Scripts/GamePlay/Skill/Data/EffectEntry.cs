using UnityEngine;

namespace Skill.Data
{
    [System.Serializable]
    public class EffectEntry
    {
        public EffectBase Effect;

        [Range(0, 1)]
        public float Chance = 1f;

        public bool IsUseable()
        {
            float ranNum = Random.value;
            return Chance >= ranNum;
        }
    }
}
