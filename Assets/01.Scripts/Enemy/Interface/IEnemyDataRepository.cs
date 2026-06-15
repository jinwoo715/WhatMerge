namespace WhatMerge.Enemies
{
    public interface IEnemyDataRepository
    {
        EnemyData GetData(int uid);
    }
}