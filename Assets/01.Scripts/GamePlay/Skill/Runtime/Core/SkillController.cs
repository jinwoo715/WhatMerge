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
        private readonly struct SkillSelection
        {
            public IActiveSkill ExecuteSkill { get; }
            public IActiveSkill FailedSkill { get; }
            public bool IsValid => ExecuteSkill != null || FailedSkill != null;

            public SkillSelection(IActiveSkill executeSkill, IActiveSkill failedSkill)
            {
                ExecuteSkill = executeSkill;
                FailedSkill = failedSkill;
            }
        }

        private readonly struct IndexedActiveSkill
        {
            public IActiveSkill Skill { get; }
            public int OriginalIndex { get; }

            public IndexedActiveSkill(IActiveSkill skill, int originalIndex)
            {
                Skill = skill;
                OriginalIndex = originalIndex;
            }
        }

        private readonly IActiveSkill _basicAttack;
        private readonly List<IActiveSkill> _activeSkills;
        private readonly List<IPassiveSkill> _passiveSkills;
        private readonly Dictionary<IActiveSkill, bool> _activationRolls = new();
        private readonly MonoBehaviour _coroutineRunner;
        private Coroutine _currentSkill;

        private float _attackInterval;
        private float _elapsedTime;
        private float _lastAttackCycleEndTime;
        private float _nextAttackTime;
        private float _mana;
        private int _hitCount;

        private bool _isUsingSkill = false;

        private float _manaChargeMultiple = 1;

        public float BasicAttackRange => _basicAttack.Target.Range;
        public float CurrentMana => _mana;
        public float MaxMana { get; }
        public float AttackInterval => _attackInterval;
        public float NextAttackTime => _nextAttackTime;

        public SkillController(List<IActiveSkill> activeSkills, List<IPassiveSkill> passiveSkills, MonoBehaviour coroutineRunner, float delay)
        {
            _basicAttack = FindBasicAttack(activeSkills);
            _activeSkills = SortActiveSkills(activeSkills);
            _passiveSkills = passiveSkills ?? throw new System.ArgumentNullException(nameof(passiveSkills));
            _coroutineRunner = coroutineRunner ?? throw new System.ArgumentNullException(nameof(coroutineRunner));
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
            _nextAttackTime = _lastAttackCycleEndTime + _attackInterval;
        }

        public void Tick(float tickValue)
        {
            _elapsedTime += tickValue;
            ChargeMana(tickValue);

            if (_isUsingSkill)
                return;

            float earliestSkillStartTime = _nextAttackTime - CalculateMaxScaledAnimationDuration();
            if (_elapsedTime < earliestSkillStartTime)
                return;

            SkillSelection selection = GetSkillSelection();
            if (!selection.IsValid)
                return;

            IActiveSkill executeSkill = selection.ExecuteSkill;
            float animationTimeScale = executeSkill != null ? CalculateAnimationTimeScale(executeSkill) : 1f;
            float scaledAnimationDuration = executeSkill != null ? executeSkill.BaseAnimationDuration * animationTimeScale : 0f;
            float skillStartTime = _nextAttackTime - scaledAnimationDuration;
            if (_elapsedTime < skillStartTime)
                return;

            float scheduleDelay = Mathf.Max(0f, _elapsedTime - skillStartTime);
            float chargeTime = executeSkill?.ChargeTime ?? 0f;
            _lastAttackCycleEndTime = _nextAttackTime + scheduleDelay + chargeTime;
            _nextAttackTime = _lastAttackCycleEndTime + _attackInterval;
            _activationRolls.Clear();

            _currentSkill = _coroutineRunner.StartCoroutine(
                CoExecuteSkill(selection, animationTimeScale));
        }

        private SkillSelection GetSkillSelection()
        {
            SkillTriggerContext context = new SkillTriggerContext(_hitCount, _mana);
            IActiveSkill selectedSkill = null;

            int skillCount = _activeSkills.Count;
            for (int i = 0; i < skillCount; i++)
            {
                IActiveSkill skill = _activeSkills[i];
                if (skill.IsUsable(context))
                {
                    selectedSkill = skill;
                    break;
                }
            }

            if (selectedSkill == null)
                return default;

            if (GetOrRollActivation(selectedSkill))
            {
                return new SkillSelection(selectedSkill, null);
            }

            IActiveSkill fallback = null;
            if (!ReferenceEquals(selectedSkill, _basicAttack)
                && _basicAttack.IsUsable(context))
            {
                fallback = _basicAttack;
            }

            return new SkillSelection(fallback, selectedSkill);
        }

        private bool GetOrRollActivation(IActiveSkill skill)
        {
            if (_activationRolls.TryGetValue(skill, out bool result))
            {
                return result;
            }

            result = skill.RollActivation();
            _activationRolls.Add(skill, result);
            return result;
        }

        private IEnumerator CoExecuteSkill(SkillSelection selection, float animationTimeScale)
        {
            _isUsingSkill = true;

            selection.FailedSkill?.Trigger.UseTriggerResourceOnFailure(this);

            if (selection.ExecuteSkill != null)
            {
                selection.ExecuteSkill.Trigger.UseTriggerResource(this);
                yield return selection.ExecuteSkill.Execute(animationTimeScale);
            }

            _isUsingSkill = false;

            _currentSkill = null;
        }

        private void ChargeMana(float manaAmount)
        {
            AddMana(manaAmount * 10 * _manaChargeMultiple);
        }

        public void ConsumeHitCount(int count)
        {
            _hitCount = Mathf.Max(0, _hitCount - count);
        }

        public void ConsumeMana(float amount)
        {
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

        private float CalculateMaxScaledAnimationDuration()
        {
            float maxDuration = 0f;

            for (int i = 0; i < _activeSkills.Count; i++)
            {
                IActiveSkill skill = _activeSkills[i];
                float scaledDuration = skill.BaseAnimationDuration * CalculateAnimationTimeScale(skill);
                maxDuration = Mathf.Max(maxDuration, scaledDuration);
            }

            return maxDuration;
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

        private static IActiveSkill FindBasicAttack(IReadOnlyList<IActiveSkill> activeSkills)
        {
            if (activeSkills == null)
                throw new System.ArgumentNullException(nameof(activeSkills));

            IActiveSkill basicAttack = null;

            for (int i = 0; i < activeSkills.Count; i++)
            {
                IActiveSkill skill = activeSkills[i]
                    ?? throw new System.InvalidOperationException(
                        $"Active skill at index {i} is null.");

                if (skill.Priority != 0)
                    continue;

                if (basicAttack != null)
                {
                    throw new System.InvalidOperationException(
                        "Active skill list has more than one basic attack.");
                }

                basicAttack = skill;
            }

            return basicAttack
                ?? throw new System.InvalidOperationException(
                    "Active skill list has no basic attack.");
        }

        private static List<IActiveSkill> SortActiveSkills(IReadOnlyList<IActiveSkill> activeSkills)
        {
            List<IndexedActiveSkill> indexedSkills = new(activeSkills.Count);

            for (int i = 0; i < activeSkills.Count; i++)
            {
                indexedSkills.Add(new IndexedActiveSkill(activeSkills[i], i));
            }

            indexedSkills.Sort((left, right) =>
            {
                int priorityComparison = right.Skill.Priority.CompareTo(left.Skill.Priority);
                return priorityComparison != 0
                    ? priorityComparison
                    : right.OriginalIndex.CompareTo(left.OriginalIndex);
            });

            List<IActiveSkill> sortedSkills = new(indexedSkills.Count);
            for (int i = 0; i < indexedSkills.Count; i++)
            {
                sortedSkills.Add(indexedSkills[i].Skill);
            }

            return sortedSkills;
        }

        public void StopRunner()
        {
            if (_currentSkill != null)
                _coroutineRunner.StopCoroutine(_currentSkill);

            _currentSkill = null;
            _isUsingSkill = false;
            _activationRolls.Clear();
            ReleasePassive();

            foreach (var activeSkill in _activeSkills)
            {
                activeSkill.Dispose();
            }
        }
    }
}
