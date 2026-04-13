using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace Enemies
{
    public class EnemySpriteRepository : ISpriteRepository
    {
        private Dictionary<string, List<Sprite>> _enemySpriteByName = new Dictionary<string, List<Sprite>>();

        public void Init(SpriteAtlas spriteAtlas)
        {
            Sprite[] sprites = new Sprite[spriteAtlas.spriteCount];

            spriteAtlas.GetSprites(sprites);

            SortSprite(sprites);
        }

        private void SortSprite(Sprite[] sprites)
        {
            foreach (var sprite in sprites)
            {
                string[] spriteNames = sprite.name.Split("_");

                if (!_enemySpriteByName.ContainsKey(spriteNames[0]))
                {
                    _enemySpriteByName.Add(spriteNames[0], new List<Sprite>());
                }

                _enemySpriteByName[spriteNames[0]].Add(sprite);
            }

            foreach (var sprite in _enemySpriteByName)
            {
                sprite.Value.Sort((a, b) => a.name.CompareTo(b.name));
            }
        }

        public List<Sprite> GetSprites(string name)
        {
            if (_enemySpriteByName.TryGetValue(name, out var value))
            {
                return value;
            }
            else
            {
                Debug.LogError($"Not Exist Key {name}");
                return new List<Sprite>();
            }
        }
    }
}
