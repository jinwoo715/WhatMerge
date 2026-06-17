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
        public void SpawnProjectile(Data.ProjectileData data, SkillPayload context);
    }

    public class TargetExecuterManager
    {
        private readonly SingleTargetExecuter _singleExecuter;
        private readonly AreaTargetExecuter _areaExecuter;

        public TargetExecuterManager(ICombatService combatService)
        {
            _singleExecuter = new SingleTargetExecuter(combatService);
            _areaExecuter = new AreaTargetExecuter(combatService);
        }

        public SkillExecuter GetExecuter(EffectTargetData data)
        {
            return data switch
            {
                SingleEffectTargetData => _singleExecuter,
                AreaEffectTargetData => _areaExecuter,
                _ => throw new System.Exception()
            };
        }
    }

    public class ProjectileSpawner : MonoBehaviour, IProjectileProvider
    {
        [SerializeField] private ProjectileItem _itemPrefab;

        private ISpriteRepository _spriteRepository;
        private ObjectPool<ProjectileItem> _projectileItemPool = new ObjectPool<ProjectileItem>();

        private Dictionary<EProjectileMoveType, Stack<IProjectileMoveStrategy>> _moveStretagy = new Dictionary<EProjectileMoveType, Stack<IProjectileMoveStrategy>>();

        private ICombatService _combatService;

        private TargetExecuterManager _executers;

        public void Init(ISpriteRepository spriteRepository, ICombatService combatService)
        {
            _spriteRepository = spriteRepository;
            _combatService = combatService;

            _projectileItemPool.Init(this.transform, _itemPrefab, 10);

            _executers = new TargetExecuterManager(combatService);
        }

        public Sprite GetProjectileSprite(string projectileData, int level)
        {
            string str = $"{projectileData}_{level}";
            var sp = _spriteRepository.GetSprite(str);
            return sp;
        }

        public IProjectileMoveStrategy GetMoveStretagy(EProjectileMoveType type)
        {
            if (_moveStretagy.TryGetValue(type, out var value))
            {
                if (value.Count > 0)
                    return value.Pop();
            }

            IProjectileMoveStrategy moveStretagy = default;

            switch (type)
            {
                case EProjectileMoveType.Line:
                    moveStretagy = new LinearMove();
                    break;
                case EProjectileMoveType.Homing:
                    moveStretagy = new HomingMove();
                    break;
                case EProjectileMoveType.Parabola:
                    moveStretagy = new Parabola();
                    break;
            }

            return moveStretagy;
        }

        //TODO
        public void SpawnProjectile(Data.ProjectileData data, SkillPayload context)
        {
            Debug.Log("Spawn");
            ProjectileItem obj = _projectileItemPool.GetItem(context.Attacker.Position);

            var move = GetMoveStretagy(data.MoveType);
            move.Init(obj.transform, context.Target, data.Speed);

            var projectileSprite = GetProjectileSprite(data.SpriteName, context.Attacker.EvolutionLevel);

            var executer = _executers.GetExecuter(data.ResolveData);

            obj.Init(context, move, data, projectileSprite, executer);
        }
    }
}
