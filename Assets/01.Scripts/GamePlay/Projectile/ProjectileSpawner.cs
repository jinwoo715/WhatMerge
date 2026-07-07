using Combat;
using Enemies;
using Heros;
using Skill;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Infrastructure;

namespace Skill.Projectile
{
    public interface IProjectileProvider
    {
        public void SpawnProjectile(ProjectileDataBase data, SkillPayload context);
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

        public Sprite GetProjectileSprite(string projectileData, int level)
        {
            string str = $"{projectileData}_{level}";
            var sp = _spriteRepository.GetSprite(str);
            return sp;
        }

        public IProjectileMoveStrategy GetMoveStretagy(ProjectileDataBase data)
        {
            switch (data)
            {
                case StraightProjectileData:
                    return new LinearMove();
                case HomingProjectileData:
                    return new HomingMove();
                case ParabolaProjectileData:
                    return new Parabola();
                default:
                    throw new System.ArgumentException("Unsupported projectile data.");
            }
        }

        //TODO
        public void SpawnProjectile(ProjectileDataBase data, SkillPayload context)
        {
            Debug.Log("Spawn");
            ProjectileItem obj = _projectileItemPool.GetItem(context.Attacker.Position);

            var move = GetMoveStretagy(data);
            move.Init(obj.transform, context.Target, data.Speed);

            var projectileSprite = GetProjectileSprite(data.Sprite, context.Attacker.EvolutionLevel);

            obj.Init(context, move, data, projectileSprite, _combatService);
        }
    }
}
