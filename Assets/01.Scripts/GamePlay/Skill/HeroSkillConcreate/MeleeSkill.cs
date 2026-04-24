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
public class ConeMelee : AttackSkill
{
    private Transform _ownerTransform;
    private IDamageable _target;
    private float _coneAngle;

    private DrawUtility _du;

    public ConeMelee(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) 
    {
        _coneAngle = data.P1;
    }

    public override IEnumerator Excute()
    {
        SetReadyMotion();

        yield return new WaitForSeconds(_data.MotionDelay);

        float radius = _statProvider.GetStat(EAttackStatType.Radius);

        if (HasValidTarget())
        {
            Vector2 dir = (_target.Position - _ownerTransform.position).normalized;

            _du.UpdateDir(dir);

            var enemies = CreatureFinder.FindNearEnemiesInConeArea(_ownerTransform.position, radius, dir, _coneAngle);

            _attackRegister.RegisterAttack(new DamageContext(_data.VFX, _target.Position, _owner));

            float dmgMultiple = _data.ValueRate * 0.01f;
            float damage = _statProvider.GetStat(EAttackStatType.Damage) * dmgMultiple;

            int resultDamage = Mathf.RoundToInt(damage);

            int FlatPenetration = (int)_statProvider.GetStat(EAttackStatType.FlatPentration);
            int PercentPenetration = (int)_statProvider.GetStat(EAttackStatType.PercentPenetration);

            for (int i = 0; i < enemies.Count; i++)
            {
                AttackPayload ap = new AttackPayload(resultDamage, FlatPenetration, PercentPenetration);
                DamageContext dc = new DamageContext(ap, enemies[i], string.Empty, _owner);
                _attackRegister.RegisterAttack(dc);
            }
        }

        SetExcuteMotion();

        yield return new WaitForSeconds(_data.ResetDelay);

        _target = null;
    }

    public override void BindService()
    {
        base.BindService();
        BindOwnerHelpService(ref _ownerTransform);

        _du = _ownerTransform.gameObject.AddComponent<DrawUtility>();
        _du.Init(_statProvider.GetStat(EAttackStatType.Radius), _data.P1*2);
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

    private IDamageable _target;

    public override IEnumerator Excute()
    {
        SetReadyMotion();

        yield return new WaitForSeconds(_data.MotionDelay);

        if(_target == null || _target.IsActive)
        {
            if (CreatureFinder.TryFindNearDamageable(_owner.Position, _statProvider.GetStat(EAttackStatType.Radius), out var target))
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
            DamageContext dc = new DamageContext(ap, _target, _data.VFX, _owner);
            _attackRegister.RegisterAttack(dc);
        }

        SetExcuteMotion();

        yield return new WaitForSeconds(_data.ResetDelay);
        
        _target = null;
    }


    public override bool HasValidTarget()
    {
        float radius = _statProvider.GetStat(EAttackStatType.Radius);

        if(_target != null)
        {
            float dist = Vector2.Distance(_target.Position, _owner.Position);

            if (dist > radius)
                _target = null;
        }

        if (_target == null)
        {
            if (CreatureFinder.TryFindNearDamageable(_owner.Position, radius, out var target))
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

public class SingleProjectile : AttackSkill
{
    private IDamageable _target;
    private IProjectileProvider _projectileProvider;

    public SingleProjectile(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }

    public override IEnumerator Excute()
    {
        SetReadyMotion();

        yield return new WaitForSeconds(_data.MotionDelay);

        SetExcuteMotion();

        ProjectilePayload projectilePayload = new ProjectilePayload();

        projectilePayload.Attacker = _owner;
        projectilePayload.Target = _target;
        projectilePayload.SpawnPos = _owner.Position;
        projectilePayload.UID = Mathf.RoundToInt(_data.P1);
        projectilePayload.HeroLevel = _heroInfoProvider.EvolutionLevel;
        projectilePayload.attackRegister = _attackRegister;
        projectilePayload.attackStatProvider = _statProvider;
        projectilePayload.DMGValue = _data.ValueRate;
        projectilePayload.VFX = _data.VFX;

        _projectileProvider.SpawnProjectile(projectilePayload);

        yield return new WaitForSeconds(_data.ResetDelay);
    }

    IHeroInfoProvider _heroInfoProvider;

    public override void BindService()
    {
        base.BindService();
        BindOwnerHelpService(ref _heroInfoProvider);
        BindSkillHelpService(ref _projectileProvider);
    }

    public override bool HasValidTarget()
    {
        float radius = _statProvider.GetStat(EAttackStatType.Radius);

        if (_target != null)
        {
            float dist = Vector2.Distance(_target.Position, _owner.Position);

            if (dist > radius)
                _target = null;
        }

        if (_target == null)
        {
            if (CreatureFinder.TryFindNearDamageable(_owner.Position, radius, out var target))
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

public class SingleSummon : AttackSkill
{
    private IDamageable _target;
    private ISummonProvider _summonProvider;

    public SingleSummon(ActiveSkillData data, ISkillContext context, ISkillContext owner) : base(data, context, owner) { }

    public override IEnumerator Excute()
    {
        SetReadyMotion();

        yield return new WaitForSeconds(_data.MotionDelay);

        SetExcuteMotion();

        ProjectilePayload projectilePayload = new ProjectilePayload();

        projectilePayload.Attacker = _owner;
        projectilePayload.Target = _target;
        projectilePayload.SpawnPos = _target.Position;
        projectilePayload.UID = Mathf.RoundToInt(_data.P1);
        projectilePayload.HeroLevel = _heroInfoProvider.EvolutionLevel;
        projectilePayload.attackRegister = _attackRegister;
        projectilePayload.attackStatProvider = _statProvider;
        projectilePayload.DMGValue = _data.ValueRate;
        projectilePayload.VFX = _data.VFX;

        _summonProvider.SpawnProjectile(projectilePayload);

        yield return new WaitForSeconds(_data.ResetDelay);
    }

    IHeroInfoProvider _heroInfoProvider;

    public override void BindService()
    {
        base.BindService();
        BindOwnerHelpService(ref _heroInfoProvider);
        BindSkillHelpService(ref _summonProvider);
    }

    public override bool HasValidTarget()
    {
        float radius = _statProvider.GetStat(EAttackStatType.Radius);

        if (_target != null)
        {
            float dist = Vector2.Distance(_target.Position, _owner.Position);

            if (dist > radius)
                _target = null;
        }

        if (_target == null)
        {
            if (CreatureFinder.TryFindNearDamageable(_owner.Position, radius, out var target))
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