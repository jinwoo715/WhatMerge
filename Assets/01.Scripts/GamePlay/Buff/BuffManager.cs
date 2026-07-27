using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Heros;

public interface IBuffService
{
    void EquipedBuff(BuffEffect timedBuffEffect, float duration, IHeroStatModifier combatant);
    void RegisterPassiveBuff(BuffData buff);
    void UnRegisterPassiveBuff(BuffData buff);

}

//TODO
public class BuffManager : MonoBehaviour, IBuffService
{
    private Stack<BuffEquipment> _buffPool = new Stack<BuffEquipment>();
    private List<BuffData> _passiveBuff = new List<BuffData>();
    private Dictionary<IHeroStatModifier, List<BuffEquipment>> _activeBuffs = new();

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
    public void EquipedBuff(BuffEffect timedBuffEffect, float duration, IHeroStatModifier statModifier)
    {
        BuffEquipment buff = GetBuff();
        BuffPayload buffPayload = new BuffPayload(statModifier, timedBuffEffect.BuffData);

        Coroutine co = StartCoroutine(CoEquippedBuff(duration, statModifier, buff));

        buff.AppplyBuff(buffPayload, co);

        AddActiveBuff(statModifier, buff);
    }
    private IEnumerator CoEquippedBuff(float duration, IHeroStatModifier combatant, BuffEquipment buffEquipment)
    {
        yield return new WaitForSeconds(duration);

        buffEquipment.ReleaseBuff();

        RemoveActiveBuff(combatant, buffEquipment);
    }
    private void AddActiveBuff(IHeroStatModifier combatant, BuffEquipment buffEquipment)
    {
        if (!_activeBuffs.ContainsKey(combatant))
            _activeBuffs.Add(combatant, new List<BuffEquipment>());

        _activeBuffs[combatant].Add(buffEquipment);
    }
    private void RemoveActiveBuff(IHeroStatModifier combatant, BuffEquipment buffEquipment)
    {
        _activeBuffs[combatant].Remove(buffEquipment);

        if (_activeBuffs[combatant].Count == 0)
            _activeBuffs.Remove(combatant);
    }
    private void ReleaseAllActiveBuff(Hero hero)
    {
        if (_activeBuffs.TryGetValue(hero.StatModify, out var buffs))
        {
            foreach (var buff in buffs)
            {
                StopCoroutine(buff.Coroutine);
                buff.ReleaseBuff();
            }

            _activeBuffs.Remove(hero.StatModify);
        }
    }
    #endregion


    private BuffEquipment GetBuff()
    {
        if (_buffPool.Count > 0)
            return _buffPool.Pop();

        return SpawnBuff();
    }
    private BuffEquipment SpawnBuff()
    {
        BuffEquipment buff = new BuffEquipment();
        buff.OnEndBuff += ReturnBuff;
        return buff;
    }
    private void ReturnBuff(BuffEquipment buff)
    {
        _buffPool.Push(buff);
    }
}
