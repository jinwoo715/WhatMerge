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

//TODO 
public class ConeMeleeAttack : AttackSkill
{
    private Transform _ownerTransform;
    private IDamageable _target;
    private float _coneAngle;

    private DrawUtility _du;

    public ConeMeleeAttack(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) 
    {
        _coneAngle = data.P2;
    }

    public override IEnumerator Excute()
    {
        SetReadyMotion();

        yield return new WaitForSeconds(_data.StartupDelay);

        float radius = _statProvider.GetStat(EAttackStatType.Radius);

        if (HasValidTarget())
        {
            Vector2 dir = (_target.Position - _ownerTransform.position).normalized;

            _du.UpdateDir(dir);

            var enemies = CreatureFinder.FindNearEnemiesInConeArea(_ownerTransform.position, radius, dir, _coneAngle);

            for (int i = 0; i < enemies.Count; i++)
            {
                int damage = (int)_statProvider.GetStat(EAttackStatType.Damage);
                int FlatPenetration = (int)_statProvider.GetStat(EAttackStatType.FlatPentration);
                int PercentPenetration = (int)_statProvider.GetStat(EAttackStatType.PercentPenetration);

                AttackPayload ap = new AttackPayload(damage, FlatPenetration, PercentPenetration);
                DamageContext dc = new DamageContext(ap, enemies[i]);
                _attackRegister.RegisterAttack(dc);
            }
        }

        SetExcuteMotion();

        yield return new WaitForSeconds(_data.ActionHoldTime);

        _target = null;
    }

    public override void BindService()
    {
        base.BindService();
        BindOwnerHelpService(ref _ownerTransform);

        _du = _ownerTransform.gameObject.AddComponent<DrawUtility>();
        _du.Init(_statProvider.GetStat(EAttackStatType.Radius), _data.P2);
    }

    public override bool HasValidTarget()
    {
        float radius = _statProvider.GetStat(EAttackStatType.Radius);

        if (_target != null)
        {
            float dist = Vector2.Distance(_target.Position, _ownerTransform.position);

            if (dist > radius)
                _target = null;
        }

        if (_target == null || !_target.IsActive)
        {
            if (CreatureFinder.TryFindNearDamageable(_ownerTransform.position, radius, out var target))
            {
                _target = target;
                return true;
            }
            else
                return false;
        }

        return true;
    }
}

//TODO
public class SingleMeleeAttack : AttackSkill
{
    public SingleMeleeAttack(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }

    private Transform _ownerTransform;
    private IDamageable _target;

    public override IEnumerator Excute()
    {
        SetReadyMotion();

        yield return new WaitForSeconds(_data.StartupDelay);

        if(_target == null || _target.IsActive)
        {
            if (CreatureFinder.TryFindNearDamageable(_ownerTransform.position, _statProvider.GetStat(EAttackStatType.Radius), out var target))
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

        if(_target != null)
        {
            float dist = Vector2.Distance(_target.Position, _ownerTransform.position);

            if (dist > radius)
                _target = null;
        }

        if (_target == null)
        {
            if (CreatureFinder.TryFindNearDamageable(_ownerTransform.position, radius, out var target))
            {
                _target = target;
                return true;
            }
            else
                return false; 
        }

        return true;
    }
}