using WhatMerge.Map;

namespace WhatMerge.Heros
{
    public interface IFieldHeroSelecter
    {
        void PointDownTile(Tile tile);
        void PointUpTile(Tile tile);
        void DragTile(Tile tile);
    }
}
