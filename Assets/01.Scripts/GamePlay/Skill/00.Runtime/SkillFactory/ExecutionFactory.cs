using Skill.Data;

namespace Skill
{
    public class ExecutionFactory
    {
        public static IExecute CreateExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext)
        {
            return executionContext.ExecutionData switch
            {
                RandomMultiExecutionData => new RandomMultiExecution(executionContext, runtimeContext),
                SequenceHitExecutionData => new SequenceExecution(executionContext, runtimeContext),
                ConeExecutionData => new ConeExecution(executionContext, runtimeContext),
                SingleExecutionData => new SingleExecution(executionContext, runtimeContext),
                _ => new SingleExecution(executionContext, runtimeContext),
            };
        }
    }
}
