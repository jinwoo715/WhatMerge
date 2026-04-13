using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    public interface ISpriteRepository
    {
        List<Sprite> GetSprites(string name);
    }
}