using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "GameConfig", menuName = "Config", order = 0)]
public class GameConfig : ScriptableObject
{
    public GameEconomyConfig GameEconomy;
    public PlayerInfoConfig PlayerConfig;
}

[System.Serializable]
public class GameEconomyConfig
{
    public int StartMoney;
    public int StartSpawnCost;
    public int IncreaseSpawnCost;
}

[System.Serializable]
public class PlayerInfoConfig
{
    public int SelectDeckIndex;

    public HeroDeck[] HeroDecks;

    public List<HeroSaveData> HaveHeros;
}

[System.Serializable]
public class HeroDeck
{
    public int[] Heros = new int[5];

    public void Init(int[] ary)
    {
        for (int i = 0; i < 5; i++)
        {
            Heros[i] = ary[i];
        }
    }

    public int RanHeroUID()
    {
        int index = Random.Range(0, 5);
        return Heros[index];
    }
}

[System.Serializable]
public class HeroSaveData
{
    public int HeroUID;
    public int Level = 1;
}
