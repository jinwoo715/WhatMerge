using System.Collections;

namespace Skill
{
    public interface ISkill
    {
        int SkillUID { get; }
    }

    public interface IActiveSkill : ISkill
    {
        public ITrigger Trigger { get; }
        public IFinder Target { get; }
        public IExecute Execution { get; }
        float BaseAnimationDuration { get; }
        bool IsUsable(SkillTriggerContext context);
        IEnumerator Execute(float animationTimeScale);
        void Dispose();
    }
    public interface IPassiveSkill : ISkill
    {
        void Apply();
        void Release();
    }
}
