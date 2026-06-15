using UnityEngine;

namespace WhatMerge.Combat
{

    public interface ICombatant
    {
        bool IsActive { get; }
        Vector3 Position { get; }
    }
}
