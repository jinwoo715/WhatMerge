using UnityEngine;
using Stat;

namespace WhatMerge.Heros
{
    public class HeroRangeIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject _heroRangeObject;
        private Hero _selectedHero;
        
        public void ShowHeroRange(Hero hero)
        {
            _selectedHero = hero;
            float range = hero.BasicAttackRange*2;

            _heroRangeObject.transform.localScale = Vector3.one * range;
            _heroRangeObject.transform.position = hero.transform.position;
            _heroRangeObject.SetActive(true);
        }
        public void HideHeroRange() 
        {
            _selectedHero = null;
            _heroRangeObject.SetActive(false);       
        }

        public void HideIfSelected(Hero hero)
        {
            if (ReferenceEquals(_selectedHero, hero))
                HideHeroRange();
        }
    }
}
