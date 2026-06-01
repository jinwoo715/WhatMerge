using Skill;
using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffPayload
{
    public IStatModifier StatModifier { get; private set; }
    public TimeBuffEffect BuffData { get; private set; }

    public BuffPayload(IStatModifier statModifier, TimeBuffEffect buffData)
    {
        StatModifier = statModifier;
        BuffData = buffData;
    }
}

public class BuffEquipment
{
    public Action<BuffEquipment> OnEndBuff;

    public IEnumerator CoApplyBuff(BuffPayload buffPayload)
    {
        var data = buffPayload.BuffData;

        Debug.Log(buffPayload);
        Debug.Log(buffPayload.StatModifier);
        Debug.Log(buffPayload.BuffData);
        buffPayload.StatModifier.ModifyStat(data.BuffType, data.IncreaseRatio);
        yield return new WaitForSeconds(data.Time);

        buffPayload.StatModifier.ModifyStat(data.BuffType, -data.IncreaseRatio);
        OnEndBuff?.Invoke(this);
    }

    //private void ApplyBuff(List<BuffData> buffDatas, IStatModifier target)
    //{
    //    for (int i = 0; i < buffDatas.Count; i++)
    //    {
    //        target.ModifyStat(buffDatas[i].BuffValue);
    //    }
    //}

    //private void RevertBuff(List<BuffData> buffDatas, IStatModifier target)
    //{
    //    for (int i = 0; i < buffDatas.Count; i++)
    //    {
    //        target.ModifyStat(-buffDatas[i].BuffValue);
    //    }
    //}
}
