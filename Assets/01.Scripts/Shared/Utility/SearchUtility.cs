using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SearchUtility
{
    public static List<Collider2D> ConeSearch(Vector3 pivot, Vector3 toDir, Collider2D[] enemyList, float detectionAngle)
    {
        List<Collider2D> detectedEnemy = new List<Collider2D>();
        foreach (var enemy in enemyList)
        {
            Vector3 directionToEnemy = (enemy.transform.position - pivot).normalized;

            float dotProduct = Vector3.Dot(toDir, directionToEnemy);
            float angleToEnemy = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

            if(angleToEnemy < detectionAngle)
            {
                Debug.Log($"Detect Enemy : {enemy.name}");
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
            Vector3 directionToEnemy = (enemy.transform.position - pivotPoint).normalized;

            float dotProduct = Vector3.Dot(toDir, directionToEnemy);
            float angleToEnemy = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

            if (angleToEnemy < detectionAngle)
            {
                Debug.Log($"Detect Enemy : {enemy.name}");
                detectedEnemy.Add(enemy);
            }
        }
        return detectedEnemy;
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

            if(distance < deltaDistance)
            {
                deltaDistance = distance;
                currentCollider = collider[i];
            }
        }

        return currentCollider.GetComponent<T>();
    }
    public static List<T> GetNearest2DTargets<T>(Vector3 position, float radius, LayerMask layer, int count) where T : MonoBehaviour
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
}
