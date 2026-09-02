using System.Collections;

namespace Skill
{
    public interface IActiveSkill : System.IDisposable
    {
        public ITrigger Trigger { get; }
        public IFinder Target { get; }
        public IExecute Execution { get; }
        float BaseAnimationDuration { get; }
        float ChargeTime { get; }
        float ActivationChance { get; }
        int Priority { get; }
        bool IsUsable(SkillTriggerContext context);
        bool RollActivation();
        IEnumerator Execute(float animationTimeScale);
    }
    public interface IPassiveSkill
    {
        void Apply();
        void Tick(float deltaTime);
        void Release();
    }
}
