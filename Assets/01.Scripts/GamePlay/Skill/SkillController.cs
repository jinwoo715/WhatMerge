using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public interface IManaModifier
    {
        void ChangeManaChargeSpeed(float ratio);
        void ImmediateManaCharge(float amount);
    }

    public class SkillController : MonoBehaviour, ISkillResourceModifier, IManaModifier
    {
        private int _currentHitCount;
        private float _currentMana;

        private float _manaChargeSpeed = 1;
        private float _attackDelay;

        private float _currentTime = 0;

        private bool _isUseingSkill = false;

        private List<IActiveSkill> _skill = new List<IActiveSkill>();

        public Action OnExcutedSkill;

        public void Clear()
        {
            StopAllCoroutines();
            _skill = null;
            _currentHitCount = 0;
            _currentMana = 0;
            _currentTime = 0;
            _isUseingSkill = false;
        }

        public void InjectSkill(List<IActiveSkill> skills)
        {
            _skill = skills;
        }
        public void SetAttackDelay(float attackSpeed)
        {
            float attackDelay = StatCalculator.AS(attackSpeed);
            _attackDelay = attackDelay;
        }

        private void Update()
        {
            _currentMana += Time.deltaTime * 10 * _manaChargeSpeed;

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

        private IEnumerator CoExcuteSkill(IActiveSkill skill, SkillTriggerContext skillContext)
        {
            _isUseingSkill = true;

            _currentHitCount++;
            skill.PayCost(this);

            yield return StartCoroutine(skill.Execute());
            
            OnExcutedSkill?.Invoke();

            _currentTime = 0;
            _isUseingSkill = false;
        }
        private bool TryGetUseableSkill(out IActiveSkill skill, SkillTriggerContext skillContext)
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
            _currentHitCount = 0;
        }
        public void ConsumeMana(float count)
        {
            _currentMana = 0;
        }

        public void ChangeManaChargeSpeed(float ratio)
        {
            _manaChargeSpeed += ratio;
        }

        public void ImmediateManaCharge(float amount)
        {
            _currentMana += amount;
        }

        public void AddHitCount(int count)
        {
            throw new NotImplementedException();
        }

        public void AddMana(float amount)
        {
            throw new NotImplementedException();
        }

        public void IncreaseManaAmoutRaio(float ratio)
        {
            throw new NotImplementedException();
        }
    }
}
