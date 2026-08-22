using Enemies;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using WhatMerge.Infrastructure;

public class VFXSpriteRepository : ISpriteRepository
{
    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
    public void Init(SpriteAtlas spriteAtlas)
    {
        Sprite[] sprites = new Sprite[spriteAtlas.spriteCount];
        spriteAtlas.GetSprites(sprites);

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sp = sprites[i];
            string key = sp.name.Replace("(Clone)", "");
            _sprites.Add(key, sp);
        }
    }

    public Sprite GetSprite(string key)
    {
        if (_sprites.TryGetValue(key, out var sp))
            return sp;

        return null;
    }

    public List<Sprite> GetSprites(string name)
    {
        return null;
    }


}
