using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Data
{
    public class SkillEnhancer : SkillBaseData
    {
        [Header("강화될 스킬")]
        public SkillBaseData TargetSkill;

        public List<EffectEntry> EffectEntries;

        public int SelectEffectIndex;

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private SkillBaseData _cachedTargetSkill;

        private void OnValidate()
        {
            if (TargetSkill == _cachedTargetSkill)
                return;

            _cachedTargetSkill = TargetSkill;

            CopyEffectEntriesFromTargetSkill();
        }

        private void CopyEffectEntriesFromTargetSkill()
        {
            EffectEntries.Clear();

            if (TargetSkill == null)
                return;

            if(TargetSkill is ActiveSkillData skill)
            {
                foreach (EffectEntry entry in skill.Execution.Effects)
                {
                    if (entry == null)
                        continue;

                    EffectEntries.Add(new EffectEntry
                    {
                        Effect = entry.Effect,
                        Chance = entry.Chance
                    });
                }
            }
        }
#endif
    }



    //스킬에 추가 이펙트 넣기
    //이펙트 값 강화하기

    //액티브 스킬 강화 -> 액티브 스킬 안의 특정 이펙트 강화
    //패시브 스킬 강화 -> 패시브 스킬 안의 특정 이펙트 강화
}