using Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Entity
{
    public interface ICreatureFinder
    {
        IDamageable GetNearestEnemy();
        List<IDamageable> GetNearEnemies();
        List<IDamageable> GetAllEnemies();
    }
}
