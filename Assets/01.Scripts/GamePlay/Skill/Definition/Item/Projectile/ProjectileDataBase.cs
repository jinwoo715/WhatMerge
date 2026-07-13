using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public abstract class ProjectileDataBase : SpawnItemData
    {
        public string Sprite;
        public float Speed;
        public float LifeTime;
    }
}
