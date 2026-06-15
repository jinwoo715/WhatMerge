using Enemies;
using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WhatMerge.Infrastructure;

public interface IVFXService
{
    public void ShowEffect(string name, Vector3 target, Vector3 attacker);
    public void ShowVFX(VFXData vfxData);
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

    public void ShowEffect(string name, Vector3 target, Vector3 attacker)
    {
        if (string.IsNullOrEmpty(name)) return;

        var effect = _effectPool.GetItem(target);
        Sprite sp = _spriteRepository.GetSprite(name);

        Vector3 dir = (target - attacker).normalized;
        effect.Init(sp, dir);
    }

    public void ShowVFX(VFXData vfxData)
    {
        throw new System.NotImplementedException();
    }
}
