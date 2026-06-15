using Enemies;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Enemies;

public interface IEventExcuter
{
    public void ExcuteOnEnemyDeath(Enemy enemy);
}

public interface IEventRegister
{
    public void SubscribeOnEnemyDeath(Action<Enemy> action);
    public void UnSubscribeOnEnemyDeath(Action<Enemy> action);
}   

public class GameEventBus : IEventExcuter, IEventRegister
{
    public event Action<Enemy> OnEnemyDeath;
    
    public void ExcuteOnEnemyDeath(Enemy enemy) => OnEnemyDeath?.Invoke(enemy);
    public void SubscribeOnEnemyDeath(Action<Enemy> action) => OnEnemyDeath += action;
    public void UnSubscribeOnEnemyDeath(Action<Enemy> action) => OnEnemyDeath -= action;
}
