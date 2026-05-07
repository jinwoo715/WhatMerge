using Entity;
using Map;
using System;
using UnityEngine;

public class TileClicker : MonoBehaviour
{
    public event Action<Tile> OnPointDownTile;
    public event Action<Tile> OnDragTile;
    public event Action<Tile> OnPointUpTile;

    private Tile _selectedTile;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if(TryGetTile(out var tile))
            {
                _selectedTile = tile;
                OnPointDownTile?.Invoke(tile);
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (_selectedTile == null) return;

            if (TryGetTile(out var tile))
            {
                OnDragTile?.Invoke(tile);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            TryGetTile(out Tile tile);
            OnPointUpTile?.Invoke(tile);

            _selectedTile = null;
        }
    }
    private bool TryGetTile(out Tile tile)
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        LayerMask layerMask = LayerMask.GetMask(Define.TileLayer);
        Collider2D hit = Physics2D.OverlapPoint(mousePos, layerMask);

        if (hit == null)
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

