using Combat;

namespace Skill
{
    public class SkillExtraEffecter : ISkillExtraEffecter
    {
        ExtraEffectData _data;

        public int TargetSkillUID { get => _data.AttachedActiveSkillUID; }
        public SkillExtraEffecter(ExtraEffectData data)
        {
            _data = data;
        }

        public void OnBeforeApply(AttackPayload payload)
        {
            int random = UnityEngine.Random.Range(0, 101);

            if (random > _data.Chance) return;

            if (_data.EffectType == EExtraAttackEffectType.IgnoreAmour)
                payload.IsPiercing = true;
            else
                payload.AddStatusEffect(_data.StatusEffectUID);
        }
    }
}