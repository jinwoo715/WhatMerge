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
        public EffectTargetData ResolveData;
        public SummonMove Move;

        public List<EffectBase> Effects;
    }

    [System.Serializable]
    public class SummonApplyTiming
    {
        public SummonApplyType ApplyType;
        public float Delay;
    }

    public enum SummonApplyType
    {
        Once,
        Interval
    }

    [System.Serializable]
    public class SummonMove
    {
        public SummonMoveType Move;
        public float Speed;
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
    public enum SummonMoveType
    {
        None,
        ToTarget,
        Attach,
    }
}
