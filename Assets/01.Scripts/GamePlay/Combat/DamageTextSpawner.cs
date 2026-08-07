using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTextSpawner : MonoBehaviour
{
    [SerializeField] private DamageValueTextItem _damageItem;
    [SerializeField] private Transform _itemParent;

    private ObjectPool<DamageValueTextItem> _damageItemPool = new ObjectPool<DamageValueTextItem>();
    private int _initPoolCount = 10;
    private float _showTimer = 0.5f;
    private float _moveVelocity = 1.5f;

    internal void Init()
    {
        _damageItemPool.OnCreateEvent += (value) => value.Init(_showTimer, _moveVelocity);
        _damageItemPool.OnCreateEvent += (value) =>  value.OnReturn += ReturnNumViewer;
        _damageItemPool.Init(_itemParent, _damageItem, _initPoolCount);
    }

    public void ShowDamageText(Vector3 position, int value)
    {
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(position);
        DamageValueTextItem item = _damageItemPool.GetItem(screenPosition);
        item.SetData(value);
    }
    private void ReturnNumViewer(DamageValueTextItem item)
    {
        _damageItemPool.ReturnItem(item);
    }
}
