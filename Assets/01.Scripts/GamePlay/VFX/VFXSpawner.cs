using Enemies;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Infrastructure;

public interface IVFXService
{
    public void ShowVFX(VFXData vfxData, Vector3 target, Vector3 owner);
}

public class VFXSpawner : MonoBehaviour, IVFXService
{
    [SerializeField] private VFXItem _hitEffect;

    private ISpriteRepository _spriteRepository;
    private ObjectPool<VFXItem> _effectPool = new();

    public void Init(ISpriteRepository spriteRepository) 
    {
        _spriteRepository = spriteRepository;
        _effectPool.Init(this.transform, _hitEffect, 10);
    }

    public void ShowVFX(VFXData vfxData, Vector3 target, Vector3 owner)
    {
        Sprite sprite = _spriteRepository.GetSprite(name);
        Vector3 spawnPosition = SpawnPosition(vfxData.PositionType, target, owner);
        Vector3 dir = (target - owner).normalized;

        var effect = _effectPool.GetItem(spawnPosition);
        effect.Init(sprite, dir);
    }

    private Vector3 SpawnPosition(VFXSpawnPositionTpye positionType, Vector3 target, Vector3 owner)
    {
        switch (positionType)
        {
            case VFXSpawnPositionTpye.Owner:
                return owner;
            case VFXSpawnPositionTpye.Target:
                return target;
            case VFXSpawnPositionTpye.Middle:
                return (target + owner) * 0.5f;
            case VFXSpawnPositionTpye.ScreenCenter:
            default:
                return Vector3.zero;
        }
    }
}
