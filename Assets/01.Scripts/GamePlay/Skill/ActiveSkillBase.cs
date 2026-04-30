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
public class ActiveSkillData : Data
{
    public string Name;
    public string Description;
    
    public string SkillType;
    
    public EExcuteTriggerType TriggerType;
    public float TriggerValue;

    public float MotionDelay;
    public float ResetDelay;

    public int ValueRate;

    public float P1;
    public float P2;
    public float P3;

    public string VFX;
}

public abstract class ActiveSkillBase : ISkill
{
    public ActiveSkillData _data { get; private set; }
    public ISkillContext _context { get; private set; }
    public ISkillContext _ownerContext { get; private set; }

    private ISpriteChanger _spriteChanger;
    private string _readySpriteName;
    private string _excuteSpriteName;

    protected IAttackable _owner;
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
}
