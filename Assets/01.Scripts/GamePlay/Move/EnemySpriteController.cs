using System.Collections.Generic;
using UnityEngine;

public class EnemySpriteController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public List<Sprite> _moveSprite;

    private int _moveSpriteIndex;
    private float _moveTimer = 0;
    private float _moveSpriteInterval;

    public void Init(List<Sprite> sprites, float moveSpriteInterval)
    {
        _spriteRenderer.sprite = sprites[0];
        _moveSprite = sprites;
        _moveSpriteInterval = moveSpriteInterval;
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
