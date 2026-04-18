using Combat;
using Enemies;
using Heros.Stat;
using Skill;
using System;
using System.Collections;
using UnityEngine;

public enum ETargetType
{
    NearestSingleEnemy,    // 기본 공격용 (가장 가까운 적)
    NearbyEnemies,  // 주변 적 전체 (광역기)
    AllEnemies,     // 맵 전체 적 (글로벌 궁극기)
    NearbyAllies,   // 주변 아군 (힐, 버프)
    AllAllies,      // 맵 전체 아군
    Self            // 자신 (자가 버프, 쉴드)
}

public enum EExcuteTriggerType
{
    None,
    HitCount,
    Mana,
    Special
}

[System.Serializable]
public class ActiveSkillData
{
    public int UID;
    public int HeroUID;
    public string Name;
    public string Description;
    
    public string SkillType;
    
    public ESkillSlot SkillSlot;
    public EExcuteTriggerType TriggerType;
    public float TriggerValue;

    public ETargetType TargetType;
    public int TargetCount;

    public float StartupDelay;
    public float ActionHoldTime;

    public float P1;
    public float P2;
    public float P3;
}

public abstract class ActiveSkillBase : ISkill
{
    public ActiveSkillData _data { get; private set; }
    public ISkillContext _context { get; private set; }
    public ISkillContext _ownerContext { get; private set; }

    public ESkillSlot SkillSlot => _data.SkillSlot;

    private ISpriteChanger _spriteChanger;
    private string _readySpriteName;
    private string _excuteSpriteName;

    private Transform _owner;
    private IAttackStatProvider _statProvider;

    public ActiveSkillBase(ActiveSkillData data, ISkillContext context, ISkillContext owner)
    {
        _data = data;
        _context = context;
        _ownerContext = owner;

        owner.TryGet<IHeroInfoProvider>(out var hero);
        owner.TryGet<ISpriteChanger>(out _spriteChanger);

        owner.TryGet(out _owner);
        owner.TryGet(out _statProvider);

        _readySpriteName = $"{data.Name}_Ready";
        _excuteSpriteName = $"{data.Name}";

        BindService();
    }

    public void SetReadyMotion()
    {
        _spriteChanger.SetSprite(_readySpriteName);
    }
    public void SetExcuteMotion()
    {
        _spriteChanger.SetSprite(_excuteSpriteName);
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
    public abstract IEnumerator Excute();
    public bool IsUseable(SkillTriggerContext context)
    {
        if (!CheckTriggerCondition(context))
            return false;

        return HasValidTarget();
    }
    private bool CheckTriggerCondition(SkillTriggerContext context)
    {
        switch (_data.TriggerType)
        {
            case EExcuteTriggerType.None:
                return true;
            case EExcuteTriggerType.HitCount:
                return context.HitCount >= _data.TriggerValue;
            case EExcuteTriggerType.Mana:
                return context.Mana >= _data.TriggerValue;

            case EExcuteTriggerType.Special:
                return CheckSpecialTrigger();
            default:
                return false;
        }
    }
    public abstract bool HasValidTarget();
    protected virtual bool CheckSpecialTrigger()
    {
        return false;
    }
    public void PayCost(ISkillResourceModifier skillResourceModifier)
    {
        switch (_data.TriggerType)
        {
            case EExcuteTriggerType.HitCount:
                skillResourceModifier.ConsumeHitCount((int)_data.TriggerValue + 1);
                break;
            case EExcuteTriggerType.Mana:
                skillResourceModifier.ConsumeManaCount(_data.TriggerValue);
                skillResourceModifier.ConsumeHitCount(1);
                break;
        }
    }
    public bool IsFindTarget()
    {
        float findRadius = _statProvider.GetStat(EAttackStatType.Radius);


        switch (_data.TargetType)
        {
            case ETargetType.NearestSingleEnemy:
            case ETargetType.NearbyEnemies:
                if (CreatureFinder.TryFindNearDamageable(_owner.position, findRadius, out var target))
                {
                    return true;
                }
                break;
            case ETargetType.AllEnemies:
                if(_context.TryGet<IFieldEnemyService>(out var fieldEnemyService))
                {
                    return fieldEnemyService.GetActiveEnemyCount != 0;
                }
                break;
            case ETargetType.NearbyAllies:

                if(CreatureFinder.TryFindNearHeors(_owner.position, findRadius).Count > 0)
                {
                    return true;
                }

                break;
            case ETargetType.AllAllies:

                break;
            case ETargetType.Self:
                if (_ownerContext.TryGet(out ICreature creature))
                {
                    return true;
                }
                break;
        }
        return false;
    }
}
