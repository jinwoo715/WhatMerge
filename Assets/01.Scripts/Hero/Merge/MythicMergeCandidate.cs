namespace WhatMerge.Heros
{
    public readonly struct MythicMergeCandidate
    {
        public int ResultHeroUID { get; }
        public int EvolutionLevel { get; }

        public MythicMergeCandidate(int resultHeroUID, int evolutionLevel)
        {
            ResultHeroUID = resultHeroUID;
            EvolutionLevel = evolutionLevel;
        }
    }
}
