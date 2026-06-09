using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data {
    [CreateAssetMenu(fileName = "SummonItem", menuName = "Skill/Item/SummonItem", order = 0)]
    public class SummonData : ScriptableObject
    {
        public string SpriteName;
        public float LifeTime;

        public ESpawnPosition SpawnPosition;

        public SummonApplyTiming ApplyTiming;
        public TargetResolveData ResolveData;
        public SummonMove Move;

        public List<EffectBase> Effects;
    }

    [System.Serializable]
    public class SummonApplyTiming
    {
        public float Delay;
        public bool IsIntervalApply;
    }

    [System.Serializable]
    public class SummonMove
    {
        public EMove Move;
        public float Speed;
    }

    public enum EApplyTiming
    {
        AtStart,
        AtEnd,
        AtTime,
        AtInterval
    }
    public enum ESpawnPosition
    {
        TargetPivot,
        TargetUpper,
        TargetLower,
        TargetRight,
        TargetLeft,

        ScreenCenter,
    }
    public enum EMove
    {
        None,

        ToTarget,

        Attach,
    }
}
