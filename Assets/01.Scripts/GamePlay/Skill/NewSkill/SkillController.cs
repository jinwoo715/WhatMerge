using Enemies;
using Entity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skills
{

    public interface ISkill
    {
        public bool IsUsable { get; }
        public bool Excute();
    }

    public class ActiveSkillData
    {
        public int UID;
        public float Range;
        public int Priority;
        public int TriggerUID;
        public ETargetType Target;
        public int ExcuteUID;
    }

    public interface ISearchTarget
    {
        bool HasTargetInRange(ETargetType targetType, Vector3 position, float range);
    }
    public interface ISkillApplyStretagy
    {
        bool Excute();
    }

    public class ActiveSkill : ISkill
    {
        public ActiveSkillData _data;
        public Hero hero;
        ISearchTarget _searchStretagy;
        ISkillApplyStretagy _skillApplyStretagy;

        public bool IsUsable => _searchStretagy.HasTargetInRange(_data.Target, hero.transform.position, _data.Range);

        public ActiveSkill(ActiveSkillData data, ISearchTarget searchStretagy, ISkillApplyStretagy skillApplyStretagy)
        {
            _data = data;
            _searchStretagy = searchStretagy;
            _skillApplyStretagy = skillApplyStretagy;
        }

        public bool Excute()
        {
            return _skillApplyStretagy.Excute();
        }
    }
    public class PassiveSkill
    {

    }

    #region ActiveSkill


    public class SkillFactory
    {
        PassiveSkillFactory passiveSkillFactory;
        ActiveSkillFactory activeSkillFactory;

        //public List<ISkill> GetSkills(List<string> datas, Hero hero)
        //{
        //    foreach (var data in datas)
        //    {
        //        string[] splitData = data.Split('-');

        //        if(splitData[0] == "A")
        //        {
                   
        //        }
        //        else if(splitData[0] == "P")
        //        {

        //        }
        //    }
        //}
    }
    public class PassiveSkillFactory
    {

    }

    public class FieldCreatureFinder : ISearchTarget
    {
        IFieldHeroService fieldHeroService;
        IFieldEnemyService fieldEnemyService;

        public bool HasTargetInRange(ETargetType targetType, Vector3 position, float range)
        {
            switch (targetType)
            {
                case ETargetType.Self:
                case ETargetType.NearHeros:
                case ETargetType.AllHeros:
                    return true;

                case ETargetType.NearestEnemy:
                case ETargetType.NearEnemies:
                    return CreatureFinder.HasNearEnemy(position, range);

                case ETargetType.AllEnemy:
                    return fieldEnemyService.GetActiveEnemyCount > 0;
            }

            return false;
        }

    }

    public class ActiveSkillFactory
    {
        public FieldCreatureFinder fieldCreatureFinder;
        public ISkillDataReader _skillDataReader;
        //public ISkill CreateSkill(int uid, Hero hero)
        //{
        //    ActiveSkillData data = _skillDataReader.GetActiveSkillData(uid);

        //    ActiveSkill activeSkill = new ActiveSkill(data, fieldCreatureFinder, );
        //}

        //private ISkillApplyStretagy GetSkillExcuteStretagy(int uid)
        //{
        //    _skillDataReader.GetActiveSkillData
        //}
    }

    #endregion

    public class HeroData
    {
        public int _baseAttackUID;
        public HeroUpgradeSkillData Skill;
    }

    public class HeroUpgradeSkillData
    {
        public string HeroUID;

        public string Lv1;
        public string Lv10;
        public string Lv20;
        public string Lv30;
        public string Lv40;
        public string Lv50;
        public string Lv60;
        public string Lv70;
        public string Lv80;
        public string Lv90;
        public string Lv100;
        public string Lv110;
        public string Lv120;
        public string Lv130;
        public string Lv140;
        public string Lv150;

        public List<string> GetSkills(int level)
        {
            List<string> result = new();

            AddIfUnlocked(result, level, 1, Lv1);
            AddIfUnlocked(result, level, 10, Lv10);
            AddIfUnlocked(result, level, 20, Lv20);
            AddIfUnlocked(result, level, 30, Lv30);
            AddIfUnlocked(result, level, 40, Lv40);
            AddIfUnlocked(result, level, 50, Lv50);
            AddIfUnlocked(result, level, 60, Lv60);
            AddIfUnlocked(result, level, 70, Lv70);
            AddIfUnlocked(result, level, 80, Lv80);
            AddIfUnlocked(result, level, 90, Lv90);
            AddIfUnlocked(result, level, 100, Lv100);
            AddIfUnlocked(result, level, 110, Lv110);
            AddIfUnlocked(result, level, 120, Lv120);
            AddIfUnlocked(result, level, 130, Lv130);
            AddIfUnlocked(result, level, 140, Lv140);
            AddIfUnlocked(result, level, 150, Lv150);

            return result;
        }

        private void AddIfUnlocked(List<string> result, int currentLevel, int unlockLevel, string upgradeUID)
        {
            if (currentLevel >= unlockLevel && !string.IsNullOrEmpty(upgradeUID))
            {
                result.Add(upgradeUID);
            }
        }

    }

    public interface IManaModifier
    {
        void AddChargeSpeed(float speed);
        void AddCurrentChargeValue(float value);
    }

    public class SkillController : IManaModifier
    {
        private int _currentHitCount;
        private float _currentMana;
        private float _manaChargeSpeed;

        public void Update()
        {
            ChargeMana();
        }

        private void CountUpHitCount()
        {
            _currentHitCount++;
        }

        private void ChargeMana()
        {
            _currentMana += Time.deltaTime * _manaChargeSpeed;
            _currentMana = Mathf.Min(_currentMana, 100);
        }

        public void AddChargeSpeed(float speed)
        {
            _manaChargeSpeed += speed;
        }

        public void AddCurrentChargeValue(float value)
        {
            _currentMana += value;
        }
    }


}