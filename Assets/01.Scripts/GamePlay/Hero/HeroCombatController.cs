using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    public interface ISkillResourceModifier
    {
        void ConsumeHitCount(int count);
        void ConsumeManaCount(float count);
    }

    public class HeroCombatController : MonoBehaviour, ISkillResourceModifier
    {
        private int _currentHitCount;
        private float _currentMana;

        private float MaxMana = 5;

        private float _manaChargeSpeed = 1;
        private float _attackDelay;

        private float _currentTime = 0;

        private bool _isUseingSkill = false;

        private List<ISkill> _skill = new List<ISkill>();

        public Action OnExcutedSkill;

        public void InjectSkill(List<ISkill> skills)
        {
            _skill = skills;

            Debug.Log(_skill);

            for (int i = 0; i < _skill.Count; i++)
            {
                Debug.Log(_skill[i]);
            }

            _skill.Sort((a, b) => a.SkillSlot.CompareTo(b.SkillSlot));
        }
        public void SetAttackDelay(float attackSpeed)
        {
            float attackDelay = StatCalculator.AS(attackSpeed);
            _attackDelay = attackDelay;
        }

        private void Update()
        {
            _currentMana += Time.deltaTime * _manaChargeSpeed;

            _currentMana = Mathf.Min(_currentMana, MaxMana);

            if (_isUseingSkill) return;

            _currentTime += Time.deltaTime;

            if (_currentTime >= _attackDelay)
            {
                SkillTriggerContext skillContext = new SkillTriggerContext(_currentHitCount, _currentMana);

                if (TryGetUseableSkill(out var skill, skillContext))
                {
                    StartCoroutine(CoExcuteSkill(skill, skillContext));
                }
            }
        }

        private IEnumerator CoExcuteSkill(ISkill skill, SkillTriggerContext skillContext)
        {
            _isUseingSkill = true;

            _currentHitCount++;
            skill.PayCost(this);

            yield return StartCoroutine(skill.Excute());
            
            OnExcutedSkill?.Invoke();

            _currentTime = 0;
            _isUseingSkill = false;
        }
        private bool TryGetUseableSkill(out ISkill skill, SkillTriggerContext skillContext)
        {
            for (int i = _skill.Count-1; i >= 0 ; i--)
            {
                if (_skill[i].IsUseable(skillContext))
                {
                    skill = _skill[i];
                    return true;
                }
            }

            skill = null;
            return false;
        }
        public void ConsumeHitCount(int count)
        {
            _currentHitCount -= count;

            _currentHitCount = Mathf.Max(_currentHitCount, 0);
        }
        public void ConsumeManaCount(float count)
        {
            _currentMana -= count;
            _currentMana = Mathf.Max(_currentMana, 0);
        }
    }
}
