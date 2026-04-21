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

    public static bool TryFindNearDamageable(Vector2 position, float radius, out IDamageable target)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, _results, LayerMask.GetMask(_enemyLayer));

        if(count == 0)
        {
            target = default;
            return false;
        }

        IDamageable nearestTarget = null;
        float minDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (_results[i].TryGetComponent<IDamageable>(out IDamageable damageable)) 
            {
                if (!damageable.IsActive) continue;

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
    public static bool TryFindNearEnemyTransform(Vector2 position, float radius, out Transform target)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, _results, LayerMask.GetMask(_enemyLayer));

        if (count == 0)
        {
            target = default;
            return false;
        }

        Transform nearestTarget = null;
        float minDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Transform tr = _results[i].transform;
            float distance = Vector2.SqrMagnitude((Vector2)_results[i].transform.position - position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTarget = tr;
            }
        }

        target = nearestTarget;
        return true;
    }

    public static List<IHeros> TryFindNearHeors(Vector2 position, float radius)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, _results, LayerMask.GetMask(_enemyLayer));

        List<IHeros> heros = new List<IHeros>();

        for (int i = 0; i < count; i++)
        {
            if (_results[i] is IHeros) continue;

            heros.Add(_results[i].GetComponent<IHeros>());
        }

        return heros;
    }

    public static List<IDamageable> FindNearEnemiesInConeArea(Vector2 position, float radius, Vector2 dir, float angle)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, radius, _results, LayerMask.GetMask(_enemyLayer));

        List<IDamageable> enemies = new List<IDamageable>();

        for (int i = 0; i < count; i++)
        {
            
            Vector2 targetPosition = _results[i].transform.position;
            Vector2 directionToEnemy = (targetPosition - position).normalized;

            float dot = Vector2.Dot(dir, directionToEnemy);
            float cos = Mathf.Cos(angle * Mathf.Deg2Rad);

            if (dot >= cos)
            {
                enemies.Add(_results[i].GetComponent<IDamageable>());
            }

            //float dotProduct = Vector3.Dot(dir, directionToEnemy);
            //dotProduct = Mathf.Clamp(dotProduct, -1f, 1f);

            //float angleToEnemy = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

            //if (angleToEnemy < angle)
            //{
            //    enemies.Add(_results[i].GetComponent<IDamageable>());
            //}
            //else
            //{
            //    Debug.Log($"Except : {_results[i].name}, {targetPosition}, {directionToEnemy}, {dotProduct}, {angleToEnemy}");
            //}
        }
        
        return enemies;
    }
}
