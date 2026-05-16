using Combat;
using Enemies;
using Entity;
using Heros.Stat;
using Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillVisualSystem
{
    private ISpriteChanger _spriteChanger;
    private string _readyName;
    private string _excuteName;
    public SkillVisualSystem(ISpriteChanger spriteChanger, string name)
    {
        _spriteChanger = spriteChanger;
        _readyName = $"{name}_Ready";
        _excuteName = name;
    }
    public void SetReady()
    {
        _spriteChanger.SetSprite(_readyName);
    }
    public void SetExcute()
    {
        _spriteChanger.SetSprite(_excuteName);
    }
}

public class TargetSystem : ITargetSystem
{
    private IFieldEnemyService _fieldEnemyService;

    public bool HasEnemyInRange(Vector3 pivot, float range)
    {
        return CreatureFinder.HasNearEnemy(pivot, range);
    }

    public bool HasEnemyOnField()
    {
        return _fieldEnemyService.GetActiveEnemyCount > 0;
    }
}

public interface ITargetSystem
{
    bool HasEnemyInRange(Vector3 pivot, float range);
    bool HasEnemyOnField();
}

public abstract class ActiveSkillBase : IActiveSkill
{
    public ActiveSkillData _data { get; private set; }
    public IServiceLocator _context { get; private set; }
    public IServiceLocator _ownerContext { get; private set; }

    protected IAttackable _owner;
    private IAttackStatProvider _statProvider;

    private ITargetSystem _targetSystem;
    private ISkillTriggerStrategy _trigger;

    private float _addP1;
    private float _addP2;
    private float _addP3;

    protected float P1 => _data.P1 + _addP1;
    protected float P2 => _data.P2 + _addP2;
    protected float P3 => _data.P3 + _addP3;

    public int UID => _data.UID;

    protected SkillVisualSystem _skillVisualSystem;

    IHeroInfoProvider _hero;
    public ActiveSkillBase(ActiveSkillData data, IServiceLocator context, IServiceLocator owner, ISkillTriggerStrategy trigger)
    {
        _data = data;
        _context = context;
        _ownerContext = owner;
        _trigger = trigger;

        owner.TryGet<IHeroInfoProvider>(out _hero);

        owner.TryGet(out _owner);
        owner.TryGet(out _statProvider);

        ISpriteChanger _spriteChanger;
        owner.TryGet<ISpriteChanger>(out _spriteChanger);
        _skillVisualSystem = new SkillVisualSystem(_spriteChanger, data.Name);

        BindService();
    }

    public abstract void BindService();
    public void BindSkillHelpService<T>(ref T service) where T : class 
    {
        if (_context.TryGet<T>(out var getService))
        {
            service = getService;
        }
        else
        {
            Debug.LogError($"Not Found {typeof(T)}");
        }
    }
    public void BindOwnerHelpService<T>(ref T service) where T : class
    {
        if (_ownerContext.TryGet<T>(out var getService))
        {
            service = getService;
        }
    }
    public abstract IEnumerator Execute();
    public bool IsUseable(SkillTriggerContext context)
    {
        if(!_trigger.CanTrigger(context))
            return false;

        return HasValidTarget();
    }

    public void SetReadyMotion()
    {
        _skillVisualSystem.SetReady();
    }

    public void SetExcuteMotion()
    {
        _skillVisualSystem.SetExcute();
    }


    public virtual bool HasValidTarget()
    {
        switch (_data.TargetType)
        {
            case ESkillTargetType.Self:
                return true;
            case ESkillTargetType.NearEnemies:
                return _targetSystem.HasEnemyInRange(_hero.Transform.position, _data.Range);
            case ESkillTargetType.AllEnemies:
                return _targetSystem.HasEnemyOnField();
        }

        return false;
    }
    public void PayCost(ISkillResourceModifier skillResourceModifier)
    {
        _trigger.PayCost(skillResourceModifier);
    }
    public void ModifyParam(int paramIndex, float value)
    {
        if (paramIndex == 1)
            _addP1 += value;
        else if (paramIndex == 2)
            _addP2 += value;
        else if (paramIndex == 3)
            _addP3 += value;
    }

    public void RegisterExtraEffect(ISkillExtraEffecter extraEffecter)
    {
        throw new NotImplementedException();
    }
}
