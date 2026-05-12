using Combat;
using System.Collections;

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

public interface ISkill
{
    IEnumerator Excute();
    bool IsUseable(SkillTriggerContext context);
    void PayCost(ISkillResourceModifier skillResourceModifier);
}

public interface ISkillStatModifier
{
    void AddParam(int paramIndex, float value);
}
