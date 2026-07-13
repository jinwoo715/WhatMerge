using System;
using UnityEngine;

namespace WhatMerge.Combat
{
    public class CombatService : ICombatService
    {
        private EffectProcessor _effectProcessor;

        public event Action<Vector3, int> OnApplyDamage;

        public void Init(EffectProcessor effectProcessor)
        {
            if (_effectProcessor != null)
                _effectProcessor.OnApplyDamage -= HandleApplyDamage;

            _effectProcessor = effectProcessor;
            _effectProcessor.OnApplyDamage += HandleApplyDamage;
        }

        public void RegisterAttack(DamageContext damageContext)
        {
            _effectProcessor?.Process(damageContext);
        }

        private void HandleApplyDamage(Vector3 position, int damage)
        {
            OnApplyDamage?.Invoke(position, damage);
        }
    }
}
