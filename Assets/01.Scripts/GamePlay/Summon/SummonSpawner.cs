using Enemies;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonSpawner : MonoBehaviour, ISummonProvider
{
    [SerializeField] private Summon _origin;

    private ObjectPool<Summon> _summonPool = new ObjectPool<Summon>();

    ISpriteRepository _spriteRepository;
    IDataProvider _dataProvider;

    public void Init(ISpriteRepository spriteRepository, IDataProvider dataProvider)
    {
        _spriteRepository = spriteRepository;
        _dataProvider = dataProvider;
        _summonPool.Init(this.transform, _origin, 10);
    }

    public Vector3 GetSpawnPosition(Vector3 pivot, ESommonPosType PositionType)
    {
        switch (PositionType)
        {
            case ESommonPosType.Center:
                return pivot;
            case ESommonPosType.Left:
                return pivot + Vector3.left * 0.5f;
            case ESommonPosType.Right:
                return pivot + Vector3.right * 0.5f;
            case ESommonPosType.Upper:
                return pivot + Vector3.up * 0.5f;
            case ESommonPosType.Bottom:
                return pivot + Vector3.down * 0.5f;
        }

        return Vector3.zero;
    }
    public void SpawnProjectile(ProjectilePayload data)
    {
        var summonData = _dataProvider.GetSummonData(data.UID);

        Vector3 pos = GetSpawnPosition(data.SpawnPos, summonData.PivotPosType);

        Debug.Log($"{data.SpawnPos}, {pos}");

        var item = _summonPool.GetItem(pos);

        var sprite = _spriteRepository.GetSprite(summonData.Sprite);

        var move = GetMoveStrategy(summonData.MoveType);

        var effect = GetSummonEffect(summonData.HitType);

        var hit = GetHitEffect(summonData.SummonAttackTarget);

        item.Init(data, summonData, sprite, move, effect, hit);
    }

    public IHitEffect GetHitEffect(ESummonAttackTarget attackTarget)
    {
        switch (attackTarget)
        {
            case ESummonAttackTarget.Single:
                return new SingleAttackEffect();
            case ESummonAttackTarget.Multi:
                return new MultiAttackEffect();
            default:
                return default;
        }
    }

    public ISummonEffect GetSummonEffect(ESummonExcuteType summonEffectType)
    {
        ISummonEffect summonEffect = default;

        switch (summonEffectType)
        {
            case ESummonExcuteType.Once:
                summonEffect = new SummonOnceEffect();
                break;
            case ESummonExcuteType.Interval:
                summonEffect = new SummonIntervalEffect();
                break;
        }

        return summonEffect;
    }
    public ISummonMove GetMoveStrategy(ESummonMoveType moveType)
    {
        ISummonMove moveStrategy = default;
        switch (moveType)
        {
            case ESummonMoveType.Fix:
                moveStrategy = new FixSummon();
                break;
            case ESummonMoveType.Follow:
                moveStrategy = new FollowSummon();
                break;
            case ESummonMoveType.Approach:
                moveStrategy = new ApprochSummon();
                break;
        }

        return moveStrategy;
    }
}
