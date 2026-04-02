using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.U2D;

public interface ISpriteChanger
{
    void SetSprite(string spriteName);
    void SetIdle();
}

public class HeroSpriteController : MonoBehaviour, ISpriteChanger
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    private SpriteAtlas _spriteAtlas;
    private string _heroName;
    private StringBuilder _builder = new StringBuilder();

    string idle = "Idle";
    public void Init(SpriteAtlas spriteAtlas, string heroName, int level)
    {
        _spriteAtlas = spriteAtlas;
        SetDefaultSpriteKey(heroName, level);
    }
    public void SetDefaultSpriteKey(string heroName, int level)
    {
        _heroName = $"{heroName}_{level}_";
        SetSprite(idle);
    }

    public void SetIdle()
    {
        SetSprite(idle);
    }

    public void SetSprite(string spriteName)
    {
        _builder.Append(_heroName);
        _builder.Append(spriteName);

        _spriteRenderer.sprite = _spriteAtlas.GetSprite(_builder.ToString());

        _builder.Clear();
    }
}
