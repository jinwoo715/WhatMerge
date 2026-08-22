[System.Serializable]
public class HeroData : BaseData
{
    public string Name;
    public string Description;

    public string SpriteKey;

    public int BaseATK;
    public float GrowthRatio;
    public float TierBonus;
    public float FirstEvolution;
    public float SecondEvolution;

    public float AttackSpeed;

    public int Penetration;

    public float CriticalChance;
    public float CriticalMultiplier;
}

