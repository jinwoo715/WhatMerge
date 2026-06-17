using System;
using UnityEngine;

namespace WhatMerge.Map
{ 
    public interface ITileObject
    {
        ITileReadOnly OccupiedTile { get; }
        void SetTile(ITileReadOnly tile, Vector2 position);
    }
}
