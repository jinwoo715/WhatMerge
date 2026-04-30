using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//DTO
public class PlayerData
{
    public string PlayerUID;
    public int CurrentDeckIndex;
}

public interface IPlayerDataLoader
{
    PlayerData LoadPlayerData();
}

public class PlayerDataLoader : IPlayerDataLoader
{
    public PlayerData LoadPlayerData()
    {
        return null;
    }
}
