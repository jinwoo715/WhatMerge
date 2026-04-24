using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{ 
    public interface ITileObject
    {
        IReadOnlyTile OccupiedTile { get; }
        event Action<IReadOnlyTile> OnOccupiedTile;
        event Action<IReadOnlyTile> OnFreeTile;
        void SetTile(IReadOnlyTile tile, Vector2 position);
    }
}
