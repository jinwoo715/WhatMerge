using UnityEngine;
using System;
using WhatMerge.Combat;
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
        private IFatalStopService _fatalStop;

        public void Init(
            ISpriteRepository spriteRepository,
            ICombatService combatService,
            IFatalStopService fatalStop)
        {
            _spriteRepository = spriteRepository;
            _combatService = combatService;
            _fatalStop = fatalStop ?? throw new ArgumentNullException(nameof(fatalStop));

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
            if (!context.Target.IsActive)
                return;

            ValidatePositiveFinite(data.Speed, nameof(data.Speed), data.name);
            ValidatePositiveFinite(data.LifeTime, nameof(data.LifeTime), data.name);
            ValidateFinite(data.RotationOffset, nameof(data.RotationOffset), data.name);

            ProjectileItem obj = _projectileItemPool.GetItem(context.SourcePosition);
            IProjectile move = null;
            IDisposable effectLifetimeLease = null;

            try
            {
                move = GetMoveStretagy(data, obj.transform, context.Target);
                Sprite projectileSprite = GetProjectileSprite(
                    data.Sprite,
                    context.SourceEvolutionLevel);
                effectLifetimeLease = context.RetainEffectLifetime();

                obj.Init(
                    context.WithoutTarget(),
                    move,
                    data,
                    projectileSprite,
                    _combatService,
                    effectLifetimeLease,
                    _fatalStop);

                move = null;
                effectLifetimeLease = null;
            }
            catch (Exception exception)
            {
                TryDispose(move);
                TryDispose(effectLifetimeLease);
                TryReturnProjectile(obj);
                _fatalStop.FatalStop(exception, $"Projectile spawn failed. Data:{data.name}.");
                throw;
            }
        }
        public IProjectile GetMoveStretagy(ProjectileDataBase data, Transform item, ICombatant target)
        {
            switch (data)
            {
                case StraightProjectileData straightProjectileData:
                    return new LinearMove(straightProjectileData, item, target);
                case HomingProjectileData homingProjectileData:
                    return new HomingMove(homingProjectileData, item, target);
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

        private static void ValidateFinite(float value, string fieldName, string dataName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    fieldName,
                    value,
                    $"Projectile '{dataName}' {fieldName} must be a finite number.");
            }
        }
    
        private void ReturnToPool(ProjectileItem returnItem)
        {
            _projectileItemPool.ReturnItem(returnItem);
        }

        private void TryReturnProjectile(ProjectileItem item)
        {
            try
            {
                if (item != null && item.IsActive)
                    _projectileItemPool.ReturnItem(item);
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }
        }

        private static void TryDispose(IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }
        }
    }
}
