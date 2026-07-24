using UnityEngine;

namespace Skill.Data
{
    public interface IRevert
    {
        void Revert();
    }

    [CreateAssetMenu(fileName = "Slow", menuName = "Skill/Effect/Slow", order = 0)]
    public class SlowEffect : DurationEffectBase, IRevert
    {
        [Range(0, 1)]
        public float SlowRatio;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }

        public void Revert()
        {
            SlowRatio *= -1;
        }
    }
}
