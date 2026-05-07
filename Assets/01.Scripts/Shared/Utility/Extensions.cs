using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    
}
public static class Vector3Utility
{
    public static Vector3 MiddlePoint(Transform a, Transform b)
    {
        Vector3 middlePoint = MiddlePoint(a.position, b.position);

        return middlePoint;
    }
    public static Vector3 MiddlePoint(Vector3 a, Vector3 b)
    {
        return (a + b) * 0.5f;
    }

    public static Quaternion TowardEuler(Transform pivot, Transform target)
    {
        Quaternion targetRotation = TowardEuler(pivot.position, target.position);
        
        return targetRotation;
    }
    public static Quaternion TowardEuler(Vector3 pivot, Vector3 target)
    {
        Vector3 dir = (target - pivot).normalized;

        float angleRad = Mathf.Atan2(dir.y, dir.x);
        float angleDeg = angleRad * Mathf.Rad2Deg - 90f;

        Quaternion targetRotation = Quaternion.Euler(0, 0, angleDeg);

        return targetRotation;
    }
}
