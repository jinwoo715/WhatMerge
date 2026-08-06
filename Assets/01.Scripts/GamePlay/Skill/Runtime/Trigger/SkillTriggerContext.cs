namespace Skill
{
    public struct SkillTriggerContext
    {
        public int HitCount;
        public float Mana;

        public SkillTriggerContext(int hitCount, float mana)
        {
            HitCount = hitCount;
            Mana = mana;
        }
    }
}
