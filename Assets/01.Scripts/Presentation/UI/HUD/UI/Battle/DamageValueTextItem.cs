using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageValueTextItem : MonoBehaviour, IPooledItem<DamageValueTextItem>
{
    [SerializeField] private TMP_Text _text;

    public event Action<DamageValueTextItem> OnReturn;

    private float _moveVelocity;
    private float _returnTime;
    private float _currentTime;

    public bool IsActive => throw new NotImplementedException();

    public void Init(float returnTime, float moveVelocity)
    {
        _currentTime = 0;
        _returnTime = returnTime;
        _moveVelocity = moveVelocity;
    }

    public void SetData(int damage)
    {
        _text.text = damage.ToString();
    }

    private void Update()
    {
        _currentTime += Time.deltaTime;

        if(_currentTime >= _returnTime)
        {
            OnReturn?.Invoke(this);
            return;
        }

        Move();
    }

    private void Move()
    {
        this.transform.position += Vector3.up * _moveVelocity;
    }

    public void OnSpawn() { }

    public void OnDespawn()
    {
        _currentTime = 0;
    }
}
