using Skill.Data;
using System;

namespace Skill
{
    public class ExecutionFactory
    {
        public static IExecute CreateExecution(SkillExecutionContext executionContext, SkillRuntimeContext runtimeContext)
        {
            return executionContext.ExecutionData switch
            {
                MultiExecutionData => new RandomMultiExecution(executionContext, runtimeContext),
                SequenceHitExecutionData => new SequenceExecution(executionContext, runtimeContext),
                ConeExecutionData => new ConeExecution(executionContext, runtimeContext),
                SingleExecutionData => new SingleExecution(executionContext, runtimeContext),
                _ => throw new InvalidOperationException($"Not Switch Type : {executionContext.ExecutionData}"),
            };
        }
    }
}
