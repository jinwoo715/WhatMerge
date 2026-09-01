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
        [SerializeField] private HeroSpriteController _spriteController;
        private CombatantElement _element = new CombatantElement();
        private SkillController _skillController;
        private HeroStats _stat = new HeroStats();
        private HeroData _heroData;

        private int _upgradeLevel = 1;

        public event Action<ICombatant> OnActiveOff;

        public bool IsActive { get; private set; }
        public string Name => _heroData.Name;
        public string SpriteName => _heroData.SpriteKey;
        public int EvolutionLevel { get; private set; } = 0;
        public Vector3 Position => this.transform.position;
        public int UID => _heroData.UID;
        public int Level => _upgradeLevel;
        public ITileReadOnly OccupiedTile { get; private set; }
        public IHeroStatModifier StatModify => _stat;
        public float BasicAttackRange => _skillController?.BasicAttackRange ?? 0f;
        public int SpawnIndex { get; private set; }
        public ISpriteChanger SpriteChanger => _spriteController;
        public IElement Element => _element;

        public HeroGrade CurrentGrade
        {
            get
            {
                if (_heroData == null)
                    throw new InvalidOperationException("Inactive hero has no current grade.");

                int gradeValue = (int)_heroData.BaseGrade + EvolutionLevel;
                if (gradeValue < (int)HeroGrade.D || gradeValue > (int)HeroGrade.S)
                {
                    throw new InvalidOperationException(
                        $"Hero UID {_heroData.UID} has invalid grade value {gradeValue}.");
                }

                return (HeroGrade)gradeValue;
            }
        }

        public void SetData(HeroData data, int upgradeLevel, int evolutionLevel, int spawnIndex)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (upgradeLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(upgradeLevel));
            if (evolutionLevel < 0 || evolutionLevel > 2)
                throw new ArgumentOutOfRangeException(nameof(evolutionLevel));
            if ((int)data.BaseGrade + evolutionLevel > (int)HeroGrade.S)
            {
                throw new InvalidOperationException(
                    $"Hero UID {data.UID} grade exceeds S at evolution {evolutionLevel}.");
            }

            _stat.Reset();

            SpawnIndex = spawnIndex;
            _heroData = data;
            _upgradeLevel = upgradeLevel;

            _stat.SetBaseValue(HeroStatType.AttackPerSecond, data.AttackSpeed);
            _stat.SetBaseValue(HeroStatType.CriticalChance, data.CriticalChance);
            _stat.SetBaseValue(HeroStatType.CriticalMultiplier, data.CriticalMultiplier);
            _stat.SetBaseValue(HeroStatType.FlatPenetration, data.Penetration);

            EvolutionLevel = evolutionLevel;

            SetEvolution();
        }

        private void UpdateAttackSpeed(HeroStatType statType, float speed)
        {
            if(statType == HeroStatType.AttackPerSecond)
            {
                _skillController.UpdateDelayTime(StatCalculator.AS(speed));
            }
        }

        public void AttachSkillController(SkillController skillController)
        {
            if (skillController == null)
                throw new ArgumentNullException(nameof(skillController));

            if (_skillController != null)
                throw new InvalidOperationException($"Hero '{name}' already has a skill controller.");

            try
            {
                _skillController = skillController;
                _stat.OnStatChanged += UpdateAttackSpeed;
                UpdateAttackSpeed(
                    HeroStatType.AttackPerSecond,
                    _stat.GetStat(HeroStatType.AttackPerSecond));
                skillController.Activate();
            }
            catch
            {
                _stat.OnStatChanged -= UpdateAttackSpeed;
                _skillController = null;
                TryDisposeController(skillController);
                throw;
            }
        }

        public void UpgradeEvolution(SkillController nextController)
        {
            if (nextController == null)
                throw new ArgumentNullException(nameof(nextController));
            if (_skillController == null)
                throw new InvalidOperationException($"Hero '{name}' has no current skill controller.");
            if (EvolutionLevel >= 2)
                throw new InvalidOperationException($"Invalid evolution level {EvolutionLevel}.");

            HeroGrade expectedGrade = (HeroGrade)((int)CurrentGrade + 1);
            SkillController previousController = _skillController;

            _stat.OnStatChanged -= UpdateAttackSpeed;
            _skillController = null;

            try
            {
                previousController.Dispose();
                EvolutionLevel++;
                SetEvolution();

                _skillController = nextController;
                _stat.OnStatChanged += UpdateAttackSpeed;
                UpdateAttackSpeed(
                    HeroStatType.AttackPerSecond,
                    _stat.GetStat(HeroStatType.AttackPerSecond));
                nextController.Activate();

                if (CurrentGrade != expectedGrade)
                {
                    throw new InvalidOperationException(
                        $"Hero UID {UID} evolved to {CurrentGrade}, expected {expectedGrade}.");
                }
            }
            catch
            {
                _stat.OnStatChanged -= UpdateAttackSpeed;
                _skillController = null;
                TryDisposeController(nextController);
                throw;
            }
        }
        private void SetEvolution()
        {
            int baseATK = StatCalculator.BaseATK(EvolutionLevel, _heroData);
            float setAtk = StatCalculator.ATK(_upgradeLevel, baseATK, _heroData.GrowthRatio, _heroData.TierBonus);
            _stat.SetBaseValue(HeroStatType.Damage, setAtk);

            _spriteController.SetEvolutionLevel(EvolutionLevel);
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

        public void DisposeSkillController()
        {
            _stat.OnStatChanged -= UpdateAttackSpeed;
            SkillController controller = _skillController;
            _skillController = null;
            controller?.Dispose();
        }

        private bool Deactivate()
        {
            if (!IsActive && _skillController == null)
                return false;

            IsActive = false;
            Exception firstException = null;

            try
            {
                DisposeSkillController();
            }
            catch (Exception exception)
            {
                firstException = exception;
            }

            try
            {
                _element.Clear();
                OnActiveOff?.Invoke(this);
            }
            catch (Exception exception)
            {
                firstException ??= exception;
            }

            ClearRuntimeState();

            if (firstException != null)
                throw firstException;

            return true;
        }

        private void ClearRuntimeState()
        {
            _heroData = null;
            OccupiedTile = null;
            _upgradeLevel = 1;
            EvolutionLevel = 0;
            SpawnIndex = 0;
        }

        private void OnDisable()
        {
            if (_skillController == null)
                return;

            try
            {
                DisposeSkillController();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void TryDisposeController(SkillController controller)
        {
            try
            {
                controller?.Dispose();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }
        }
    }
}
