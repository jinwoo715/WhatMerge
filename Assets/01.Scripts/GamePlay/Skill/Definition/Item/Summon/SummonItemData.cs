using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public abstract class SummonItemData : SpawnItemData
    {
        public string Sprite;
        public float Duration;
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
