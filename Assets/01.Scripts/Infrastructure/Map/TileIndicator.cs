using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatMerge.Map 
{
    public interface ITileIndicator
    {
        void ShowTileMarker(Tile tile);
        void UpdateTileMarker(Tile tile);
        void HideTileMarker();
    }

    public class TileIndicator : MonoBehaviour, ITileIndicator
    {
        [SerializeField] private GameObject _startTileMarker;
        [SerializeField] private GameObject _currentTileMarker;
        [SerializeField] private GameObject _bridgeLine;

        private Tile _startTile;
        private Tile _currentTile;

        public void ShowTileMarker(Tile tile)
        {
            _startTile = tile;
            _currentTile = tile;

            ActiveOnMarks();

            SetMarkPosition(_startTile, _startTileMarker.transform);
            SetMarkPosition(_startTile, _currentTileMarker.transform);

            SetBridgeLineTransform();
        }

        public void HideTileMarker()
        {
            _startTile = null;
            _currentTile = null;

            ActiveOffMarks();
        }

        public void ActiveOnMarks()
        {
            _startTileMarker.SetActive(true);
            _currentTileMarker.SetActive(true);
            _bridgeLine.SetActive(true);
        }
        public void ActiveOffMarks()
        {
            _startTileMarker.SetActive(false);
            _currentTileMarker.SetActive(false);
            _bridgeLine.SetActive(false);
        }

        public void UpdateTileMarker(Tile tile)
        {
            _currentTile = tile;
            SetMarkPosition(_currentTile, _currentTileMarker.transform);

            SetBridgeLineTransform();
        }

        private void SetMarkPosition(Tile tile, Transform mark)
        {
            mark.transform.position = tile.transform.position;
        }

        private void SetBridgeLineTransform()
        {
            Vector3 startTileTransform = _startTile.transform.position;
            Vector3 currentTileTransform = _currentTile.transform.position;

            float distance = Vector3.Distance(startTileTransform, currentTileTransform);
            _bridgeLine.transform.position = Vector3Utility.MiddlePoint(startTileTransform, currentTileTransform);
            _bridgeLine.transform.rotation = Vector3Utility.TowardEuler(startTileTransform, currentTileTransform);
            _bridgeLine.transform.localScale = new Vector3(0.1f, distance, 0.1f);
        }
    }
}
