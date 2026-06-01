using Combat;
using System;
using UnityEngine;

namespace Skill.Summon
{
    public interface ISummonMoveStrategy
    {
        event Action OnLooseTarget;
        void Init(Transform owner, ICreature target, float speed);
        void Tick();
    }
    public abstract class SummonMoveStrategy : ISummonMoveStrategy
    {
        public event Action OnLooseTarget;
        public abstract void Init(Transform owner, ICreature target, float speed);
        public abstract void Tick();
    }
}