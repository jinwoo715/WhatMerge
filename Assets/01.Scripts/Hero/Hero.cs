using WhatMerge.Map;
using Skill;
using System;
using UnityEngine;
using UnityEngine.U2D;
using WhatMerge.Combat;


namespace WhatMerge.Heros
{
    public interface IHeroInfoProvider
    {
        public int UID { get; }
        public string Name { get; }
        public int EvolutionLevel { get; }
    }

    public interface IAttackRangeProvider
    {
        float BasicAttackRange { get; }
    }

    public class Hero : MonoBehaviour, ITileObject, IHeroInfoProvider, IAttacker, IPooledItem<Hero>, IAttackRangeProvider
    {
        private SkillController _skillController;
        private HeroStats _stat = new HeroStats();
        private HeroData _heroData;
        private ATKData _atkData;

        private int _upgradeLevel = 1;
        private IHeroVisual _heroVisual;
        public bool IsActive { get; private set; }
        public string Name => _heroData.Name;
        public int EvolutionLevel { get; private set; }
        public Vector3 Position => this.transform.position;
        public int UID => _heroData.UID;
        public ITileReadOnly OccupiedTile { get; private set; }
        public IHeroStatModifier StatModify => _stat;
        public float BasicAttackRange => _skillController.BasicAttackRange;
        public int SpawnIndex { get; }

        public void SetData(HeroData data, ATKData atkData, IHeroVisual heroVisual, int upgradeLevel, int evolutionLevel)
        {
            _heroData = data;
            _atkData = atkData;
            _upgradeLevel = upgradeLevel;

            _stat.SetBaseValue(HeroStatType.AttackPerSecond, data.AS);
            _stat.SetBaseValue(HeroStatType.CriticalChance, data.CriticalChance);
            _stat.SetBaseValue(HeroStatType.CriticalMultiplier, data.CriticalMultiple);
            _stat.SetBaseValue(HeroStatType.FlatPenetration, data.Penetration);

            EvolutionLevel = evolutionLevel;
            _heroVisual = heroVisual;

            SetEvolution();
        }
        public void SetSkill(SkillController skillController)
        {
            _skillController = skillController;
        }
        public void UpgradeEvolution()
        {
            EvolutionLevel++;
            SetEvolution();
        }
        private void SetEvolution()
        {
            int baseATK = StatCalculator.BaseATK(EvolutionLevel, _atkData);
            float setAtk = StatCalculator.ATK(_upgradeLevel, baseATK, _atkData.GrowthRatio, _atkData.TierMultiplier);
            _stat.SetBaseValue(HeroStatType.Damage, setAtk);

            _heroVisual.SetEvolutionLevel(EvolutionLevel);
        }
        private void Update()
        {
            if (_skillController == null) return;

            _skillController.Tick(Time.deltaTime);
        }
        public void SetTile(ITileReadOnly tile, Vector2 position)
        {
            OccupiedTile = tile;
            this.transform.position = position;
        }
        public AttackPayload CreateAttackPayload()
        {
            int damage = Mathf.RoundToInt(_stat.GetStat(HeroStatType.Damage));
            int flatPenetration = Mathf.RoundToInt(_stat.GetStat(HeroStatType.FlatPenetration));
            int percentPenetration = Mathf.RoundToInt(_stat.GetStat(HeroStatType.PercentPenetration));
            float criticalChance = _stat.GetStat(HeroStatType.CriticalChance);
            float criticalMultiplier = _stat.GetStat(HeroStatType.CriticalMultiplier);
            AttackPayload payload = new AttackPayload(damage, flatPenetration, percentPenetration, criticalChance, criticalMultiplier);

            return payload;
        }
        public void OnSpawn() 
        {
            IsActive = true;
        }
        public void OnDespawn() 
        {
            IsActive = false;
            _skillController.StopRunner();
        }
    }
}
