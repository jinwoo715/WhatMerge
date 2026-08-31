using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.U2D;

public interface ISpriteChanger
{
    void SetSprite(string spriteName);
    void SetIdle();
    void Init(SpriteAtlas spriteAtlas, string heroName);
    void LookAt(Vector3 targetPosition);
}

public interface IHeroVisual
{
    void SetEvolutionLevel(int level);
}

public class HeroSpriteController : MonoBehaviour, ISpriteChanger, IHeroVisual
{
    private const float FacingEpsilon = 0.0001f;

    [SerializeField] private SpriteRenderer _spriteRenderer;
    private SpriteAtlas _spriteAtlas;
    private string _heroName;
    private string _spriteKey;
    private StringBuilder _builder = new StringBuilder();

    string idle = "Idle";

    public void Init(SpriteAtlas spriteAtlas, string heroName)
    {
        _spriteAtlas = spriteAtlas;
        _heroName = heroName;
    }

    public void SetDefaultSpriteKey(int level)
    {
        _spriteKey = $"{_heroName}_{level+1}_";
        SetSprite(idle);
    }

    public void SetIdle()
    {
        SetSprite(idle);
    }

    public void SetSprite(string spriteName)
    {
        _builder.Append(_spriteKey);
        _builder.Append(spriteName);

        _spriteRenderer.sprite = _spriteAtlas.GetSprite(_builder.ToString());

        _builder.Clear();
    }

    public void LookAt(Vector3 targetPosition)
    {
        if (_spriteRenderer == null)
            throw new System.InvalidOperationException($"{nameof(SpriteRenderer)} is not assigned.");

        float deltaX = targetPosition.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= FacingEpsilon)
            return;

        bool targetIsRight = deltaX > 0f;
        bool transformMirrorsSprite = transform.lossyScale.x < 0f;
        bool shouldMirrorSource = !targetIsRight;

        _spriteRenderer.flipX = shouldMirrorSource ^ transformMirrorsSprite;
    }

    public void SetEvolutionLevel(int level)
    {
        SetDefaultSpriteKey(level);
    }
}
