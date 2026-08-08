using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Enemies;

public static class SearchUtility
{
    public static List<Collider2D> ConeSearch(Vector3 pivot, Vector3 toDir, Collider2D[] enemyList, float detectionAngle)
    {
        List<Collider2D> detectedEnemy = new List<Collider2D>();
        foreach (var enemy in enemyList)
        {
            if (enemy != null && IsInsideCone(pivot, toDir, enemy.transform.position, detectionAngle))
            {
                detectedEnemy.Add(enemy);
            }
        }
        return detectedEnemy;
    }
    public static List<Collider2D> ConeSearch(Vector3 pivotPoint, Vector3 toDir, float range, LayerMask layerMask, float detectionAngle)
    {
        Collider2D[] searchTotalEnemy = Physics2D.OverlapCircleAll(pivotPoint, range, layerMask);
        List<Collider2D> detectedEnemy = new List<Collider2D>();
        foreach (var enemy in searchTotalEnemy)
        {
            if (enemy != null && IsInsideCone(pivotPoint, toDir, enemy.transform.position, detectionAngle))
            {
                detectedEnemy.Add(enemy);
            }
        }
        return detectedEnemy;
    }
    public static List<Enemy> GetConeEnemies(Vector3 pivotPoint, Vector3 toDir, float range,float detectionAngle)
    {
        Collider2D[] searchTotalEnemy = Physics2D.OverlapCircleAll(pivotPoint, range, LayerMask.GetMask("Enemy"));
        List<Enemy> detectedEnemy = new List<Enemy>();

        foreach (var enemy in searchTotalEnemy)
        {
            if (enemy != null && IsInsideCone(pivotPoint, toDir, enemy.transform.position, detectionAngle))
            {
                Enemy target = enemy.GetComponent<Enemy>();
                if (target != null)
                    detectedEnemy.Add(target);
            }
        }
        return detectedEnemy;
    }

    public static List<Enemy> GetNearEnemies(Vector3 pivot, float range)
    {
        Collider2D[] searchTotalEnemy = Physics2D.OverlapCircleAll(pivot, range, LayerMask.GetMask("Enemy"));
        List<Enemy> detectedEnemy = new List<Enemy>();

        foreach (var enemy in searchTotalEnemy)
        {
            detectedEnemy.Add(enemy.GetComponent<Enemy>());
        }
        return detectedEnemy;
    }

    public static Enemy GetNearestEnemy(Vector3 position, float radius)
    {
        return GetNearest2DTarget<Enemy>(position, radius, LayerMask.GetMask("Enemy"));
    }

    public static T GetNearest2DTarget<T>(Vector3 position, float radius, LayerMask layer) where T : MonoBehaviour
    {
        Collider2D[] collider = Physics2D.OverlapCircleAll(position, radius, layer);

        if (collider.Length == 0)
            return null;

        float deltaDistance = Mathf.Infinity;
        Collider2D currentCollider = collider[0];

        for (int i = 0; i < collider.Length; i++)
        {
            float distance = Vector3.SqrMagnitude(position - collider[i].transform.position);

            if(distance < deltaDistance && collider[i].gameObject.activeSelf)
            {
                deltaDistance = distance;
                currentCollider = collider[i];
            }
        }

        return currentCollider.GetComponent<T>();
    }

    public static List<Enemy> GetNearEnemiesByDistance(Vector3 position, float radius, int count, Enemy except = null)
    {
        return GetNear2DTargets<Enemy>(position, radius, LayerMask.GetMask("Enemy"), count, except);
    }

    public static List<T> GetNear2DTargets<T>(Vector3 position, float radius, LayerMask layer, int count, Enemy except = null) where T : MonoBehaviour
    {
        Collider2D[] collider = Physics2D.OverlapCircleAll(position, radius, layer);

        if (collider.Length == 0)
            return null;

        List<T> targets = new List<T>();

        for (int i = 0; i < collider.Length; i++)
        {
            T component = collider[i].GetComponent<T>();
            if (component != null)
                targets.Add(component);
        }

        if (except != null)
            targets.Remove(except as T);

        // 거리 기준으로 정렬
        targets.Sort((a, b) =>
        {
            float distA = Vector2.SqrMagnitude((Vector2)a.transform.position - (Vector2)position);
            float distB = Vector2.SqrMagnitude((Vector2)b.transform.position - (Vector2)position);
            return distA.CompareTo(distB);
        });

        // 가까운 것부터 count 개수만 리턴
        if (targets.Count > count)
        {
            targets = targets.GetRange(0, count);
        }

        return targets;
    }
    public static List<T> GetNearAll2DTargets<T>(Vector3 position, float radius, LayerMask layer) where T : MonoBehaviour
    {
        Collider2D[] collider = Physics2D.OverlapCircleAll(position, radius, layer);

        if (collider.Length == 0)
            return null;

        List<T> targets = new List<T>();

        for (int i = 0; i < collider.Length; i++)
        {
            T component = collider[i].GetComponent<T>();
            if (component != null)
                targets.Add(component);
        }

        return targets;
    }
    public static List<T> GetRandom2DTargets<T>(Vector3 position, float radius, LayerMask layer, int count) where T : MonoBehaviour
    {
        Collider2D[] collider = Physics2D.OverlapCircleAll(position, radius, layer);

        if (collider.Length == 0)
            return null;

        List<Collider2D> colliderTargets = collider.ToList();
        List<T> targets = new List<T>();

        while (colliderTargets.Count != 0 || targets.Count != count)
        {
            int ranNum = UnityEngine.Random.Range(0, colliderTargets.Count);
            targets.Add(colliderTargets[ranNum].GetComponent<T>());
            colliderTargets.Remove(colliderTargets[ranNum]);
        }

        return targets;
    }
    public static bool IsExistEnemyInRange(Vector3 position, float radius)
    {
        Collider2D[] collider = Physics2D.OverlapCircleAll(position, radius, LayerMask.GetMask("Enemy"));
        return collider.Length != 0;
    }
    public static T GetNearestTarget<T>(IReadOnlyList<ICombatant> list, Vector3 pivot) where T : class
    {
        float minSqrDistance = Mathf.Infinity;

        T nearestTarget = null;

        foreach (var item in list)
        {
            if (item is not T target)
            {
                break;
            }

            if(item.IsActive)
            {
                float distance = Vector3.SqrMagnitude(item.Position - pivot);

                if (distance < minSqrDistance)
                {
                    minSqrDistance = distance;
                    nearestTarget = target;
                }
            }
        }

        return nearestTarget;
    }
    public static List<T> GetConeTargets<T>(IReadOnlyList<ICombatant> list, Vector3 pivot, Vector3 dir, float angle) where T : class
    {
        List<T> results = new List<T>();

        if (list == null)
            return results;

        foreach (var target in list)
        {
            if (target == null || !target.IsActive)
                continue;

            if (target is T result && IsInsideCone(pivot, dir, target.Position, angle))
            {
                results.Add(result);
            }
        }

        return results;
    }

    private static bool IsInsideCone(
        Vector3 pivot,
        Vector3 direction,
        Vector3 targetPosition,
        float fullAngle)
    {
        if (float.IsNaN(fullAngle) || float.IsInfinity(fullAngle) || fullAngle <= 0f)
            return false;

        Vector2 toTarget = targetPosition - pivot;
        if (toTarget.sqrMagnitude <= 0.000001f)
            return true;

        Vector2 forward = direction;
        if (forward.sqrMagnitude <= 0.000001f)
            return false;

        float halfAngle = Mathf.Clamp(fullAngle * 0.5f, 0f, 180f);
        float minimumDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        float dot = Vector2.Dot(forward.normalized, toTarget.normalized);

        return dot + 0.00001f >= minimumDot;
    }
}
