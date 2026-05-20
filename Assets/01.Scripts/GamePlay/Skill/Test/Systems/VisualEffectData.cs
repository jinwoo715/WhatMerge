using UnityEngine;

namespace Skill.Data
{
    [CreateAssetMenu(fileName = "VFX", menuName = "Skill/VFX", order = 0)]
    public class VisualEffectData : ScriptableObject
    {
        public string VFXName;
        public EVFXPositionType Position;
        public EVFXMoveDirection MoveDirection;
        public float Speed;
        public float Duration;
    }

    public enum EVFXPositionType
    {
        Target, //Å¸°Ù À§
        OnHero, //¿µ¿õ À§
        Middle, //Å¸°Ù°ú ¿µ¿õÀÇ Áß°£
        ScreenCenter    //È­¸é Áß¾Ó
    }

    public enum EVFXMoveDirection
    {
        None,
        Up,
        Right,
        Down,
        Left,
        ToTarget
    }
}
