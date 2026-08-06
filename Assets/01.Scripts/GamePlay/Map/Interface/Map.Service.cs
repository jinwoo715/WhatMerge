using UnityEngine;

namespace WhatMerge.Map
{
    public interface IFieldTileService
    {
        int MaxRow { get; }
        int MaxCol { get; }
        bool TryGetNextFieldTile(out Tile tile);
        Vector2 GetTileWorldPosition(ITileReadOnly tile);
        void OccupyFieldTile(ITileReadOnly tile);
        void FreeFieldTile(ITileReadOnly tile);
    }

    public interface IPathProvider
    {
        public Vector3 GetDestination(int index);
        public int GetNextIndex(int currentIndex);
    }
}