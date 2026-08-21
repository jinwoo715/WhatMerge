using Enemies;
using Skill.Data;
using System;
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

        _effectPool.OnCreateEvent += (item) => item.OnReturn += ReturnVFXItem;
        _effectPool.Init(this.transform, _hitEffect, 10);
    }

    private void ReturnVFXItem(VFXItem item)
    {
        _effectPool.ReturnItem(item);
    }

    public void ShowVFX(VFXData vfxData, Vector3 target, Vector3 owner)
    {
        Sprite sprite = _spriteRepository.GetSprite(vfxData.VFXName);
        Vector3 spawnPosition = SpawnPosition(vfxData.PositionType, target, owner);
        Vector3 dir = vfxData.IsApplyDir ? (target - owner).normalized : Vector3.zero;

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
                return GetScreenCenter(target.z);
            default:
                return Vector3.zero;
        }
    }

    private static Vector3 GetScreenCenter(float worldZ)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            throw new InvalidOperationException(
                $"{nameof(VFXSpawnPositionTpye.ScreenCenter)} requires a camera tagged MainCamera.");
        }

        float distanceFromCamera = Mathf.Abs(worldZ - mainCamera.transform.position.z);
        Vector3 worldPosition = mainCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, distanceFromCamera));
        worldPosition.z = worldZ;
        return worldPosition;
    }
}
