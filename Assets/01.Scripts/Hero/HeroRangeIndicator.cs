using UnityEngine;
using Stat;

namespace WhatMerge.Heros
{
    public class HeroRangeIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject _heroRangeObject;
        
        public void ShowHeroRange(Hero hero)
        {
            float range = hero.BasicAttackRange*2;

            _heroRangeObject.transform.localScale = Vector3.one * range;
            _heroRangeObject.transform.position = hero.transform.position;
            _heroRangeObject.SetActive(true);
        }
        public void HideHeroRange() 
        {
            _heroRangeObject.SetActive(false);       
        }
    }
}