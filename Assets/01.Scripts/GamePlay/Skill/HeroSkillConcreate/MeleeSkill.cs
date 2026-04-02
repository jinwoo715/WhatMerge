using Combat;
using Heros.Stat;
using Skill;
using System;
using System.Collections;
using UnityEngine;

public abstract class AttackSkill : ActiveSkillBase
{
    protected IAttackRegister _attackRegister;
    protected IAttackStatProvider _statProvider;

    public AttackSkill(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }

    public override void BindService()
    {
        BindSkillHelpService<IAttackRegister>(ref _attackRegister);
        BindOwnerHelpService<IAttackStatProvider>(ref _statProvider);
    }
}


public class SingleMeleeAttack : AttackSkill
{
    public SingleMeleeAttack(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }

    private Transform _ownerTransform;
    IDamageable _target;

    public override IEnumerator Excute()
    {
        SetReadyMotion();

        yield return new WaitForSeconds(_data.StartupDelay);

        if(_target == null || _target.IsActive)
        {
            if (CreatureFinder.TryFindNearEnemy(_ownerTransform.position, _statProvider.GetStat(EAttackStatType.Radius), out var target))
                _target = target;
            else
                yield break;
        }

        if(_target != null)
        {
            int damage = (int)_statProvider.GetStat(EAttackStatType.Damage);
            int FlatPenetration = (int)_statProvider.GetStat(EAttackStatType.FlatPentration);
            int PercentPenetration = (int)_statProvider.GetStat(EAttackStatType.PercentPenetration);

            AttackPayload ap = new AttackPayload(damage, FlatPenetration, PercentPenetration);
            DamageContext dc = new DamageContext(ap, _target);
            _attackRegister.RegisterAttack(dc);
        }

        SetExcuteMotion();

        yield return new WaitForSeconds(_data.ActionHoldTime);
        
        _target = null;
    }

    public override void BindService()
    {
        base.BindService();
        BindOwnerHelpService(ref _ownerTransform);
    }

    public override bool HasValidTarget()
    {
        float radius = _statProvider.GetStat(EAttackStatType.Radius);

        if (CreatureFinder.TryFindNearEnemy(_ownerTransform.position, radius, out var target))
        {
            _target = target;
            return true;
        }

        return false;
    }
}