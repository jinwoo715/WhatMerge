using System;
using System.Collections.Generic;

namespace WhatMerge.Heros
{
    [Serializable]
    public class MythicMergeData
    {
        public int ResultHeroUID;
        public List<MythicMergeMaterialData> Materials;
    }

    [Serializable]
    public class MythicMergeMaterialData
    {
        public int HeroUID;
        public int Count;
    }
}
