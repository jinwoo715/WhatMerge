using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "MultiTarget", menuName = "Skill/Execution/MultiTarget", order = 0)]
    public class MultiExecutionData : ExecutionData
    {
        public int MultiCount;
        public bool IsRandom = true;
    }
}
