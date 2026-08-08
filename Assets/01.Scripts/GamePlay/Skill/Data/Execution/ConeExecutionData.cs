using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class ConeExecutionData : ExecutionData
    {
        [Range(0.1f, 360f)]
        [Tooltip("Full cone angle in degrees.")]
        public float Angle = 60f;
    }
}
