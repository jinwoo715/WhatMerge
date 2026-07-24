using Skill;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Combat;
using WhatMerge.Heros;

public abstract class Execution : IExecute
{
    private SkillAnimationData _animaData;
    private ISpriteChanger _spriteChanger;

    protected readonly ExecutionData _executionData;
    protected readonly IExecute _executeDelivery;

    public Execution(SkillAnimationData animaData, ISpriteChanger spriteChanger, IExecute execute, ExecutionData executionData)
    {
        _animaData = animaData;
        _spriteChanger = spriteChanger;
        _executeDelivery = execute;
        _executionData = executionData;
    }
    public abstract IEnumerator Execute(IReadOnlyList<ICombatant> targets);
    public IEnumerator SetReadyMotion()
    {
        _spriteChanger.SetSprite(_animaData.MotionReadyName);

        yield return new WaitForSeconds(_animaData.ExecutionMotionTime);
    }
    public IEnumerator SetExecutionMotion()
    {
        _spriteChanger.SetSprite(_animaData.MotionName);

        Debug.Log(_animaData.MotionName);

        yield return new WaitForSeconds(_animaData.ReadyMotionTime);
    }
    public void SetIdleMotion()
    {
        _spriteChanger.SetIdle();
    }
}
