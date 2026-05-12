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
public abstract class ActiveSkillBase : ISkill, ISkillStatModifier
{
    public ActiveSkillData _data { get; private set; }
    public ISkillContext _context { get; private set; }
    public ISkillContext _ownerContext { get; private set; }

    private ISpriteChanger _spriteChanger;
    private string _readySpriteName;
    private string _excuteSpriteName;

    protected IAttackable _owner;
    private IAttackStatProvider _statProvider;

    private ITrigger _trigger;

    private float _addP1;
    private float _addP2;
    private float _addP3;

    protected float P1 => _data.P1 + _addP1;
    protected float P2 => _data.P2 + _addP2;
    protected float P3 => _data.P3 + _addP3;


    public ActiveSkillBase(ActiveSkillData data, ISkillContext context, ISkillContext owner, ITrigger trigger)
    {
        _data = data;
        _context = context;
        _ownerContext = owner;
        _trigger = trigger;

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
        if(!_trigger.CanTrigger(context))
            return false;

        return HasValidTarget();
    }

    public abstract bool HasValidTarget();
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

    public void AddParam(int paramIndex, float value)
    {
        if (paramIndex == 1)
            _addP1 += value;
        else if (paramIndex == 2)
            _addP2 += value;
        else if (paramIndex == 3)
            _addP3 += value;
    }
}

#region Passive Skill
public class PassiveData : Data
{
    public EPassiveType Type;
    public int PassiveUID;
}
public enum EPassiveType
{
    Buff,
    DeBuff,
    AttackExtra,    //공격에 대한 추가 효과 부여
    Skill           //스킬 파라미터 강화
}
public class BuffPassiveData : Data
{
    public EBuffTargetType TargetType;
    public EHeroStatType StatType;
    public int Value;
}
public enum EBuffTargetType
{
    Self,
    NearHeros,
    AllHeros
}

public class DeBuffPassiveData : Data
{
    public EEnemyStatType StatType;
    public int Value;
}
public enum EEnemyStatType
{
    MoveSpeed,
    Amour
}

public class AttackExtraData : Data
{
    public string ExtraName;
    public float Param;
}

public class ChancePiercing : ISkillApplyModifier
{
    AttackExtraData _data;
    public void OnBeforeApply(AttackPayload payload)
    {
        int random = UnityEngine.Random.Range(0, 101);

        if (random < _data.Param)
            payload.IsPiercing = true;
    }
    public void OnAfterApply(AttackPayload payload)
    {
        throw new NotImplementedException();
    }
}

public interface ISkillApplyModifier
{
    void OnBeforeApply(AttackPayload payload);
    void OnAfterApply(AttackPayload payload);
}

public class SkillModifyPassiveData : Data
{
    public int TargetSkillUID;
    public int ParamIndex;
    public float AddValue;
}

#endregion

#region Trigger
public interface ITrigger
{
    void Init(float cost);
    bool CanTrigger(SkillTriggerContext context);
}
public class HitCountTrigger : ITrigger
{
    private int _require;
    public void Init(float require)
    {
        _require = (int)require;
    }
    public bool CanTrigger(SkillTriggerContext context)
    {
        return _require <= context.HitCount;
    }
}
public class ManaTrigger : ITrigger
{
    private float _cost;
    public void Init(float cost)
    {
        _cost = cost;
    }
    public bool CanTrigger(SkillTriggerContext context)
    {
        return _cost <= context.Mana;
    }
}
public class AlwaysTrigger : ITrigger
{
    public void Init(float cost) { }
    public bool CanTrigger(SkillTriggerContext context) => true;
}
#endregion