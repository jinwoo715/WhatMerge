using Combat;
using GamePlay.Entity;
using Stat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Skill 
{
    public interface IObjectGetter
    {
        void GetObject();
    }

    public class SkillContext : ISkillContext
    {
        private Dictionary<Type, object> _services = new();

        public void Register<T>(T service) where T : class
        {
            _services.Add(typeof(T), service);
            Debug.Log(typeof(T));
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if(_services.TryGetValue(typeof(T), out var obj))
            {
                service = obj as T;
                return true;
            }
            else
            {
                service = default;
                return false;
            }
        }
    }

    public interface ISkillContext
    {
        void Register<T>(T service) where T : class;
        bool TryGet<T>(out T service) where T : class;
    }
}
