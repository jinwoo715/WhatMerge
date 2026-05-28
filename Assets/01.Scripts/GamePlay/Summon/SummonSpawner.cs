using Combat;
using Enemies;
using Skill;
using Skill.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonSpawner : MonoBehaviour, ISummonProvider
{
    [SerializeField] private SummonItem _item;

    private ObjectPool<SummonItem> _summonItemPool = new ObjectPool<SummonItem>();
    private Dictionary<EMove, Stack<ISummonMoveStrategy>> _summonMoveStrategies = new Dictionary<EMove, Stack<ISummonMoveStrategy>>();

    ISpriteRepository _spriteRepository;
    ICombatService _combatService;

    public void Init(ISpriteRepository spriteRepository, ICombatService combatService)
    {
        _spriteRepository = spriteRepository;
        _combatService = combatService;

        _summonItemPool.Init(this.transform, _item, 5);
    }

    public Vector3 GetSpawnPosition(Vector3 pivot, ESpawnPosition PositionType)
    {
        switch (PositionType)
        {
            case ESpawnPosition.TargetPivot:
                return pivot;
            case ESpawnPosition.TargetUpper:
                return pivot + Vector3.up * 0.5f;
            case ESpawnPosition.TargetLower:
                return pivot + Vector3.down * 0.5f;
            case ESpawnPosition.TargetRight:
                return pivot + Vector3.right * 0.5f;
            case ESpawnPosition.TargetLeft:
                return pivot + Vector3.left * 0.5f;
            case ESpawnPosition.ScreenCenter:
            default:
                return Vector3.zero;
        }
    }
    private ISummonMoveStrategy GetMoveStretagy(SummonMove move, Transform owner, ICreature target)
    {
        ISummonMoveStrategy moveStrategy = default;

        if (_summonMoveStrategies.TryGetValue(move.Move, out var value))
        {
            if (value.Count > 0)
                moveStrategy = value.Pop();
        }
        else
        {
            switch (move.Move)
            {
                case EMove.None:
                    moveStrategy = new NoneMoveStretagy();
                    break;
                case EMove.ToTarget:
                    moveStrategy = new ToTargetMoveStretagy();
                    break;
                case EMove.Attach:
                    moveStrategy = new AttachMoveStretagy();
                    break;
            }
        }

        moveStrategy.Init(owner, target, move.Speed);
        return moveStrategy;
    }
    public void SpawnSummon(SummonDataSO dataSO, ProjectileEventContext context)
    {
        Debug.Log($"{context}, {context.Target}");

        Vector3 spawnPosition = GetSpawnPosition(context.Target.Position, dataSO.SpawnPosition);
        var summon = _summonItemPool.GetItem(spawnPosition);

        ProjectileEffectExecuter projectileEffectExecuter = new ProjectileEffectExecuter(_combatService, dataSO.ResolveData, context);

        var sprite = _spriteRepository.GetSprite(dataSO.SpriteName);

        ISummonMoveStrategy move = GetMoveStretagy(dataSO.Move, summon.transform, context.Target);
        summon.Init(context,projectileEffectExecuter, move, dataSO, sprite);
    }
}

public interface ISummonMoveStrategy
{
    void Init(Transform owner, ICreature target, float speed);
    void Tick();
}
public interface ISummonExecuteTimer
{
    event Action OnExecute;
    void Init(SummonApplyTiming applyTiming);
    void Tick();
}
public class SummonExecuteTimer : ISummonExecuteTimer
{
    private SummonApplyTiming _applyTiming;
    private float _currentTime;
    private bool _isApplied;

    public event Action OnExecute;

    public void Init(SummonApplyTiming applyTiming)
    {
        _applyTiming = applyTiming;
        _isApplied = false;
    }

    public void Tick()
    {
        if (_isApplied && !_applyTiming.IsIntervalApply) 
            return;

        _currentTime += Time.deltaTime;

        if(_currentTime >= _applyTiming.Delay)
        {
            _currentTime -= _applyTiming.Delay;
            _isApplied = true;
            OnExecute?.Invoke();
        }
    }
}



