using Skill.Data;

namespace Skill
{
    public class ExecutionFactory
    {
        public static IExecute CreateExecution(ActiveSkillContext executionService, SkillCommonContext skillExecutionService)
        {
            return executionService.Execution switch
            {
                RandomMultiExecutionData => new RandomMultiExecution(executionService, skillExecutionService),
                SequenceHitExecutionData => new SequenceExecution(executionService, skillExecutionService),
                ConeExecutionData => new ConeExecution(executionService, skillExecutionService),
                SingleExecutionData => new SingleExecution(executionService, skillExecutionService),
                _ => new SingleExecution(executionService, skillExecutionService),
            };
        }
    }
}
