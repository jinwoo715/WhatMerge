using UnityEngine;
using System;
using WhatMerge.Combat;
using WhatMerge.Heros;
using WhatMerge.Infrastructure;
using WhatMerge.Projectiles.Data;

namespace WhatMerge.Projectiles
{
    public interface IProjectileProvider
    {
        public void SpawnProjectile(ProjectileDataBase data, DamageContext context);
    }

    public class ProjectileSpawner : MonoBehaviour, IProjectileProvider
    {
        [SerializeField] private ProjectileItem _itemPrefab;

        private ISpriteRepository _spriteRepository;
        private ObjectPool<ProjectileItem> _projectileItemPool = new ObjectPool<ProjectileItem>();
        private ICombatService _combatService;

        public void Init(ISpriteRepository spriteRepository, ICombatService combatService)
        {
            _spriteRepository = spriteRepository;
            _combatService = combatService;

            _projectileItemPool.OnCreateEvent += (item) => { item.OnReturn += ReturnToPool; };
            _projectileItemPool.Init(this.transform, _itemPrefab, 10);
        } 
        public void SpawnProjectile(ProjectileDataBase data, DamageContext context)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Target == null)
                throw new InvalidOperationException("Projectile requires a target.");
            if (context.Target is not IDamageable)
            {
                throw new InvalidOperationException(
                    $"Projectile requires an {nameof(IDamageable)} target. " +
                    $"Received: {context.Target.GetType().Name}.");
            }
            if (!context.Target.IsActive)
                return;

            if (context.Attacker is not Hero attacker)
            {
                throw new InvalidOperationException(
                    $"Projectile requires a {nameof(Hero)} attacker. " +
                    $"Received: {context.Attacker?.GetType().Name ?? "null"}.");
            }

            ValidatePositiveFinite(data.Speed, nameof(data.Speed), data.name);
            ValidatePositiveFinite(data.LifeTime, nameof(data.LifeTime), data.name);

            ProjectileItem obj = _projectileItemPool.GetItem(attacker.Position);

            var move = GetMoveStretagy(data, obj.transform, context.Target);

            var projectileSprite = GetProjectileSprite(data.Sprite, attacker.EvolutionLevel);

            IDisposable effectLifetimeLease = context.RetainEffectLifetime();

            try
            {
                obj.Init(context, move, data, projectileSprite, _combatService, effectLifetimeLease);
                effectLifetimeLease = null;
            }
            finally
            {
                effectLifetimeLease?.Dispose();
            }
        }
        public IProjectile GetMoveStretagy(ProjectileDataBase data, Transform item, ICombatant target)
        {
            switch (data)
            {
                case StraightProjectileData straightProjectileData:
                    return new LinearMove(straightProjectileData, item, target);
                case HomingProjectileData homingProjectileData:
                    return new HomingMove(item, target, data.Speed);
                case ParabolaProjectileData parabolaProjectileData:
                    return new Parabola(parabolaProjectileData, item, target);
                default:
                    throw new System.ArgumentException("Unsupported projectile data.");
            }
        }
        private Sprite GetProjectileSprite(string projectileData, int level)
        {
            string str = $"{projectileData}_{level}";
            var sp = _spriteRepository.GetSprite(str);
            return sp;
        }

        private static void ValidatePositiveFinite(float value, string fieldName, string dataName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    fieldName,
                    value,
                    $"Projectile '{dataName}' {fieldName} must be a finite number greater than zero.");
            }
        }
    
        private void ReturnToPool(ProjectileItem returnItem)
        {
            _projectileItemPool.ReturnItem(returnItem);
        }
    }
}
