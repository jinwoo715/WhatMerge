using Combat;
using Heros;
using Heros.Stat;
using Map;
using Skill;
using Stat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public interface IHeroInfoProvider
{
    public Transform Transform { get; }
    public string Name { get; }
    public int EvolutionLevel { get; }
    public int UID { get; }
}

public interface IAttackStatProvider
{
    float GetStat(EAttackStatType attackStatType);
}

public interface IStatModifier
{
    void ModifyStat(EHeroStatType stat, float value);
}

public enum EAttackStatType
{
    Damage,
    FlatPentration,
    PercentPenetration,
    Radius,
    AttackSpeed
}


public interface IHeros : ICreature, IStatModifier
{
    IHeroInfoProvider Provider { get; }
}

namespace Entity
{
    public class Creature : MonoBehaviour
    {
        public bool IsActive { get; set; }
    }

    public class Hero : Creature, ITileObject, IHeroInfoProvider, IAttackStatProvider, IAttackable, IHeros, IPooledItem<Hero>
    {
        [SerializeField] private HeroSpriteController _spriteController;

        //TODO
        public Skill.Data.SkillController skillController;

        private HeroData _heroData;
        private ATKData _atkData;

        private HeroStatController _stat = new HeroStatController();

        private IReadOnlyTile _underTile;

        public event Action<IReadOnlyTile> OnOccupiedTile;
        public event Action<IReadOnlyTile> OnFreeTile;
        public event Action<Hero> OnReturn;

        public SkillServiceLocate Context { get; private set; }

        private int _heroLevel = 1;
        private int _evolutionLevel = 0;
        public Transform Transform => this.transform;
        public string Name => _heroData.Name;
        public int EvolutionLevel => _evolutionLevel;

        public ISpriteChanger SpriteChanger => _spriteController;

        public Vector3 Position => this.transform.position;

        public IHeroInfoProvider Provider => throw new NotImplementedException();

        public IHeroStatReadOnly StatReadOnly => _stat;

        public int UID => _heroData.UID;

        public IReadOnlyTile OccupiedTile => _underTile;

        public void SpawnInit()
        {
            Context = new SkillServiceLocate();
            Context.Register<ICreature>(this);
            Context.Register<IAttackable>(this);
            Context.Register<Transform>(this.transform);
            Context.Register<IHeroInfoProvider>(this);
            Context.Register<ISpriteChanger>(SpriteChanger);
            Context.Register<IAttackStatProvider>(this);
        }
        public void SetData(HeroData data, ATKData atkData, SpriteAtlas spriteAtlas, int level)
        {
            _heroData = data;
            _atkData = atkData;
            _heroLevel = level;

            _stat.SetBaseValue(EHeroStat.AttackSpeed, data.AS);

            _spriteController.Init(spriteAtlas, _heroData.Name, _evolutionLevel);
        }

        public void SetEvolution(int evolutionLevel)
        {
            _evolutionLevel = evolutionLevel;

            int baseATK = StatCalculator.BaseATK(_evolutionLevel, _atkData);
            float setAtk = StatCalculator.ATK(_heroLevel, baseATK, _atkData.GrowthRatio, _atkData.TierMultiplier);
            _stat.SetBaseValue(EHeroStat.Damage, setAtk);

            _spriteController.SetLevel(_evolutionLevel);
        }

        public void EvolutionUp()
        {
            _evolutionLevel++;
            SetEvolution(_evolutionLevel);
        }

        private void Update()
        {
            skillController.Tick(Time.deltaTime);
        }

        public void SetPassive(List<ISkill> skills)
        {
            foreach (var skill in skills)
            {
                //StartCoroutine(skill.Execute());
            }
        }

        public void SetTile(IReadOnlyTile tile, Vector2 position)
        {
            //if (IsExistUnderTile())
            //    OnFreeTile?.Invoke(_underTile);

            //OnOccupiedTile?.Invoke(tile);
            _underTile = tile;

            this.transform.position = position;
        }
        private bool IsExistUnderTile()
        {
            return _underTile != null;
        }

        public float GetStat(EAttackStatType attackStatType)
        {
            switch (attackStatType)
            {
                case EAttackStatType.Damage:
                    return _stat.GetStat(EHeroStat.Damage);
                case EAttackStatType.FlatPentration:
                    return 0;
                case EAttackStatType.PercentPenetration:
                    return 0;
                case EAttackStatType.Radius:
                    break;
                case EAttackStatType.AttackSpeed:
                    break;
                default:
                    break;
            }
            return 1;
        }

        public void RequestDamage(DamageContext dc)
        {
            throw new NotImplementedException();
        }

        public DamageContext CreateDamageContext()
        {
            throw new NotImplementedException();
        }

        public void ModifyStat(EHeroStatType stat, float value)
        {
            Debug.Log($"Apply Stat : {stat}, {value}");
        }

        public void OnSpawn()
        {
            
        }

        public void OnDespawn()
        {
           
        }

        public void Return()
        {
            OnReturn?.Invoke(this);
        }
    }
}
