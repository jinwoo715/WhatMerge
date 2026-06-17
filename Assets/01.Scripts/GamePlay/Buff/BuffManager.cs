using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Heros;

public interface IBuffRegister
{
    void RegisterBuff(BuffEffect timedBuffEffect, IHeroStatModifier statModifier);
}

//TODO
public class BuffManager : MonoBehaviour, IBuffRegister
{
    private Stack<BuffEquipment> _buffPool = new Stack<BuffEquipment>();
    IDataProvider _dataProvider;

    public void Init(IDataProvider dataProvider) 
    { 
        _dataProvider = dataProvider;
    }
    //public void RegisterBuff(int uid, IStatModifier statModifier)
    //{
    //    var buffDatas = _dataProvider.GetBuffDatas(uid);

    //    foreach (var item in buffDatas)
    //    {
    //        BuffEquipment buff = GetBuff();
    //        BuffPayload buffPayload = new BuffPayload(statModifier, item);
    //        StartCoroutine(buff.CoApplyBuff(buffPayload));
    //    }
    //}
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

    public void RegisterBuff(BuffEffect timedBuffEffect, IHeroStatModifier statModifier)
    {
        BuffEquipment buff = GetBuff();
        BuffPayload buffPayload = new BuffPayload(statModifier, timedBuffEffect);
        StartCoroutine(buff.CoApplyBuff(buffPayload));
    }
}
