using WhatMerge.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;

namespace WhatMerge.Enemies
{
    public class Enemy : MonoBehaviour, IDamageable, IPooledItem<Enemy>, IRewardProvider, IEnemyStatModifier
    {
        private MoveController _move;
        [SerializeField] private EnemySpriteController _spriteController;
        [SerializeField] private Transform _healthBarAnchor;
        [SerializeField, Min(0f)] private float _healthBarPadding = 0;

        private readonly CombatantElement _element = new CombatantElement();
        private readonly StatusContainer _status = new StatusContainer();
        private readonly EnemyStats _stats = new EnemyStats();
        private EnemyData _data;
        private ElementType _baseAttribute;
        private int _currentHP;

        public event Action<Enemy> OnDeath;
        public event Action<ICombatant> OnActiveOff;
        public event Action<int> OnAppliedNomalDamage;
        public event Action<int, int> OnHealthChanged;

        public int UID => _data.UID;
        public EnemyType Type => _data.EnemyType;
        public bool IsActive { get; private set; }
        public int Armor => Mathf.RoundToInt(_stats.GetStat(EnemyStatType.Armor));
        public int CurrentHP => _currentHP;
        public int MaxHP => Mathf.RoundToInt(_stats.GetStat(EnemyStatType.MaxHP));
        public ElementType BaseAttribute => _baseAttribute;
        public IStatusReader TemporaryAttributes => _status;
        public IStatusModifier TemporaryAttributeModifier => _status;
        public Vector3 Position => this.transform.position;
        public Vector3 HealthBarPosition => _healthBarAnchor != null
            ? _healthBarAnchor.position
            : transform.position;
        public int LifeCycleVersion { get; private set; }
        public IEnemyStatModifier StatModifier => _stats;
        public IMoveable Move => _move;
        public IElement Element => _element;
        private void Update()
        {
            _move?.UpdateDeltatime(Time.deltaTime);
        }
        public void Initialize(IPathProvider pathProvider)
        {
            if (pathProvider == null)
                throw new ArgumentNullException(nameof(pathProvider));
            if (_spriteController == null)
                throw new InvalidOperationException($"{nameof(EnemySpriteController)} is not assigned.");
            if (float.IsNaN(_healthBarPadding) || float.IsInfinity(_healthBarPadding) || _healthBarPadding < 0f)
                throw new InvalidOperationException("Health bar padding must be finite and non-negative.");
            if (_move != null)
                throw new InvalidOperationException($"{nameof(Enemy)} is already initialized.");

            _move = new MoveController(transform, pathProvider);
            _move.OnDirectionChanged += _spriteController.SetDirection;

            _stats.OnChangedStat += HandleStatChanged;
        }
        public void Init(EnemyData data, List<Sprite> sprites)
        {
            ValidateInitializationData(data, sprites);

            if (_move == null)
                throw new InvalidOperationException($"Call {nameof(Initialize)} before {nameof(Init)}.");
            if (IsActive)
                throw new InvalidOperationException("An active enemy cannot be initialized again.");

            _status.Clear();
            _data = data;
            _baseAttribute = data.Attribute;
            _element.Clear();

            if (_baseAttribute != ElementType.None)
                _element.GetElement(_baseAttribute);

            _stats.SetBaseValue(EnemyStatType.MaxHP, data.MaxHP);
            _stats.SetBaseValue(EnemyStatType.Armor, data.Armor);
            _stats.SetBaseValue(EnemyStatType.MoveSpeed, data.MoveSpeed);

            _currentHP = Mathf.RoundToInt(_stats.GetStat(EnemyStatType.MaxHP));
            _move.Init(_stats.GetStat(EnemyStatType.MoveSpeed));
            
            _spriteController.Init(sprites, 0.25f);
            UpdateHealthBarAnchor();

            LifeCycleVersion++;
            IsActive = true;
            _move.ActiveOn();
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        private void UpdateHealthBarAnchor()
        {
            if (_healthBarAnchor == null)
                return;

            _healthBarAnchor.position = _spriteController.GetAnimationTopPosition(_healthBarPadding);
        }

        public void TakeDamage(AttackResultPayload resultPayload)
        {
            if (!IsActive)
                return;

            if(resultPayload.ResultType == DamageResultType.NomalDamage)
                OnAppliedNomalDamage?.Invoke(resultPayload.Damage);

            _currentHP -= resultPayload.Damage;

            _currentHP = Mathf.Clamp(_currentHP, 0, (int)_stats.GetStat(EnemyStatType.MaxHP));
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);

            if (_currentHP <= 0)
                Death();
        }
        private void HandleStatChanged(EnemyStatType type, float value)
        {
            if (type == EnemyStatType.MoveSpeed)
            {
                _move.UpdateSpeed(value);
                return;
            }

            if (type != EnemyStatType.MaxHP || !IsActive)
                return;

            _currentHP = Mathf.Clamp(_currentHP, 0, MaxHP);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }
        private void Death()
        {
            if (!Deactivate())
                return;

            OnDeath?.Invoke(this);
        }

        private bool Deactivate()
        {
            if (!IsActive)
                return false;

            IsActive = false;
            _status.Clear();
            _element.Clear();
            _move?.ActiveOff();
            OnActiveOff?.Invoke(this);
            return true;
        }

        private static void ValidateInitializationData(EnemyData data, List<Sprite> sprites)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.UID <= 0)
                throw new ArgumentOutOfRangeException(nameof(data), data.UID, "Enemy UID must be greater than zero.");
            if (string.IsNullOrWhiteSpace(data.Name))
                throw new ArgumentException("Enemy name is required.", nameof(data));
            if (string.IsNullOrWhiteSpace(data.SpriteKey))
                throw new ArgumentException("Enemy sprite key is required.", nameof(data));
            if (!Enum.IsDefined(typeof(EnemyType), data.EnemyType))
                throw new ArgumentOutOfRangeException(nameof(data), data.EnemyType, "EnemyType must be a defined value.");
            if (float.IsNaN(data.MaxHP) || float.IsInfinity(data.MaxHP) || data.MaxHP <= 0f)
                throw new ArgumentOutOfRangeException(nameof(data), data.MaxHP, "Enemy MaxHP must be greater than zero.");
            if (float.IsNaN(data.Armor) || float.IsInfinity(data.Armor) || data.Armor < 0f)
                throw new ArgumentOutOfRangeException(nameof(data), data.Armor, "Enemy Armor cannot be negative.");
            if (float.IsNaN(data.MoveSpeed) || float.IsInfinity(data.MoveSpeed) || data.MoveSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(data), data.MoveSpeed, "Enemy MoveSpeed must be greater than zero.");
            if (!Enum.IsDefined(typeof(ElementType), data.Attribute))
                throw new ArgumentOutOfRangeException(nameof(data), data.Attribute, "Enemy Attribute must be a single defined value.");
            if (data.KillGold < 0)
                throw new ArgumentOutOfRangeException(nameof(data), data.KillGold, "Enemy KillGold cannot be negative.");
            if (data.RewardGroupUID < 0)
                throw new ArgumentOutOfRangeException(nameof(data), data.RewardGroupUID, "Enemy RewardGroupUID cannot be negative.");
            if (sprites == null)
                throw new ArgumentNullException(nameof(sprites));
            if (sprites.Count < 3)
                throw new ArgumentException("Enemy movement animation requires at least three sprites.", nameof(sprites));

            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] == null)
                    throw new ArgumentException($"Enemy sprite at index {i} is null.", nameof(sprites));
            }
        }

        public int KillGold => _data.KillGold;
        public int RewardGroupUID => _data.RewardGroupUID;
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
            Deactivate();
        }
        public void KnockBack(float distance)
        {
            _move.Knockback(distance);
        }
    }
}
