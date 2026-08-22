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

    public class Hero : MonoBehaviour, ITileObject, IHeroInfoProvider, IAttacker, IPooledItem<Hero>, IAttackRangeProvider, IManaReceiver
    {
        private CombatantElement _element = new CombatantElement();
        private SkillController _skillController;
        private HeroStats _stat = new HeroStats();
        private HeroData _heroData;

        private int _upgradeLevel = 1;
        private IHeroVisual _heroVisual;

        public event Action<ICombatant> OnActiveOff;

        public bool IsActive { get; private set; }
        public string Name => _heroData.Name;
        public string SpriteName => _heroData.SpriteKey;
        public int EvolutionLevel { get; private set; } = 0;
        public Vector3 Position => this.transform.position;
        public int UID => _heroData.UID;
        public ITileReadOnly OccupiedTile { get; private set; }
        public IHeroStatModifier StatModify => _stat;
        public float BasicAttackRange => _skillController?.BasicAttackRange ?? 0f;
        public int SpawnIndex { get; private set; }

        public IElement Element => _element;

        public void SetData(HeroData data, IHeroVisual heroVisual, int upgradeLevel, int evolutionLevel, int spawnIndex)
        {
            _stat.Reset();

            SpawnIndex = spawnIndex;
            _heroData = data;
            _upgradeLevel = upgradeLevel;

            _stat.SetBaseValue(HeroStatType.AttackPerSecond, data.AttackSpeed);
            _stat.SetBaseValue(HeroStatType.CriticalChance, data.CriticalChance);
            _stat.SetBaseValue(HeroStatType.CriticalMultiplier, data.CriticalMultiplier);
            _stat.SetBaseValue(HeroStatType.FlatPenetration, data.Penetration);

            EvolutionLevel = evolutionLevel;
            _heroVisual = heroVisual;

            SetEvolution();
        }

        private void UpdateAttackSpeed(HeroStatType statType, float speed)
        {
            if(statType == HeroStatType.AttackPerSecond)
            {
                _skillController.UpdateDelayTime(StatCalculator.AS(speed));
            }
        }

        public void SetSkill(SkillController skillController)
        {
            if (skillController == null)
                throw new ArgumentNullException(nameof(skillController));

            if (_skillController != null)
                throw new InvalidOperationException($"Hero '{name}' already has a skill controller.");

            _skillController = skillController;
            _stat.OnStatChanged += UpdateAttackSpeed;
            UpdateAttackSpeed(
                HeroStatType.AttackPerSecond,
                _stat.GetStat(HeroStatType.AttackPerSecond));
        }
        public void UpgradeEvolution()
        {
            if (EvolutionLevel >= 2)
                throw new InvalidOperationException($"InValide Evolution Level {EvolutionLevel}");

            EvolutionLevel++;
            SetEvolution();
        }
        private void SetEvolution()
        {
            int baseATK = StatCalculator.BaseATK(EvolutionLevel, _heroData);
            float setAtk = StatCalculator.ATK(_upgradeLevel, baseATK, _heroData.GrowthRatio, _heroData.TierBonus);
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
            int criticalChance = Mathf.RoundToInt(_stat.GetStat(HeroStatType.CriticalChance));
            float criticalMultiplier = _stat.GetStat(HeroStatType.CriticalMultiplier);
            AttackPayload payload = new AttackPayload(damage, flatPenetration, percentPenetration, criticalChance, criticalMultiplier);

            return payload;
        }
        public void RestoreMana(float amount)
        {
            if (!IsActive || _skillController == null)
                return;

            _skillController.AddMana(amount);
        }
        public void OnSpawn() 
        {
            if (IsActive)
                return;

            IsActive = true;
        }
        public void OnDespawn() 
        {
            Deactivate();
        }

        private bool Deactivate()
        {
            if (!IsActive)
                return false;

            IsActive = false;
            _stat.OnStatChanged -= UpdateAttackSpeed;
            _skillController?.StopRunner();
            _skillController = null;
            _element.Clear();
            OnActiveOff?.Invoke(this);

            ClearRuntimeState();
            return true;
        }

        private void ClearRuntimeState()
        {
            _heroData = null;
            _heroVisual = null;
            OccupiedTile = null;
            _upgradeLevel = 1;
            EvolutionLevel = 0;
            SpawnIndex = 0;
        }
    }
}
