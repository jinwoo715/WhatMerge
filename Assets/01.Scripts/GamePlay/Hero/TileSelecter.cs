using Entity;
using Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITileSelecter
{
    event Action<Tile> OnPointDownTile;
    event Action<Tile> OnDragTile;
    event Action<Tile> OnPointUpTile;
}

public class TileSelecter : MonoBehaviour, ITileSelecter
{
    public event Action<Tile> OnPointDownTile;
    public event Action<Tile> OnDragTile;
    public event Action<Tile> OnPointUpTile;

    private Tile _selectedTile = null;
    private Tile _currentOnTile = null;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if(TryGetTile(out var tile))
            {
                _selectedTile = tile;
                OnPointDownTile?.Invoke(_selectedTile);
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (_selectedTile == null) return;

            if (TryGetTile(out var tile))
            {
                if (tile == _currentOnTile) return;

                _currentOnTile = tile;
                OnDragTile?.Invoke(_currentOnTile);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_selectedTile == null) return;

            if(TryGetTile(out Tile tile))
            {
                OnPointUpTile?.Invoke(tile);
            }
            else
            {
                OnPointUpTile?.Invoke(_selectedTile);
            }

            _selectedTile = null;
            _currentOnTile = null;
        }
    }

    private bool TryGetTile(out Tile tile)
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        LayerMask layerMask = LayerMask.GetMask("Tile");
        Collider2D hit = Physics2D.OverlapPoint(mousePos, layerMask);
        if(hit == null)
        {
            tile = null;
            return false;
        }
        else
        {
            tile = hit.GetComponent<Tile>();
            return true;
        }
    }
}
