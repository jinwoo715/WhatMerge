using Combat;
using Enemies;
using Heros;
using Skill;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Projectile
{
    public interface IProjectileProvider
    {
        public void SpawnProjectile(ProjectileDataSO data, SkillPayload context);
    }

    public class ProjectileSpawner : MonoBehaviour, IProjectileProvider
    {
        [SerializeField] private ProjectileItem _itemPrefab;

        private ISpriteRepository _spriteRepository;
        private ObjectPool<ProjectileItem> _projectileItemPool = new ObjectPool<ProjectileItem>();

        private Dictionary<EProjectileMoveType, Stack<IProjectileMoveStrategy>> _moveStretagy = new Dictionary<EProjectileMoveType, Stack<IProjectileMoveStrategy>>();

        private ICombatService _combatService;

        public void Init(ISpriteRepository spriteRepository, ICombatService combatService)
        {
            _spriteRepository = spriteRepository;
            _combatService = combatService;

            _projectileItemPool.OnCreateEvent += CreateProjectile;
            _projectileItemPool.Init(this.transform, _itemPrefab, 10);
        }

        private void CreateProjectile(ProjectileItem item)
        {
            SkillExecuter skillExecuter = new SkillExecuter(_combatService);
            item.Initialize(skillExecuter);
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
        public void SpawnProjectile(ProjectileDataSO data, SkillPayload context)
        {
            ProjectileItem obj = _projectileItemPool.GetItem(context.Attacker.Position);

            Debug.Log($"Spawner {context.effects.Count}");

            var move = GetMoveStretagy(data.MoveType);
            move.Init(obj.transform, context.Target, data.Speed);

            var projectileSprite = GetProjectileSprite(data.SpriteName, context.Attacker.EvolutionLevel);

            obj.Init(context, move, data, projectileSprite);
        }
    }
}
