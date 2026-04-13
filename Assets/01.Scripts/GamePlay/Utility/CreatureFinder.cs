using Combat;
using Enemies;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CreatureFinder 
{
    private static readonly Collider2D[] _results = new Collider2D[100];
    private static string _enemyLayer = "Enemy";
    private static string _heroLayer = "Hero";

    public static bool TryFindNearEnemy(Vector2 position, float radius, out IDamageable target, out Vector2 targetPosition)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, _results, LayerMask.GetMask(_enemyLayer));

        if(count == 0)
        {
            target = default;
            targetPosition = Vector3.zero;
            return false;
        }

        IDamageable nearestTarget = null;
        float minDistance = float.MaxValue;
        Vector2 nearPosition = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            if (_results[i].TryGetComponent<IDamageable>(out IDamageable damageable)) 
            {
                float distance = Vector2.SqrMagnitude((Vector2)_results[i].transform.position - position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestTarget = damageable;
                    nearPosition = (Vector2)_results[i].transform.position;
                }
            }
        }

        target = nearestTarget;
        targetPosition = nearPosition;
        return true;
    }

    public static bool TryFindNearConeEnemies(Vector2 position, float radius, Vector2 dir, float angle, out List<IDamageable> enemies)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, _results, LayerMask.GetMask(_enemyLayer));

        if (count == 0)
        {
            enemies = default;
            return false;
        }

        enemies = new List<IDamageable>();

        for (int i = 0; i < count; i++)
        {
            Vector2 targetPosition = _results[i].transform.position;
            Vector2 directionToEnemy = (targetPosition - position).normalized;

            float dotProduct = Vector3.Dot(dir, directionToEnemy);
            float angleToEnemy = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

            if (angleToEnemy < angle)
            {
                enemies.Add(_results[i].GetComponent<IDamageable>());
            }
        }
        
        return true;
    }
}
