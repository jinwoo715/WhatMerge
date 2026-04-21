using Combat;
using Enemies;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProjectileProvider
{
    public void SpawnProjectile(ProjectilePayload data);
}

public class ProjectilePayload
{
    public IAttackable Attacker;
    public ICreature Target;
    public Vector3 SpawnPos;
    public int UID;
    public int HeroLevel;
} 

public interface IDataProvider
{
    ProjectileData GetProjecTileData(int uid);
    SummonObjectData GetSummonData(int uid);
}

public class MoveStretagyFactory
{
    private Dictionary<EProjectileMoveType, Stack<IMoveStretagy>> _moveStretagy = new Dictionary<EProjectileMoveType, Stack<IMoveStretagy>>();

}

public class ProjectileSpawner : MonoBehaviour, IProjectileProvider
{
    [SerializeField] private Projectile _prefab;

    private IDataProvider _dataProvider;
    private ISpriteRepository _spriteRepository;
    private ObjectPool<Projectile> _projectilePool = new ObjectPool<Projectile>();

    private Dictionary<EProjectileMoveType, Stack<IMoveStretagy>> _moveStretagy = new Dictionary<EProjectileMoveType, Stack<IMoveStretagy>>();

    private void Start()
    {
        _projectilePool.Init(this.transform, _prefab, 10);
    }

    public void Init(IDataProvider dataProvider, ISpriteRepository spriteRepository)
    {
        _dataProvider = dataProvider;
        _spriteRepository = spriteRepository;
    }

    public void SpawnProjectile(ProjectilePayload data)
    {
        Debug.Log("Spawn");

        Projectile obj = _projectilePool.GetItem(data.SpawnPos);
        ProjectileData projectileData = _dataProvider.GetProjecTileData(data.UID);

        var move = GetMoveStretagy(projectileData.MoveType);

        var sp = GetProjectileSprite(projectileData, data.HeroLevel);

        obj.Init(GetMoveStretagy(projectileData.MoveType), null, projectileData, sp, data.Target);

        //obj.Init(move, );
    }

    public Sprite GetProjectileSprite(ProjectileData projectileData, int level)
    {
        if (projectileData.LevelSwap)
        {
            string str = $"{projectileData.SpriteName}_{level}";
            var sp = _spriteRepository.GetSprite(str);
            return sp;
        }
        else
        {
            var sp = _spriteRepository.GetSprite(projectileData.SpriteName);
            return sp;
        }
    }

    public IMoveStretagy GetMoveStretagy(EProjectileMoveType type)
    {
        if(_moveStretagy.TryGetValue(type, out var value))
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
}
