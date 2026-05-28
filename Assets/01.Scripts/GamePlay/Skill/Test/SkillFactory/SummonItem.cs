using Combat;
using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class SummonItem : MonoBehaviour, IPooledItem<SummonItem>
    {
        [SerializeField] private SpriteRenderer _renderer;

        private ISummonMoveStrategy _move;
        private float _currentTime;
        private SummonDataSO _summonData;
        private ISummonExecuteTimer _timer = new SummonExecuteTimer();
        private ProjectileEffectExecuter _executer;
        private ProjectileEventContext _data;
        public bool IsActive { get; set; }

        public event Action<SummonItem> OnReturn;

        internal void Init(ProjectileEventContext data, ProjectileEffectExecuter effectExecuter, ISummonMoveStrategy move, SummonDataSO summonData, Sprite sprite)
        {
            _move = move;
            _summonData = summonData;
            _currentTime = 0;
            _data = data;
            _executer = effectExecuter;
            _renderer.sprite = sprite;
            _timer.Init(summonData.ApplyTiming);
            _timer.OnExecute += Execute;
        }

        private void Update()
        {
            _currentTime += Time.deltaTime;

            _move.Tick();
            _timer.Tick();

            if(_currentTime >= _summonData.LifeTime)
            {
                OnReturn?.Invoke(this);
            }

        }

        private void Execute()
        {
            _executer.Execute(new ProjectileImpactContext(_data.Target, this.transform.position));
        }

        public void OnDespawn()
        {
            IsActive = false;
            _timer.OnExecute -= Execute;
        }
        public void OnSpawn()
        {
            IsActive = true;
        }
    }
}
