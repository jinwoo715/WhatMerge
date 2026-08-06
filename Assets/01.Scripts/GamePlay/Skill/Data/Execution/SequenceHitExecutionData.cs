using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "SequenceApply", menuName = "Skill/Execution/SequenceApply", order = 0)]
    public class SequenceHitExecutionData : ExecutionData
    {
        public int SequenceCount;
    }
}
