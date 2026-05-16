using Skill;
using System.Collections;
using UnityEngine;

public class AttachBuff : ActiveSkillBase
{
    public AttachBuff(ActiveSkillData data, IServiceLocator context, IServiceLocator owner, ISkillTriggerStrategy trigger) : base(data, context, owner, trigger) { }

    private IBuffRegister _buffRegister;

    public override void BindService()
    {
        BindSkillHelpService(ref _buffRegister);
        Debug.Log(_buffRegister);
    }

    public override IEnumerator Execute()
    {
        _skillVisualSystem.SetReady();

        yield return new WaitForSeconds(_data.MotionDelay);

        var heros = CreatureFinder.TryFindNearHeors(_owner.Position, 1);

        Debug.Log($"Buff Target Count : {heros.Count}");

        foreach (var hero in heros)
        {
            _buffRegister.RegisterBuff(Mathf.RoundToInt(_data.P3), hero);
        }

        _skillVisualSystem.SetExcute();

        yield return new WaitForSeconds(_data.ResetDelay);
    }
}
