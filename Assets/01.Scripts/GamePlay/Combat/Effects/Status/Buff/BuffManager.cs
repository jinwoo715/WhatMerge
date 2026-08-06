using Skill.Data;
using System;
using System.Collections.Generic;
using WhatMerge.Combat;
using WhatMerge.Heros;

namespace WhatMerge.Combat.Effects
{
public interface IBuffService
{
    IRuntimeEffectHandle ApplyBuff(BuffEffect buffEffect, IHeroStatModifier statModifier);
    void RegisterPassiveBuff(BuffData buff);
    void UnRegisterPassiveBuff(BuffData buff);
}

public class BuffManager : UnityEngine.MonoBehaviour, IBuffService
{
    private List<BuffData> _passiveBuff = new List<BuffData>();
    private Dictionary<IHeroStatModifier, HashSet<IRuntimeEffectHandle>> _activeBuffs = new();

    private IFieldHeroService _fieldHeroService;

    public void Init(IFieldHeroService fieldHeroService)
    {
        _fieldHeroService = fieldHeroService;

        _fieldHeroService.OnSpawnedHero += ApplyPassiveBuff;
        _fieldHeroService.OnDestroyHero += ReleasePassiveBuff;
        _fieldHeroService.OnDestroyHero += ReleaseAllActiveBuff;
    }

    #region Passive Buff
    public void RegisterPassiveBuff(BuffData buff) 
    {
        _passiveBuff.Add(buff);

        var heros = _fieldHeroService.GetAllFieldHero;

        foreach (var hero in heros)
        {
            hero.StatModify.AddMultiplier(buff.BuffType, buff.IncreaseRatio);
        }

    }
    public void UnRegisterPassiveBuff(BuffData buff) 
    {
        _passiveBuff.Remove(buff);

        var heros = _fieldHeroService.GetAllFieldHero;

        foreach (var hero in heros)
        {
            hero.StatModify.AddMultiplier(buff.BuffType, -buff.IncreaseRatio);
        }
    }
    private void ApplyPassiveBuff(Hero hero)
    {
        foreach (var buff in _passiveBuff)
        {
            hero.StatModify.AddMultiplier(buff.BuffType, buff.IncreaseRatio);
        }
    }
    private void ReleasePassiveBuff(Hero hero)
    {
        foreach (var buff in _passiveBuff)
        {
            hero.StatModify.AddMultiplier(buff.BuffType, -buff.IncreaseRatio);
        }
    }
    #endregion

    #region Active Buff
    public IRuntimeEffectHandle ApplyBuff(BuffEffect buffEffect, IHeroStatModifier statModifier)
    {
        if (buffEffect == null)
            throw new ArgumentNullException(nameof(buffEffect));
        if (buffEffect.BuffData == null)
            throw new InvalidOperationException($"{nameof(BuffEffect)} has no {nameof(BuffData)}.");
        if (statModifier == null)
            throw new ArgumentNullException(nameof(statModifier));

        BuffEquipment handle = new BuffEquipment(
            statModifier,
            buffEffect.BuffData,
            disposedHandle => RemoveActiveBuff(statModifier, disposedHandle));

        AddActiveBuff(statModifier, handle);
        return handle;
    }

    private void AddActiveBuff(IHeroStatModifier statModifier, IRuntimeEffectHandle handle)
    {
        if (!_activeBuffs.TryGetValue(statModifier, out HashSet<IRuntimeEffectHandle> buffs))
        {
            buffs = new HashSet<IRuntimeEffectHandle>();
            _activeBuffs.Add(statModifier, buffs);
        }

        buffs.Add(handle);
    }

    private void RemoveActiveBuff(IHeroStatModifier statModifier, IRuntimeEffectHandle handle)
    {
        if (!_activeBuffs.TryGetValue(statModifier, out HashSet<IRuntimeEffectHandle> buffs))
            return;

        buffs.Remove(handle);

        if (buffs.Count == 0)
            _activeBuffs.Remove(statModifier);
    }

    private void ReleaseAllActiveBuff(Hero hero)
    {
        if (_activeBuffs.TryGetValue(hero.StatModify, out var buffs))
        {
            var handles = new List<IRuntimeEffectHandle>(buffs);

            foreach (IRuntimeEffectHandle handle in handles)
                handle.Dispose();
        }
    }
    #endregion

    private void OnDisable()
    {
        var handles = new List<IRuntimeEffectHandle>();

        foreach (HashSet<IRuntimeEffectHandle> activeBuffs in _activeBuffs.Values)
            handles.AddRange(activeBuffs);

        foreach (IRuntimeEffectHandle handle in handles)
            handle.Dispose();

        _activeBuffs.Clear();
    }
}
}
