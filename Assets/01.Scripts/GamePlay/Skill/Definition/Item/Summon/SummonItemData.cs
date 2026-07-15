using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public abstract class SummonItemData : SpawnItemData
    {
        public string Sprite;
        public float Duration;

        public ESpawnPosition SpawnPosition;
        public SummonMoveType MoveType;
    }
    public enum SummonMoveType
    {
        None,
        Follow,
        Close
    }
    public enum ESpawnPosition
    {
        TargetPivot,
        TargetUpper,
        TargetLower,
        TargetRight,
        TargetLeft,
    }
}
