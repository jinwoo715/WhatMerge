using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//DTO
public class PlayerData
{
    public string PlayerUID;
    public int CurrentDeckIndex;
    public HeroDeck[] HeroDecks = new HeroDeck[5];
    public HeroDeck GetSelectHeroDeck()
    {
        return HeroDecks[CurrentDeckIndex];
    }
}

public interface IPlayerDataLoader
{
    PlayerData LoadPlayerData();
}

public class PlayerDataLoader : IPlayerDataLoader
{
    public PlayerData LoadPlayerData()
    {
        PlayerData data = new PlayerData();
        data.CurrentDeckIndex = 0;

        data.HeroDecks[0] = new HeroDeck();
        data.HeroDecks[0].Init(new int[]{1,3,8,9,10});

        return data;
    }
}
