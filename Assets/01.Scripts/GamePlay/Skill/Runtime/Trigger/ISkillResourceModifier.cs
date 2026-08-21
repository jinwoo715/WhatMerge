namespace Skill
{
    public interface IManaReceiver
    {
        void RestoreMana(float amount);
    }

    public interface ISkillResourceModifier
    {
        void ConsumeHitCount(int count);
        void ConsumeMana(float amout);
        void AddHitCount(int count);
        void AddMana(float amount);
        void IncreaseManaAmoutRaio(float ratio);
    }
}
