using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GamePlay;
using UnityEngine.UI;

public class VFXItem : MonoBehaviour, IPooledItem<VFXItem>
{
    [SerializeField] private SpriteRenderer _render;

    public bool IsActive { get; private set; }
    public event Action<VFXItem> OnReturn;

    private float _timer = 0;

    public void Init(Sprite sprite, Vector3 dir)
    {
        _render.sprite = sprite;
        _timer = Define.HIT_EFFECT_TIME;

        RotationToTarget(dir);
    }

    private void RotationToTarget(Vector3 dir)
    {
        float angleRad = Mathf.Atan2(dir.y, dir.x);
        float angleDeg = angleRad * Mathf.Rad2Deg - 180f;

        Quaternion targetRotation = Quaternion.Euler(0, 0, angleDeg);

        this.transform.rotation = targetRotation;
    }

    private void Update()
    {
        if (!IsActive) return;

        _timer -= Time.deltaTime;

        if(_timer < 0)
        {
            OnReturn?.Invoke(this);
        }
    }

    public void OnDespawn()
    {
        IsActive = false;
    }

    public void OnSpawn()
    {
        IsActive = true;
    }
}
