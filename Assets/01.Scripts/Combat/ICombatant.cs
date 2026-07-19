using System;
using UnityEngine;

namespace WhatMerge.Combat
{

    public interface IElement
    {
        bool IsHasElement(ElementType type);
        void GetElement(ElementType type);
        void ReleaseElement(ElementType type);
        void Clear();
    }

    public class CombatantElement : IElement
    {
        private ElementType _type = ElementType.None;
        public ElementType Type => _type;

        public void Clear()
        {
            _type = ElementType.None;
        }

        public void GetElement(ElementType type)
        {
            _type |= type;
        }

        public bool IsHasElement(ElementType type)
        {
            return (_type & type) != 0;
        }

        public void ReleaseElement(ElementType type)
        {
            _type = _type & ~type;
        }
    }

    public interface ICombatant
    {
        event Action<ICombatant> OnActiveOff;
        IElement Element { get; }
        bool IsActive { get; }
        Vector3 Position { get; }
    }
}
