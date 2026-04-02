using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class GridWorld
    {
        private float _xOffset;
        private float _yOffset;
        public GridWorld(int xSize, int ySize)
        {
            _xOffset = xSize / 2f - 0.5f;
            _yOffset = ySize / 2f - 0.5f;
        }

        public Vector2 GridToWorldPosition(IReadOnlyTile tile)
        {
            return new Vector2(tile.X - _xOffset, tile.Y - _yOffset);
        }
    }
}