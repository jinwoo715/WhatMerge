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

        public bool IsAddableStatus(ElementType elementType) => _elementCounts[elementType] < MaxStackElement;
        public bool HasStatus(ElementType elementType) => _elementCounts[elementType] > 0;
        public void AddStatus(ElementType elementType)
        {
            if (!IsAddableStatus(elementType))
                return;

            _elementCounts[elementType]++;
        }
        public void RemoveStatus(ElementType elementType)
        {
            _elementCounts[elementType] = Mathf.Max(0, _elementCounts[elementType] - 1);
        }
        public void Clear()
        {
            foreach (ElementType elementType in Enum.GetValues(typeof(ElementType)))
            {
                _elementCounts[elementType] = 0;
            }
        }
    }

    public class Enemy : MonoBehaviour, IDamageable, IPooledItem<Enemy>, IRewardProvider, IStatusHolder, IEnemyStatModifier
    {
        [SerializeField] private MoveController _move;
        [SerializeField] private EnemySpriteController _spriteController;

        private CombatantElement _element = new CombatantElement();
        private StatusContainer _status = new StatusContainer();
        private EnemyStats _stats = new EnemyStats();
        private EnemyData _data;
        private int _currentHP;

        public event Action<Enemy> OnDeath;
        public event Action<ICombatant> OnActiveOff;

        public EnemyType Type => _data.EnemyType;
        public bool IsActive { get; private set; }
        public int Armor => Mathf.RoundToInt(_stats.GetStat(EnemyStatType.Armor));
        public int CurrentHP => _currentHP;
        public int MaxHP => Mathf.RoundToInt(_stats.GetStat(EnemyStatType.MaxHP));
        public Vector3 Position => this.transform.position;
        public StatusContainer Status => _status;
        public int LifeCycleVersion { get; private set; }

        public IEnemyStatModifier StatModifier => _stats;

        public IMoveable Move => _move;

        public IElement Element => _element;

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
            LifeCycleVersion++;
            _status.Clear();
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
            OnActiveOff?.Invoke(this);
            OnDeath?.Invoke(this);
        }
        public RewardData GetRewardData()
        {
            var reward = new RewardData();
            reward.CompensationType = EnemyRewordType.Gold;
            reward.Value = _data.Coin;

            return reward;
        }
        public void AddFixedValue(EnemyStatType type, float value)
        {
            _stats.AddFixedValue(type, value);
        }
        public void AddMultiplier(EnemyStatType type, float value)
        {
            _stats.AddMultiplier(type, value);
        }
        public void OnSpawn() { }
        public void OnDespawn()
        {
            IsActive = false;
            _status.Clear();
            _element.Clear();
        }
    }
}
