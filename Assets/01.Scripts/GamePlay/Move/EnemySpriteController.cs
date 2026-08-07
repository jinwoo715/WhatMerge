using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpriteController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    public Sprite sprite;

    public List<Sprite> _moveSprite;

    private int _moveSpriteIndex;
    private float _moveTimer = 0;
    private float _moveSpriteInterval;

    public void Init(List<Sprite> sprites, float moveSpriteInterval)
    {
        if (_spriteRenderer == null)
            throw new InvalidOperationException($"{nameof(SpriteRenderer)} is not assigned.");

        _moveSprite = sprites;
        _moveSpriteInterval = moveSpriteInterval;
        _moveSpriteIndex = 0;
        _moveTimer = 0f;
        _front = true;
        _spriteRenderer.sprite = _moveSprite[_moveSpriteIndex];
    }

    public Vector3 GetAnimationTopPosition(float padding)
    {
        if (_spriteRenderer == null)
            throw new InvalidOperationException($"{nameof(SpriteRenderer)} is not assigned.");
        if (_moveSprite == null || _moveSprite.Count == 0)
            throw new InvalidOperationException("Enemy movement sprites are not initialized.");
        if (float.IsNaN(padding) || float.IsInfinity(padding) || padding < 0f)
            throw new ArgumentOutOfRangeException(nameof(padding), padding, "Padding must be finite and non-negative.");

        Bounds animationBounds = _moveSprite[0].bounds;
        for (int i = 1; i < _moveSprite.Count; i++)
            animationBounds.Encapsulate(_moveSprite[i].bounds);

        Vector3 localPosition = new Vector3(0f, animationBounds.center.y + padding, 0f);
        return _spriteRenderer.transform.TransformPoint(localPosition);
    }

    public void SetDirection(EMoveDirection moveDirection)
    {
        if (moveDirection == EMoveDirection.Up || moveDirection == EMoveDirection.Right)
            Flip(true);
        else
            Flip(false);
    }

    private void Flip(bool isRight)
    {
        _spriteRenderer.flipX = !isRight;
    }

    private void Update()
    {
        PlayMoveAnimation();
    }

    public bool _front = true;

    public void PlayMoveAnimation()
    {
        _moveTimer += Time.deltaTime;
        if(_moveTimer >= _moveSpriteInterval)
        {
            if (_front)
            {
                _moveSpriteIndex++;

                if (_moveSpriteIndex == 2)
                    _front = false;
            }
            else
            {
                _moveSpriteIndex--;

                if (_moveSpriteIndex == 0)
                    _front = true;
            }
            
            _spriteRenderer.sprite = _moveSprite[_moveSpriteIndex];
            _moveTimer = 0;
        }
    }
}
