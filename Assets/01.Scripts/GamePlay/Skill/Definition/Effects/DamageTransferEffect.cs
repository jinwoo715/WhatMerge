using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "DamageTransfer", menuName = "Skill/Effect/DamageTransfer", order = 0)]
    public class DamageTransferEffect : DurationEffectBase
    {
        public float Radius;
        public int Count;

        [Range(0, 1)]
        public float TransitionRatio;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}