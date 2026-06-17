using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public interface ISkillRunner
    {
        void Tick(float tickValue);
        void StopRunner();
    }

    public class SkillController : ISkillResourceModifier, ISkillRunner
    {
        private List<IActiveSkill> _activeSkills;
        private List<IPassiveSkill> _passiveSkills;
        private MonoBehaviour _coroutineRunner;
        private Coroutine _currentSkill;

        private float _executionTime;
        private float _time;
        private float _mana;
        private int _hitCount;

        private bool _isUsingSkill = false;

        private float _manaChargeMultiple = 1;

        public float BasicAttackRange => _activeSkills.Count > 0 ? _activeSkills[0].Search.Range : 0f;

        public SkillController(List<IActiveSkill> activeSkills, List<IPassiveSkill> passiveSkills, MonoBehaviour coroutineRunner, float delay)
        {
            _activeSkills = activeSkills;
            _passiveSkills = passiveSkills;
            _coroutineRunner = coroutineRunner;
            _executionTime = delay;
            
            ApplyPassive();
        }

        public void ApplyPassive()
        {
            foreach (var passive in _passiveSkills)
            {
                passive.Apply();
            }
        }

        public void UpdateDelayTime(float delay)
        {
            _executionTime = delay;
        }

        public void Tick(float tickValue)
        {
            if (_isUsingSkill == true) return;

            _time += tickValue;
            ChargeMana(tickValue);

            if (_time >= _executionTime)
            {
                IActiveSkill executeSkill = GetUsableSkill();

                if (executeSkill != null)
                {
                    _currentSkill = _coroutineRunner.StartCoroutine(CoExecuteSkill(executeSkill));
                }

                _time = 0;
            }
        }
        private IActiveSkill GetUsableSkill()
        {
            IActiveSkill usableSkill = null;

            SkillTriggerContext context = new SkillTriggerContext(_hitCount, _mana);

            int skillCount = _activeSkills.Count;
            for (int i = skillCount - 1; i >= 0; i--)
            {
                IActiveSkill skill = _activeSkills[i];

                if (skill.IsUsable(context))
                {
                    usableSkill = skill;
                    break;
                }
            }

            return usableSkill;
        }

        private IEnumerator CoExecuteSkill(IActiveSkill skill)
        {
            _isUsingSkill = true;

            skill.Trigger.UseTriggerResource(this);
            yield return skill.Execute();

            _isUsingSkill = false;

            _currentSkill = null;
        }

        private void ChargeMana(float manaAmount)
        {
            _mana += manaAmount * 10 * _manaChargeMultiple;
        }

        public void ConsumeHitCount(int count)
        {
            Debug.Log("Concume HitCount");
            _hitCount = 0;
            //_hitCount -= count;
        }

        public void ConsumeMana(float amount)
        {
            Debug.Log("Concume Mana");
            _mana = 0;
            //_mana -= amount;
        }

        public void AddHitCount(int count)
        {
            _hitCount += count;
        }

        public void AddMana(float amount)
        {
            _mana += amount;
        }

        public void IncreaseManaAmoutRaio(float ratio)
        {
            _manaChargeMultiple += ratio;
        }

        public void StopRunner()
        {
            if (_currentSkill != null)
                _coroutineRunner.StopCoroutine(_currentSkill);

            _currentSkill = null;
        }


    }
}
