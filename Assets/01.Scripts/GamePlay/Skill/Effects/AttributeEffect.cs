using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "AttributeEffect", menuName = "Skill/Effect/AttributeEffect", order = 0)]
    public class StatusEffectBase : EffectBase
    {
        public float Duration;
    }

    //속성
    public class AttributeEffect : StatusEffectBase
    {
        public EAttributeType Attribute;
    }


    //군중제어
    public class CrowdControllerEffect : StatusEffectBase { }
    public class SlowEffect : StatusEffectBase
    {
        [Range(0,1)]
        public float SlowRatio;
    }
    public class StunEffect : StatusEffectBase
    {

    }
    public class Knockback : StatusEffectBase
    {

    }

    //도트
    public class DotEffectBase : StatusEffectBase
    {
        public float IntervalTime;
    }
    public class FixDotEffect : DotEffectBase
    {
        public int DotDamage;
    }
    public class CurrentHPRatioDotEffect : DotEffectBase
    {
        public float Ratio;
    }
    public class MaxHPRatioDotEffect : DotEffectBase
    {
        public float Ratio;
    }

    //방어력
    public class StatDebuffEffectBase : StatusEffectBase { }
    public class DecreseFixAmour : StatDebuffEffectBase
    {
        public int DecreaseValue;
    }
    public class DecresseRatioAmour : StatDebuffEffectBase
    {
        [Range(0,1)]
        public float DecreaseValue;
    }
}

