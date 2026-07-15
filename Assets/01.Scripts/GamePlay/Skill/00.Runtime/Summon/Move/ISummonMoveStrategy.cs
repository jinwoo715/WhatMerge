using Combat;
using System;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Summon
{
    public interface ISummonMoveStrategy
    {
        event Action OnTargetLost;
        void Tick(float tick);
    }
}
