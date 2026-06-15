namespace WhatMerge.Enemies
{
    public interface IEnemyStatModifier
    {
        void AddFixedValue(EnemyStatType type, float value);
        void AddMultiplier(EnemyStatType type, float value);
    }
}
