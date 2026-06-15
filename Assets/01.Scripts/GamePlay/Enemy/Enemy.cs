using Entity;
using Map;
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

        public event Action<Enemy> OnDeath;
        public event Action<Enemy> OnReturn;
        public event Action<EnemyRewordType, int> OnOccuredReward;

        private EnemyData _data;
        private int _currentHP;

        public bool IsBoss => _data.IsBoss;
        public bool IsActive { get; private set; }
        public ElementType Element => ElementType.None;
        public int Armor => Mathf.RoundToInt(_stats.GetStat(EnemyStatType.Amour));
        public int CurrentHP => _currentHP;
        public Vector3 Position => this.transform.position;

        public void Initialize(IPathProvider pathProvider)
        {
            _move.OnDirectionChanged += FlipEnemy;
            _move.Initialize(transform, pathProvider);
            IsActive = true;
        }
        public void Init(EnemyData data, List<Sprite> sprites)
        {
            _data = data;
            _stats.SetBaseValue(EnemyStatType.HP, data.HP);
            _stats.SetBaseValue(EnemyStatType.Amour, data.Amour);
            _stats.SetBaseValue(EnemyStatType.MoveSpeed, data.MoveSpeed);

            _currentHP = Mathf.RoundToInt(_stats.GetStat(EnemyStatType.HP));
            _move.Init(_stats.GetStat(EnemyStatType.MoveSpeed));
            _spriteController.Init(sprites, 0.25f);
        }
        private void FlipEnemy(EMoveDirection moveDirection)
        {
            if (moveDirection == EMoveDirection.Up || moveDirection == EMoveDirection.Right)
                _spriteController.Flip(true);
            else
                _spriteController.Flip(false);
        }
        public void TakeDamage(AttackResultPayload resultPayload)
        {
            _currentHP -= resultPayload.Damage;

            if (_currentHP <= 0)
                Death();
        }
        private void Death()
        {
            OnDeath?.Invoke(this);
            OnReturn?.Invoke(this);
        }
        public void OnSpawn()
        {
            IsActive = true;
        }
        public void OnDespawn()
        {
            IsActive = false;
        }
        public RewardData GetRewardData()
        {
            var reward = new RewardData();
            reward.CompensationType = EnemyRewordType.Gold;
            reward.Value = _data.Coin;

            return reward;
        }
        public void SetAttribute(ElementType attributeType, float duration)
        {
            Debug.Log($"{attributeType} : {duration}");
        }
    }
}
