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

        private float _attackInterval;
        private float _elapsedTime;
        private float _lastAttackStartTime;
        private float _nextAttackTime;
        private float _mana;
        private int _hitCount;

        private bool _isUsingSkill = false;

        private float _manaChargeMultiple = 1;

        public float BasicAttackRange => _activeSkills.Count > 0 ? _activeSkills[0].Target.Range : 0f;
        public float CurrentMana => _mana;
        public float MaxMana { get; }
        public float AttackInterval => _attackInterval;
        public float NextAttackTime => _nextAttackTime;

        public SkillController(List<IActiveSkill> activeSkills, List<IPassiveSkill> passiveSkills, MonoBehaviour coroutineRunner, float delay)
        {
            _activeSkills = activeSkills;
            _passiveSkills = passiveSkills;
            _coroutineRunner = coroutineRunner;
            _attackInterval = ValidateAttackInterval(delay);
            _nextAttackTime = _attackInterval;
            MaxMana = CalculateMaxMana(_activeSkills);

            ApplyPassive();
        }

        public void ApplyPassive()
        {
            foreach (var passive in _passiveSkills)
            {
                passive.Apply();
            }
        }
        private void ReleasePassive()
        {
            foreach (var passive in _passiveSkills)
            {
                passive.Release();
            }
        }

        public void UpdateDelayTime(float delay)
        {
            _attackInterval = ValidateAttackInterval(delay);
            _nextAttackTime = _lastAttackStartTime + _attackInterval;
        }

        public void Tick(float tickValue)
        {
            _elapsedTime += tickValue;
            ChargeMana(tickValue);

            if (_isUsingSkill || _elapsedTime < _nextAttackTime)
                return;

            IActiveSkill executeSkill = GetUsableSkill();
            if (executeSkill == null)
            {
                return;
            }

            _lastAttackStartTime = _elapsedTime;
            _nextAttackTime = _lastAttackStartTime + _attackInterval;

            float animationTimeScale = CalculateAnimationTimeScale(executeSkill);
            _currentSkill = _coroutineRunner.StartCoroutine(
                CoExecuteSkill(executeSkill, animationTimeScale));
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

        private IEnumerator CoExecuteSkill(IActiveSkill skill, float animationTimeScale)
        {
            _isUsingSkill = true;

            skill.Trigger.UseTriggerResource(this);
            yield return skill.Execute(animationTimeScale);

            _isUsingSkill = false;

            _currentSkill = null;
        }

        private void ChargeMana(float manaAmount)
        {
            AddMana(manaAmount * 10 * _manaChargeMultiple);
        }

        public void ConsumeHitCount(int count)
        {
            Debug.Log("Concume HitCount");
            _hitCount = Mathf.Max(0, _hitCount - count);
        }

        public void ConsumeMana(float amount)
        {
            Debug.Log("Concume Mana");
            _mana = Mathf.Clamp(_mana - amount, 0f, MaxMana);
        }

        public void AddHitCount(int count)
        {
            _hitCount += count;
        }

        public void AddMana(float amount)
        {
            _mana = Mathf.Clamp(_mana + amount, 0f, MaxMana);
        }

        public void IncreaseManaAmoutRaio(float ratio)
        {
            _manaChargeMultiple += ratio;
        }

        private static float CalculateMaxMana(IReadOnlyList<IActiveSkill> activeSkills)
        {
            float maxMana = 0f;

            for (int i = 0; i < activeSkills.Count; i++)
            {
                if (activeSkills[i]?.Trigger is ManaTrigger manaTrigger)
                {
                    maxMana = Mathf.Max(maxMana, manaTrigger.RequiredMana);
                }
            }

            return maxMana;
        }

        private float CalculateAnimationTimeScale(IActiveSkill skill)
        {
            float animationDuration = skill.BaseAnimationDuration;
            if (animationDuration <= 0f || animationDuration <= _attackInterval)
            {
                return 1f;
            }

            return _attackInterval / animationDuration;
        }

        private static float ValidateAttackInterval(float attackInterval)
        {
            if (float.IsNaN(attackInterval)
                || float.IsInfinity(attackInterval)
                || attackInterval <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(attackInterval),
                    attackInterval,
                    "Attack interval must be a finite number greater than zero.");
            }

            return attackInterval;
        }

        public void StopRunner()
        {
            if (_currentSkill != null)
                _coroutineRunner.StopCoroutine(_currentSkill);

            _currentSkill = null;
            _isUsingSkill = false;
            ReleasePassive();

            foreach (var activeSkill in _activeSkills)
            {
                activeSkill.Dispose();
            }
        }
    }
}
