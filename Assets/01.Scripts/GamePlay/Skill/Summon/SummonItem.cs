using Combat;
using Skill.Data;
using Skill.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace Skill.Summon  
{
    public class SummonItem : MonoBehaviour, IPooledItem<SummonItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private SummonExecuteTimer _timer;
        private SkillExecuter _executer;

        private SummonData _summonData;
        private ISummonMoveStrategy _move;
        private SkillPayload _payload;

        public bool IsActive { get; private set; }
        public event Action<SummonItem> OnReturn;
        private Action OnTimeOut;

        public void Initialize(SkillExecuter executer, SummonExecuteTimer timer)
        {
            _executer = executer;

            _timer = timer;
            _timer.OnExecute += Execute;

            OnTimeOut += () => OnReturn?.Invoke(this);

            _timer.OnTimeOut += TimeOut;
        }
        public void TimeOut()
        {
            OnReturn?.Invoke(this);
            Debug.Log("TimeOut");
        }

        internal void Init(SkillPayload payload, ISummonMoveStrategy move, SummonData summonData, Sprite sprite)
        {
            _move = move;
            _summonData = summonData;
            _renderer.sprite = sprite;
            _payload = payload;

            _move.OnLooseTarget += TimeOut;

            _timer.Init(_summonData.LifeTime, summonData.ApplyTiming);

            _executer.SetData(summonData.ResolveData, _payload);
        }

        private void Update()
        {
            _move.Tick();
            _timer.Tick();
        }

        private void Execute()
        {
            _executer.Execute(new SkillImpactContext(_payload.Target as IDamageable, this.transform.position));
        }

        public void OnDespawn()
        {
            IsActive = false;
            _move.OnLooseTarget -= TimeOut;
        }
        public void OnSpawn()
        {
            IsActive = true;
        }
    }
}

