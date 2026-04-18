using Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawUtility : MonoBehaviour
{
    public float _radius;
    public float _angle;
    public Vector3 _dir;

    public void Init(float radius, float angle)
    {
        _radius = radius;
        _angle = angle;

        Debug.Log($"Radius : {_radius}, Angle : {_angle}");
    }

    public void UpdateDir(Vector3 dir)
    {
        _dir = dir;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);

        Vector3 center = transform.position;

        float baseAngle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;

        float halfAngle = _angle * 0.5f;
        float startAngle = baseAngle - halfAngle;
        float endAngle = baseAngle + halfAngle;

        int segments = 30;
        float angleStep = (endAngle - startAngle) / segments;

        Vector3 prevPoint = center + DirFromAngle(startAngle) * _radius;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, prevPoint);

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            Vector3 currentPoint = center + DirFromAngle(currentAngle) * _radius;

            Gizmos.DrawLine(prevPoint, currentPoint);

            if (i == segments)
            {
                Gizmos.DrawLine(center, currentPoint);
            }

            prevPoint = currentPoint;
        }
    }

    private Vector3 DirFromAngle(float angleInDegrees)
    {
        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }
}
