using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "MultiTarget", menuName = "Skill/Execution/MultiTarget", order = 0)]
    public class RandomMultiExecutionData : ExecutionData
    {
        public int MultiCount;
    }
}