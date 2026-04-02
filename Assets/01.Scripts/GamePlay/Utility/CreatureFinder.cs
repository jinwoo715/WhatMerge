using Combat;
using Enemies;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CreatureFinder 
{
    private static readonly Collider2D[] _results = new Collider2D[20];
    private static string _enemyLayer = "Enemy";
    private static string _heroLayer = "Hero";

    public static bool TryFindNearEnemy(Vector2 position, float radius, out IDamageable target)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, _results, LayerMask.GetMask(_enemyLayer));

        if(count == 0)
        {
            target = null;
            return false;
        }

        IDamageable nearestTarget = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (_results[i].TryGetComponent<IDamageable>(out IDamageable damageable)) 
            {
                float distance = Vector2.SqrMagnitude((Vector2)_results[i].transform.position - position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestTarget = damageable;
                }
            }
        }

        target = nearestTarget;
        return true;
    }
    static List<Enemy> TryNearFindAllEnemies(Vector2 position, float radius)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, _results, LayerMask.GetMask(_enemyLayer));

        List<Enemy> enemyList = new List<Enemy>();
        //for (int i = 0; i < enemies.Length; i++)
        //{
        //    enemyList.Add(enemies[i].GetComponent<Enemy>());
        //}

        return enemyList;
    }
    
}
