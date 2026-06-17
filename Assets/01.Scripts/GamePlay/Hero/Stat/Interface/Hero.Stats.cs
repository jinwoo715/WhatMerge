using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Heros
{
    public interface IHeroStatModifier
    {
        public event Action<HeroStatType, float> OnStatChanged;
        void AddFixedValue(HeroStatType type, float value);
        void AddMultiplier(HeroStatType type, float value);
    }
    public interface IHeroStatReadOnly
    {
        float GetStat(HeroStatType stat);
    }
}
