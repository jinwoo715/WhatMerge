using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "KnockBack", menuName = "Skill/Effect/KnockBack", order = 0)]
    public class KnockBackEffect : NomalEffect
    {
        public float Diatance;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}
