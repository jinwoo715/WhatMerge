using Combat;
using Enemies;
using Heros;
using Skill;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Heros;
using WhatMerge.Infrastructure;

namespace Skill.Projectile
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

            _projectileItemPool.Init(this.transform, _itemPrefab, 10);
        } 
        public void SpawnProjectile(ProjectileDataBase data, DamageContext context)
        {
            if (context == null || context.Target == null)
                return;

            if (context.Attacker is not Hero attacker)
                return;

            ProjectileItem obj = _projectileItemPool.GetItem(attacker.Position);

            var move = GetMoveStretagy(data, obj.transform, attacker);

            var projectileSprite = GetProjectileSprite(data.Sprite, attacker.EvolutionLevel);

            obj.Init(context, move, data, projectileSprite, _combatService);
        }
        public IProjectileMoveStrategy GetMoveStretagy(ProjectileDataBase data, Transform item, ICombatant target)
        {
            switch (data)
            {
                case StraightProjectileData:
                    return new LinearMove(item, target, data.Speed);
                case HomingProjectileData:
                    return new HomingMove(item, target, data.Speed);
                case ParabolaProjectileData:
                    return new Parabola();
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
    }
}
