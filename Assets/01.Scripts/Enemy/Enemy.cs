using WhatMerge.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace WhatMerge.Enemies
{
    public class Enemy : MonoBehaviour, IDamageable, IPooledItem<Enemy>, IRewardProvider
    {
        [SerializeField] private MoveController _move;
        [SerializeField] private EnemySpriteController _spriteController;

        private EnemyStats _stats = new EnemyStats();
        private EnemyData _data;
        private int _currentHP;

        public event Action<Enemy> OnDeath;

        public bool IsBoss => _data.IsBoss;
        public bool IsActive { get; private set; }
        public int Armor => Mathf.RoundToInt(_stats.GetStat(EnemyStatType.Armor));
        public int CurrentHP => _currentHP;
        public Vector3 Position => this.transform.position;

        public void Initialize(IPathProvider pathProvider)
        {
            _move.OnDirectionChanged += _spriteController.SetDirection;
            _move.Initialize(transform, pathProvider);

            _stats.OnChangedStat += (type, speed) =>
            {
                if (type == EnemyStatType.MoveSpeed)
                {
                    _move.UpdateSpeed(speed);
                }
            };
        }
        public void Init(EnemyData data, List<Sprite> sprites)
        {
            _data = data;
            _stats.SetBaseValue(EnemyStatType.MaxHP, data.HP);
            _stats.SetBaseValue(EnemyStatType.Armor, data.Amour);
            _stats.SetBaseValue(EnemyStatType.MoveSpeed, data.MoveSpeed);

            _currentHP = Mathf.RoundToInt(_stats.GetStat(EnemyStatType.MaxHP));
            _move.Init(_stats.GetStat(EnemyStatType.MoveSpeed));
            
            _spriteController.Init(sprites, 0.25f);

            IsActive = true;
        }
        public void TakeDamage(AttackResultPayload resultPayload)
        {
            if (!IsActive)
                return;

            _currentHP -= resultPayload.Damage;

            _currentHP = Mathf.Clamp(_currentHP, 0, (int)_stats.GetStat(EnemyStatType.MaxHP));

            if (_currentHP <= 0)
                Death();
        }
        private void Death()
        {
            IsActive = false;
            OnDeath?.Invoke(this);
        }
        public RewardData GetRewardData()
        {
            var reward = new RewardData();
            reward.CompensationType = EnemyRewordType.Gold;
            reward.Value = _data.Coin;

            return reward;
        }
        public void OnSpawn() { }
        public void OnDespawn() { }
    }
}
