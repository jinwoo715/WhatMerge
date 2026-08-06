using UnityEngine;
using WhatMerge.Summons.Data;

namespace Skill.Data
{
    public class SummonSpawnEffect : NormalEffect
    {
        public float DurationTime;
        public SummonMove Move;
        public SummonExecutionData Execution;
        public ESpawnPosition SpawnPosition;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }
}

