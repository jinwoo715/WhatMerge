namespace Skill
{
    public class SkillParamModifier
    {
        SkillStatModifyData _data;
        public int TargetUID => _data.UID;
        public SkillParamModifier(SkillStatModifyData data)
        {
            _data = data;
        }

        public void Excute(ISkill skillStatModifier)
        {
            skillStatModifier.ModifyParam(_data.ParamIndex, _data.Value);
        }
    }
}