using Combat;
using Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    public class Enemy : MonoBehaviour, IDamageable, IPooledItem<Enemy>
    {
        [SerializeField] private MoveController _move;
        [SerializeField] private EnemySpriteController _spriteController;

        public event Action<Enemy> OnDeath;
        public event Action<Enemy> OnReturn;

        private int _currentMoveDestinationIndex = 0;
        public int CurrentMoveDestinationIndex => _currentMoveDestinationIndex;

        public Vector3 Position => this.transform.position;
        public int CurrentHP => _currentHP;
        public int Amour => (int)_data.Amour;
        public EnemyData _data;

        public bool IsBoss => _data.IsBoss;

        public EAttribute Attribute => EAttribute.None;

        public bool IsActive => true;

        private int _currentHP;

        public void Initialize(IPathProvider pathProvider)
        {
            _move.OnDirectionChanged += FlipEnemy;
            _move.Initialize(transform, pathProvider);
        }

        public void Init(EnemyData data, List<Sprite> sprites)
        {
            _data = data;
            _currentHP = (int)_data.HP;
            _currentMoveDestinationIndex = 0;
            _move.Init(data.MoveSpeed);
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

            if (_currentHP < 0)
                Death();
        }
        private void Death()
        {
            OnDeath?.Invoke(this);
        }
        public void OnSpawn() { }
        public void OnDespawn() { }
    }
}
