using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProjectileProvider
{
    public void SpawnProjectile(ProjectilePayload data);
}

public class ProjectilePayload
{
    public Vector3 SpawnPos;
    public ProjectileData Data;

}

public class ProjectileSpawner : MonoBehaviour, IProjectileProvider
{
    [SerializeField] private Projectile _prefab;

    private ObjectPool<Projectile> _projectilePool = new ObjectPool<Projectile>();
    private void Start()
    {
        _projectilePool.Init(this.transform, _prefab, 10);
    }

    public void SpawnProjectile(ProjectilePayload data)
    {
        Projectile obj = _projectilePool.GetItem(data.SpawnPos);

        IMoveStretagy moveStretagy;

        switch (data.Data.MoveType)
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
    }
}
