using UnityEngine;

namespace WhatMerge.Map
{
    public interface ITileReadOnly
    {
        int X { get; }
        int Y { get; }
        bool Occupied { get; }
    }
    public interface IModifyTile
    {
        public void OccupyTile();
        public void UnOccupyTile();
    }

    public class Tile : MonoBehaviour, ITileReadOnly, IModifyTile
    {
        private int _x;
        private int _y;
        private bool _occupied;

        public int X => _x;
        public int Y => _y;
        public bool Occupied => _occupied;

        public void Init(int x, int y)
        {
            _x = x;
            _y = y;
            _occupied = false;
        }
        public void OccupyTile()
        {
            _occupied = true;
        }
        public void UnOccupyTile()
        {
            _occupied = false;
        }
    }
}
