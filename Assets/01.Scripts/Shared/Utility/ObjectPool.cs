using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPooledItem<T>
{
    public bool IsActive { get; }

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

    public int index = 0;
    public T CreateItem()
    {
        T item = GameObject.Instantiate(_origin, _parent);

        item.gameObject.SetActive(false);

        OnCreateEvent?.Invoke(item);

        item.gameObject.name = index.ToString();
        index++;
        return item;
    }
    public void ReturnItem(T item)
    {
        if (_itemPool.Contains(item)) return;

        Exception firstException = null;

        try
        {
            item.OnDespawn();
        }
        catch (Exception exception)
        {
            firstException = exception;
        }

        try
        {
            item.gameObject.SetActive(false);
        }
        catch (Exception exception)
        {
            firstException ??= exception;
        }

        _itemPool.Push(item);

        try
        {
            OnReturnEvent?.Invoke(item);
        }
        catch (Exception exception)
        {
            firstException ??= exception;
        }

        if (firstException != null)
            throw firstException;
    }
}
