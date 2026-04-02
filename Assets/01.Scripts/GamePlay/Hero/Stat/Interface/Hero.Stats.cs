using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stat
{
    public interface IHeroStatModifier
    {
        public event Action<EHeroStat, float> OnStatChange;
        void SetBaseValue(EHeroStat stat, float value);
        void AddFixedStatValue(EHeroStat stat, float value);
        void AddMultiplyValue(EHeroStat stat, float value);
    }
    public interface IHeroStatReadOnly
    {
        float GetStat(EHeroStat stat);
    }
}
