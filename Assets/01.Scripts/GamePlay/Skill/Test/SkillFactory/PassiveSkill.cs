using Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public interface IPassiveSkill : ISkill
    {
        void Apply();
    }

    //TODO 패시브 스킬
    public abstract class PassiveSkill : IPassiveSkill, IDisposable
    {
        public int UID { get; private set; }
        public abstract void Apply();
        public abstract void ModifyParam(int paramIndex, float value);
        public abstract void Dispose();
        public void SetUID(int uid) { UID = uid; }
        public void ModifyChance(int effectIndex, float value)
        {
            throw new NotImplementedException();
        }
        public void AddEffect(EffectEntry effect)
        {
            throw new NotImplementedException();
        }
    }
    public class SelfPassive : PassiveSkill
    {
        public IStatModifier _statModifier;
        public List<EffectBase> _effects;
        public SelfPassive(IStatModifier statModifier, List<EffectBase> effects)
        {
            _statModifier = statModifier;
            _effects = effects;
        }

        public override void Apply()
        {
            foreach (var effect in _effects)
            {
                if(effect is BuffEffect buff)
                {
                    _statModifier.ModifyStat(buff.BuffType, buff.IncreaseRatio);
                }
            }
        }

        public override void Dispose()
        {
        }

        public override void ModifyParam(int paramIndex, float value)
        {

        }
    }
}
