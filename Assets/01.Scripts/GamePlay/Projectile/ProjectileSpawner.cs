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
        public void SpawnProjectile(ProjectilePayload data);
        public void SpawnProjectile(ProjectileDataSO data, ProjectileEventContext context);
    }

    public interface ISummonProvider
    {
        public void SpawnProjectile(ProjectilePayload data);
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
        [SerializeField] private Projectile _prefab;
        [SerializeField] private ProjectileItem _itemPrefab;

        private IDataProvider _dataProvider;
        private ISpriteRepository _spriteRepository;
        private ObjectPool<Projectile> _projectilePool = new ObjectPool<Projectile>();
        private ObjectPool<ProjectileItem> _projectileItemPool = new ObjectPool<ProjectileItem>();

        private Dictionary<EProjectileMoveType, Stack<IMoveStretagy>> _moveStretagy = new Dictionary<EProjectileMoveType, Stack<IMoveStretagy>>();

        private ICombatService _combatService;
        private ISummonProvider _summonProvider;

        private void Start()
        {
            _projectilePool.Init(this.transform, _prefab, 10);
            _projectileItemPool.Init(this.transform, _itemPrefab, 10);
        }

        public void Init(IDataProvider dataProvider, ISpriteRepository spriteRepository)
        {
            _dataProvider = dataProvider;
            _spriteRepository = spriteRepository;
        }

        public void SpawnProjectile(ProjectilePayload data)
        {
            Projectile obj = _projectilePool.GetItem(data.SpawnPos);
            ProjectileData projectileData = _dataProvider.GetProjecTileData(data.UID);

            var move = GetMoveStretagy(projectileData.MoveType);

            var sp = GetProjectileSprite(projectileData.SpriteName, data.HeroLevel);

            var destroyer = new ProjectileDestoryer();
            destroyer.Init(projectileData.DestoryType, 0);

            var resolver = GetProjectileEffectResolver(projectileData.TargetType);
            resolver.Init(projectileData.DestoryType);

            obj.Init(data, move, destroyer, resolver, projectileData, sp, data.Target);
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
        public IProjectileEffectResolver GetProjectileEffectResolver(EProjectileAttackType type)
        {
            switch (type)
            {
                case EProjectileAttackType.Single:
                    return new SingleDamageResolver();
                case EProjectileAttackType.Multiple:
                    return new AreaDamageResolver();
                default:
                    return default;
            }
        }

        //TODO
        public void SpawnProjectile(ProjectileDataSO data, ProjectileEventContext context)
        {
            ProjectileItem obj = _projectileItemPool.GetItem(context.Attacker.Position);

            var move = GetMoveStretagy(data.MoveType);
            move.Init(obj.transform, context.Target, data.Speed);

            var projectileSprite = GetProjectileSprite(data.SpriteName, context.Attacker.EvolutionLevel);

            ProjectileEffectExecuter effectExecuter = new ProjectileEffectExecuter(_combatService, _summonProvider, context.effects);

            obj.Init(context, effectExecuter, move, data, projectileSprite);
        }
    }
}
