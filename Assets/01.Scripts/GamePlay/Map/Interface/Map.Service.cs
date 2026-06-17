using UnityEngine;

namespace WhatMerge.Map
{
    public interface IHeroMapService
    {
        bool HasEmptyHeroTile { get; }
        bool TryGetNextHeroTile(out Tile tile);
        Vector2 GetTileWorldPosition(ITileReadOnly tile);
        void OccupyHeroTile(ITileReadOnly tile);
        void FreeHeroTile(ITileReadOnly tile);
    }

    public interface IPathProvider
    {
        public Vector3 GetDestination(int index);
        public int GetNextIndex(int currentIndex);
    }
}