using Entity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stat;

namespace Heros
{
    public class HeroRangeViewer : MonoBehaviour
    {
        [SerializeField] private GameObject _heroRangeObject;
        
        public void ShowHeroRange(Hero hero)
        {
            float range = hero.StatReadOnly.GetStat(EHeroStat.AttackRange);

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