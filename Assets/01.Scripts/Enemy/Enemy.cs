using WhatMerge.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace WhatMerge.Enemies
{
    public interface IStatusHolder
    {
        StatusContainer Status { get; }
    }
    public class StatusContainer
    {
        private readonly int MaxStackElement = 5;

        Dictionary<ElementType, int> _elementCounts = new();

        public StatusContainer()
        {
            int count = Enum.GetValues(typeof(ElementType)).Length;

            for (int i = 0; i < count; i++)
            {
                _elementCounts.Add((ElementType)i, 0);
            }
        }

        public bool IsAddableStatus(ElementType elementType) => _elementCounts[elementType] >= MaxStackElement;
        public bool HasStatus(ElementType elementType) => _elementCounts[elementType] > 0;
        public void AddStatus(ElementType elementType) { }
        public void RemoveStatus(ElementType elementType) { }
    }

    public class Enemy : MonoBehaviour, IDamageable, IPooledItem<Enemy>, IRewardProvider, IStatusHolder
    {
        [SerializeField] private MoveController _move;
        [SerializeField] private EnemySpriteController _spriteController;

        private StatusContainer _status = new StatusContainer();
        private EnemyStats _stats = new EnemyStats();
        private EnemyData _data;
        private int _currentHP;

        public event Action<Enemy> OnDeath;
        public EnemyType Type => _data.EnemyType;
        public bool IsActive { get; private set; }
        public int Armor => Mathf.RoundToInt(_stats.GetStat(EnemyStatType.Armor));
        public int CurrentHP => _currentHP;
        public Vector3 Position => this.transform.position;
        public StatusContainer Status => _status;

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
