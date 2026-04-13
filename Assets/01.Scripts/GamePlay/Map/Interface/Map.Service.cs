using UnityEngine;

namespace Map
{
    public interface IHeroMapService
    {
        bool HasEmptyHeroTile { get; }
        bool TryGetNextHeroTile(out Tile tile);
        Vector2 GetTileWorldPosition(IReadOnlyTile tile);
        void OccupyHeroTile(IReadOnlyTile tile);
        void FreeHeroTile(IReadOnlyTile tile);
    }

    public interface IPathProvider
    {
        public Vector3 GetDestination(int index);
        public int GetNextIndex(int currentIndex);
    }
}