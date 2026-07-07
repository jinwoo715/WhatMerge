using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Map
{
    public class MapBoard : MonoBehaviour, IFieldTileService, IPathProvider
    {
        [SerializeField] private Tile _tilePrefab;
        [SerializeField] private int _xSize = 5;
        [SerializeField] private int _ySize = 7;

        private Tile[,] _grid;
        private GridWorld _gridWorld;

        private List<Tile> _enemyDestinations = new List<Tile>();

        private HashSet<ITileReadOnly> OccupiedTiles = new HashSet<ITileReadOnly>();
        private Tile _nextHeroEmptyTile = null;

        public void Init()
        {
            _grid = new Tile[_xSize, _ySize];
            _gridWorld = new GridWorld(_xSize, _ySize);

            for (int i = 0; i < _xSize; i++)
            {
                for (int j = 0; j < _ySize; j++)
                {
                    Tile tile = Instantiate(_tilePrefab, this.transform);
                    tile.Init(i, j);
                    tile.transform.position = _gridWorld.GridToWorldPosition(tile);

                    _grid[i, j] = tile;

                    if (i == 0 || i == _xSize-1 || j == 0 || j == _ySize-1)
                        tile.gameObject.layer = 0;
                }
            }

            _nextHeroEmptyTile = _grid[1, 1];

            _enemyDestinations.Add(_grid[0, 0]);
            _enemyDestinations.Add(_grid[0, _ySize - 1]);
            _enemyDestinations.Add(_grid[_xSize - 1, _ySize - 1]);
            _enemyDestinations.Add(_grid[_xSize-1, 0]);
        }

        public bool TryGetNextFieldTile(out Tile tile)
        {
            if (_nextHeroEmptyTile != null)
            {
                tile = _nextHeroEmptyTile;
                return true;
            }
            else
            {
                tile = null;
                return false;
            }
        }
        private void SetNextEmptyHeroTile()
        {
            for (int i = 1; i < _ySize - 1; i++)
            {
                for (int j = 1; j < _xSize - 1; j++)
                {
                    if (!_grid[j, i].Occupied)
                    {
                        _nextHeroEmptyTile = _grid[j, i];
                        return;
                    }
                }
            }

            _nextHeroEmptyTile = null;
        }
        public void OccupyFieldTile(ITileReadOnly tile)
        {
            OccupiedTiles.Add(tile);

            Tile occupyTile = GetTile(tile);
            occupyTile.OccupyTile();

            SetNextEmptyHeroTile();
        }
        public void FreeFieldTile(ITileReadOnly tile)
        {
            Tile freeTile = GetTile(tile);
            freeTile.UnOccupyTile();

            UpdateNextHeroTile(freeTile);
        }
        private void UpdateNextHeroTile(Tile returnTile)
        {
            if (_nextHeroEmptyTile == null)
            {
                _nextHeroEmptyTile = returnTile;
                return;
            }

            if (returnTile.Y > _nextHeroEmptyTile.Y)
                return;

            if (returnTile.X > _nextHeroEmptyTile.X)
                return;

            _nextHeroEmptyTile = returnTile;
        }

        private Tile GetTile(ITileReadOnly readOnlyTile)
        {
            return _grid[readOnlyTile.X, readOnlyTile.Y];
        }


        public Vector2 GetTileWorldPosition(ITileReadOnly tile)
        {
            return _gridWorld.GridToWorldPosition(tile);
        }

        public Vector3 GetDestination(int index)
        {
            Tile tile = _enemyDestinations[index];
            return _gridWorld.GridToWorldPosition(tile);
        }

        public int GetNextIndex(int currentIndex)
        {
            int nextIndex = currentIndex + 1;
            nextIndex = nextIndex % _enemyDestinations.Count;
            return nextIndex;
        }
    }
}
