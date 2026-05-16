namespace Skill
{
    [System.Serializable]
    public class ActiveSkillData : BaseData
    {
        public string Name;
        public string Description;

        public string SkillType;

        public ESkillTriggerType TriggerType;
        public float TriggerValue;

        public ESkillTargetType TargetType;

        public float MotionDelay;
        public float ResetDelay;

        public int ValueRate;

        public float Range;

        public float P1;
        public float P2;
        public float P3;

        public string VFX;
    }
}