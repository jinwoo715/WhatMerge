using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Infrastructure
{
    public interface ISpriteRepository
    {
        List<Sprite> GetSprites(string name);
        Sprite GetSprite(string key);
    }
}