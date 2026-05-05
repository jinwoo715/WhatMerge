using Entity;
using Map;
using System;
using UnityEngine;

public interface ITileSelecter
{
    event Action<Tile> OnPointDownTile;
    event Action<Tile> OnDragTile;
    event Action<Tile> OnPointUpTile;
}

public class TileClicker : MonoBehaviour, ITileSelecter
{
    [SerializeField] private GameObject _clickedTileMarker;
    [SerializeField] private GameObject _currentTileMarker;
    [SerializeField] private GameObject _markLine;

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
                
                TurnOnMarker(_clickedTileMarker);
                UpdateMarkerPosition(_clickedTileMarker, tile.transform.position);
                
                TurnOnMarker(_currentTileMarker);
                UpdateMarkerPosition(_currentTileMarker, tile.transform.position);

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

                UpdateMarkerPosition(_currentTileMarker, tile.transform.position);

                _markLine.transform.position = GetTileMiddlePoint();


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

            TurnOffMarker(_clickedTileMarker);
            TurnOffMarker(_currentTileMarker);

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

    private void UpdateMarkerPosition(GameObject marker, Vector3 position)
    {
        marker.transform.position = position;
    }
    private void TurnOnMarker(GameObject marker)
    {
        marker.SetActive(true);
    }
    private void TurnOffMarker(GameObject marker)
    {
        marker.SetActive(false);
    }

    private Vector3 GetTileMiddlePoint()
    {
        if (!_selectedTile || !_currentOnTile) return Vector3.zero;

        Vector3 middle = (_selectedTile.transform.position + _currentOnTile.transform.position) * 0.5f;
        return middle;
    }

}

