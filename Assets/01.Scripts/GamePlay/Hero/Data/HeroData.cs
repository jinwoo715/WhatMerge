namespace WhatMerge.Heros
{
    public enum HeroGrade
    {
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4
    }

    [System.Serializable]
    public class HeroData : BaseData
    {
        public string Name;
        public string Description;

        public HeroGrade BaseGrade;

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

}
