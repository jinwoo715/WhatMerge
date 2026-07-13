using Skill.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skill
{
    public class RuntimeEffectSlot
    {
        public readonly List<EffectBase> RuntimeList;
        public readonly int Index;

        public readonly EffectBase Original;

        public RuntimeEffectSlot(List<EffectBase> owner, EffectBase origin, int index)
        {
            RuntimeList = owner;
            Index = index;
            Original = origin;
        }

        public EffectBase GetRuntimeEffect()
        {
            EffectBase current = RuntimeList[Index];

            //다르면 RuntimeList[Index]는 이미 Clone으로 만들어진 객체라서 바로 사용 가능
            if (current != Original)
                return current;

            //같으면 현재 원본
            RuntimeList[Index] = Object.Instantiate(Original);
            return RuntimeList[Index];
        }
    }
}