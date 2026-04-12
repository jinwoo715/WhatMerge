using Combat;
using Heros.Stat;
using Map;
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
    public int Level { get; }
}
public interface IHeroStatProvider
{
    public int Damage { get; }
    public int FlatPenetration { get; }
    public float PercentPenetration { get; }
    public float Radius { get; }
}

public interface IAttackStatProvider
{
    float GetStat(EAttackStatType attackStatType);
}
public enum EAttackStatType
{
    Damage,
    FlatPentration,
    PercentPenetration,
    Radius,
    AttackSpeed
}

namespace Entity
{
    public class Hero : MonoBehaviour, ITileObject, IHeroInfoProvider, IAttackStatProvider
    {
        [SerializeField] private HeroCombatController _heroCombat;
        [SerializeField] private HeroSpriteController _spriteController;

        private HeroData _heroData;
        private ATKData _atkData;

        private HeroStatController _stat = new HeroStatController();

        private IReadOnlyTile _underTile;

        public event Action<IReadOnlyTile> OnOccupiedTile;
        public event Action<IReadOnlyTile> OnFreeTile;

        private int _level = 1;
        public Transform Transform => this.transform;
        public string Name => _heroData.Name;
        public int Level => _level;

        public ISpriteChanger SpriteChanger => _spriteController;

        public void SpawnInit()
        {
            _stat.OnStatChange += (type, value) => { if(type == EHeroStat.AttackSpeed) _heroCombat.SetAttackDelay(value); };
        }
        public void SetData(HeroData data, ATKData atkData, SpriteAtlas spriteAtlas)
        {
            _heroData = data;
            _atkData = atkData;

            float setAtk = StatCalculator.ATK(_level, atkData.BaseATK, atkData.GrowthRatio, atkData.TierMultiplier);
            _stat.SetBaseValue(EHeroStat.Damage, setAtk);

            _stat.SetBaseValue(EHeroStat.AttackSpeed, data.AS);

            _spriteController.Init(spriteAtlas, _heroData.Name, _level);

            _heroCombat.OnExcutedSkill += _spriteController.SetIdle;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _level++;
                _spriteController.SetDefaultSpriteKey(_heroData.Name, _level);
            }
        }

        public void SetSkill(List<ISkill> skills)
        {
            _heroCombat.InjectSkill(skills);
        }
        public void SetTile(IReadOnlyTile tile, Vector2 position)
        {
            if (IsExistUnderTile())
                OnFreeTile?.Invoke(_underTile);

            OnOccupiedTile?.Invoke(tile);
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

        public float radius;
        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
