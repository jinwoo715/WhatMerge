using Combat;
using Enemies;
using Heros;
using Skill;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public interface IProjectileProvider
    {
        public void SpawnProjectile(ProjectileDataSO data, ProjectileEventContext context);
    }

    public interface ISummonProvider
    {
        public void SpawnSummon(SummonDataSO dataSO, ProjectileEventContext context);
    }

    public class ProjectilePayload
    {
        public IAttackable Attacker;
        public IDamageable Target;
        public ICombatService attackRegister;
        public IAttackStatProvider attackStatProvider;
        public Vector3 SpawnPos;
        public int UID;
        public int HeroLevel;
        public int DMGValue;
        public string VFX;
    }

    public class ProjectileSpawner : MonoBehaviour, IProjectileProvider
    {
        [SerializeField] private ProjectileItem _itemPrefab;

        private ISpriteRepository _spriteRepository;
        private ObjectPool<ProjectileItem> _projectileItemPool = new ObjectPool<ProjectileItem>();

        private Dictionary<EProjectileMoveType, Stack<IMoveStretagy>> _moveStretagy = new Dictionary<EProjectileMoveType, Stack<IMoveStretagy>>();

        private ICombatService _combatService;

        private void Start()
        {
            _projectileItemPool.Init(this.transform, _itemPrefab, 10);
        }

        public void Init(ISpriteRepository spriteRepository, ICombatService combatService)
        {
            _spriteRepository = spriteRepository;
            _combatService = combatService;
        }

        public Sprite GetProjectileSprite(string projectileData, int level)
        {
            string str = $"{projectileData}_{level}";
            var sp = _spriteRepository.GetSprite(str);
            return sp;
        }

        public IMoveStretagy GetMoveStretagy(EProjectileMoveType type)
        {
            if (_moveStretagy.TryGetValue(type, out var value))
            {
                if (value.Count > 0)
                    return value.Pop();
            }

            IMoveStretagy moveStretagy = default;

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
        public void SpawnProjectile(ProjectileDataSO data, ProjectileEventContext context)
        {
            ProjectileItem obj = _projectileItemPool.GetItem(context.Attacker.Position);

            Debug.Log($"Spawner {context.effects.Count}");

            var move = GetMoveStretagy(data.MoveType);
            move.Init(obj.transform, context.Target, data.Speed);

            var projectileSprite = GetProjectileSprite(data.SpriteName, context.Attacker.EvolutionLevel);

            ProjectileEffectExecuter effectExecuter = new ProjectileEffectExecuter(_combatService, data.ResolveData, context);

            obj.Init(context, effectExecuter, move, data, projectileSprite);
        }
    }
}
