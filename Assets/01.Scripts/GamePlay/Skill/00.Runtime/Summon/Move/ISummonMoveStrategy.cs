using Combat;
using Skill.Data;
using System;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Summon
{
    public interface ISummonMoveStrategy
    {
        event Action<TargetLostEventType> OnTargetLost;
        void Tick(float tick);
    }


}
