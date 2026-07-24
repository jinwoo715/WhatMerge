using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhatMerge.Combat 
{
    public interface IDotService
    {
        void ApplyDotEffect(DotData dotData);
    }

    public class DotProcessBundle
    {
        private Dictionary<(int SkillUid, int OwnerSpawnIndex), DotProcess> _dotProcesses = new();
        public int Dots => _dotProcesses.Count;
        public List<DotProcess> AllDatas => _dotProcesses.Values.ToList();
        public bool TryGetProcess(DotData key, out DotProcess value)
        {
            if(_dotProcesses.TryGetValue((key.SkillUid, key.OwnerSpawnIndex), out var proccess))
            {
                value = proccess;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
        public void AddDotProcess(DotData key, DotProcess value)
        {
            _dotProcesses.Add((key.SkillUid, key.OwnerSpawnIndex), value);
        }

        public bool IsExistDot(DotData data)
        {
            return _dotProcesses.ContainsKey((data.SkillUid, data.OwnerSpawnIndex));
        }

        public void RemoveDotProcess(DotData key)
        {
            if (_dotProcesses.ContainsKey((key.SkillUid, key.OwnerSpawnIndex)))
            {
                _dotProcesses.Remove((key.SkillUid, key.OwnerSpawnIndex));
            }
        }
    }

    public class DotProcess
    {
        public DotData _dotData;
        public Coroutine _dotCoroutine;

        public DotProcess(DotData data, Coroutine cor)
        {
            _dotData = data;
            _dotCoroutine = cor;
        }
    } 

    public class DotData
    {
        public int SkillUid;
        public int OwnerSpawnIndex;
        public float _duration;
        public float value;
        public DotDamageType dotDamageType;
        public float interval;
        public DamageContext Context;

        public DotData(float duration, DotEffect dotEffect, DamageContext context)
        {
            SkillUid = context.SkillUid;
            OwnerSpawnIndex = context.OwnerSpawnIndex;
            _duration = duration;

            value = dotEffect.Value;
            dotDamageType = dotEffect.ApplyType;
            interval = dotEffect.IntervalTime;

            Context = context;
        }
    }


    public class DotEffectManager : MonoBehaviour, IDotService
    {
        private Dictionary<ICombatant, DotProcessBundle> _dots = new();
        private IDamageApplier _damageApplier;

        public void Init(IDamageApplier damageApplier)
        {
            _damageApplier = damageApplier;
        }

        public void ApplyDotEffect(DotData dotData)
        {
            if(dotData.interval <=0 || dotData._duration <= 0)
            {
                Debug.LogError($"Dot Effect Invalid Time");
                return;
            }

            ICombatant target = dotData.Context.Target;

            if (!IsAppliedMaxDotStack(target))
            {
                if(!_dots.TryGetValue(target, out var datas))
                {
                    _dots.Add(target, new DotProcessBundle());

                    target.OnActiveOff += ReleaseCombatAllDot;
                }

                if (_dots[target].IsExistDot(dotData))
                    return;

                Coroutine dotCoroutine = StartCoroutine(CoDot(dotData));
                _dots[target].AddDotProcess(dotData, new DotProcess(dotData, dotCoroutine));
            }
        }

        private IEnumerator CoDot(DotData dotData)
        {
            float totalTimer = 0;
            float timer = 0;

            ICombatant target = dotData.Context.Target;

            while(totalTimer < dotData._duration)
            {
                yield return null;

                totalTimer += Time.deltaTime;
                timer += Time.deltaTime;

                while(timer >= dotData.interval)
                {
                    ApplyDot(dotData.Context.Target, GetDotDamage(dotData.dotDamageType, target, dotData.value, dotData.Context.AttackPayload.AttackDamage));
                    timer -= dotData.interval ;
                }
            }

            ReleaseDotEffect(target, dotData);
        }


        private int GetDotDamage(DotDamageType type, ICombatant target, float value, int damage)
        {
            int returnValue = 0;

            switch (type)
            {
                case DotDamageType.Fixed:
                    returnValue = (int)value;
                    break;
                case DotDamageType.DamageRatio:
                    returnValue = Mathf.RoundToInt(damage * value);
                    break;
                case DotDamageType.CurrentHPRatio:

                    if(target is IDamageable damageable)
                    {
                        returnValue = (int)(damageable.CurrentHP * value);
                    }

                    break;
                case DotDamageType.MaxHPRatio:

                    if (target is IDamageable damageable2)
                    {
                        returnValue = (int)(damageable2.MaxHP * value);
                    }

                    break;
                default:
                    return 0;
            }

            return returnValue;
        }
        private void ApplyDot(ICombatant key, int damage)
        {
            if(key is IDamageable damageable)
            {
                _damageApplier.TryApply(damageable, damage);
            }
        }
        private bool IsAppliedMaxDotStack(ICombatant target)
        {
            if(_dots.TryGetValue(target, out var value))
            {
                return value.Dots >= 5;
            }

            return false;
        }
        private void ReleaseCombatAllDot(ICombatant target)
        {
            if (_dots.ContainsKey(target)) 
            {
                var data = _dots[target];

                foreach (var dot in data.AllDatas)
                {
                    ReleaseDotEffect(target, dot._dotData);
                }
            }
        }
        private void ReleaseDotEffect(ICombatant key, DotData dotData)
        {
            if(_dots.TryGetValue(key, out var bundle))
            {
                if(bundle.TryGetProcess(dotData, out DotProcess process))
                {
                    StopCoroutine(process._dotCoroutine);

                    bundle.RemoveDotProcess(dotData);

                    if (bundle.Dots == 0)
                    {
                        _dots.Remove(key);
                        key.OnActiveOff -= ReleaseCombatAllDot;
                    }
                }
            }
        }
    }
}
