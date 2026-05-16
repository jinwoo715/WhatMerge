using Enemies;
using System.Collections;

namespace Skill
{
    public class DebuffPassive : IPassiveSkill
    {
        DeBuffData _data;
        IFieldEnemyService _fieldEnemyService;
        private float _addValue;

        public float DebuffValue => _data.Value + _addValue;
        public int UID => _data.UID;

        public DebuffPassive(IFieldEnemyService fieldEnemyService, DeBuffData data)
        {
            _fieldEnemyService = fieldEnemyService;
            _fieldEnemyService.OnSpawnEnemy += ApplyBuff;
            _fieldEnemyService.OnEnemyDeath += RevertBuff;
            _data = data;
        }

        public void ModifyParam(int paramIndex, float value)
        {
            _addValue += value;
        }

        public void Apply()
        {
            var allHeros = _fieldEnemyService.GetAllFieldEnemy;
            foreach (var hero in allHeros)
            {
                ApplyBuff(hero);
            }
        }

        public void Remove()
        {
            var allHeros = _fieldEnemyService.GetAllFieldEnemy;
            foreach (var hero in allHeros)
            {
                RevertBuff(hero);
            }

            _fieldEnemyService.OnSpawnEnemy -= ApplyBuff;
            _fieldEnemyService.OnEnemyDeath -= RevertBuff;
        }

        public void ApplyBuff(Enemy target)
        {
            target.ModifyStat(_data.StatType, -DebuffValue);
        }
        public void RevertBuff(Enemy target)
        {
            target.ModifyStat(_data.StatType, DebuffValue);
        }
    }
}