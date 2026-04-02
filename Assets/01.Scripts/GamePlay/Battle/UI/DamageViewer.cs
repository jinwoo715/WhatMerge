using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPooledItem<T>
{
    event Action<T> OnReturn;
    void OnSpawn();
    void OnDespawn();
}

public class ObjectPool<T> where T : MonoBehaviour, IPooledItem<T>
{
    private Transform _parent;
    private T _origin;
    private Stack<T> _itemPool = new Stack<T>();

    public event Action<T> OnCreateEvent;
    public event Action<T> OnReturnEvent;

    public void Init(Transform parent, T origin, int initCount)
    {
        _parent = parent;
        _origin = origin;

        for (int i = 0; i < initCount; i++)
        {
            _itemPool.Push(CreateItem());
        }
    }
    public T GetItem(Vector3 position)
    {
        T item = _itemPool.Count > 0 ? _itemPool.Pop() : CreateItem();
        item.transform.position = position;
        item.gameObject.SetActive(true);
        item.OnSpawn();

        return item;
    }
    public T CreateItem()
    {
        T item = GameObject.Instantiate(_origin, _parent);
        item.gameObject.SetActive(false);
        item.OnReturn += ReturnItem;
        OnCreateEvent?.Invoke(item);

        return item;
    }
    public void ReturnItem(T item)
    {
        if (_itemPool.Contains(item)) return;

        item.OnDespawn();
        item.gameObject.SetActive(false);
        _itemPool.Push(item);
        OnReturnEvent?.Invoke(item);
    }
}


public class DamageViewer : MonoBehaviour
{
    [SerializeField] private DamageValueTextItem _damageItem;

    private ObjectPool<DamageValueTextItem> _damageItemPool = new ObjectPool<DamageValueTextItem>();
    private int _initPoolCount = 10;
    private float _showTimer = 0.5f;
    private float _moveVelocity = 1.5f;

    internal void Init()
    {
        _damageItemPool.OnCreateEvent += (value) => value.Init(_showTimer, _moveVelocity);
        _damageItemPool.Init(this.transform, _damageItem, _initPoolCount);
    }

    public void ShowDamageText(Vector3 position, int value)
    {
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(position);
        DamageValueTextItem item = _damageItemPool.GetItem(screenPosition);
        item.SetData(value);
    }
}
