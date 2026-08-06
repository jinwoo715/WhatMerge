using System;
using UnityEngine;
using WhatMerge.Combat.Effects;

namespace WhatMerge.Combat
{
    public class CombatService : ICombatService
    {
        private EffectProcessor _effectProcessor;

        public void Init(EffectProcessor effectProcessor)
        {
            _effectProcessor = effectProcessor
                ?? throw new ArgumentNullException(nameof(effectProcessor));
        }

        public void RegisterAttack(DamageContext damageContext)
        {
            if (_effectProcessor == null)
                throw new InvalidOperationException($"{nameof(CombatService)} is not initialized.");

            _effectProcessor.Process(damageContext);
        }

        public IRuntimeEffectHandle ApplyPersistentEffects(DamageContext damageContext)
        {
            if (_effectProcessor == null)
                throw new InvalidOperationException($"{nameof(CombatService)} is not initialized.");

            return _effectProcessor.ApplyPersistentEffects(damageContext);
        }
    }
}
