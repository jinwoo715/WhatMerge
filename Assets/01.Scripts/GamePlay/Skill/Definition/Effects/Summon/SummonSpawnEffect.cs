using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public enum TargetLostEventType
    {
        Disappear,
        OnExecute
    }

    public class SummonSpawnEffect : NomalEffect
    {
        public float DurationTime;
        public SummonMove Move;
        public SummonExecution Execution;

        public override void AddStat(string key, float value)
        {
            throw new System.NotImplementedException();
        }
    }

    [System.Serializable]
    public class SummonExecution : ScriptableObject { }

    [System.Serializable]
    public class SummonOnceExecution : SummonExecution
    {
        public NomalEffect Effect;
    }

    [System.Serializable]
    public class OnExpireExecutionSummon : SummonOnceExecution
    {
    }

    [System.Serializable]
    public class OnTickExecutionSummon : SummonOnceExecution
    {
        public float TickTime;
    }

    [System.Serializable]
    public class OnStayExecutionSummon : SummonExecution 
    {
        public DurationEffectBase DurationEffect;
    }

    [System.Serializable]
    public class OnEnterExecutionSummon : SummonOnceExecution
    {
    }
    

    [System.Serializable]
    public class SummonMove : ScriptableObject { }

    [System.Serializable]
    public class SummonNoneMove : SummonMove { }

    [System.Serializable]
    public class SummonFollowMove : SummonMove 
    {
        public TargetLostEventType LostTargetEvent;
    }

    [System.Serializable]
    public class SummonAttachMove : SummonFollowMove { }

    [System.Serializable]
    public class SummonApproachMove : SummonFollowMove { }
}

