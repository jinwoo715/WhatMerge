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
            _effectProcessor = effectProcessor;
        }

        public void RegisterAttack(DamageContext damageContext)
        {
            _effectProcessor?.Process(damageContext);
        }
    }
}
