using System;

namespace WhatMerge.Heros
{
    public class HeroBag : IHeroBagService
    {
        public bool IsUsableBag => CurrentUsedBagItem < TotalBagSpace;
        public int TotalBagSpace => _totalUsableBagSpace;
        public int CurrentUsedBagItem => _currentUsedBagSpace;

        private int _totalUsableBagSpace;
        private int _currentUsedBagSpace;
        private int _currentIndex;

        private HeroBagSlotData[] _bagValue = new HeroBagSlotData[5];

        public event Action<int, HeroBagSlotData> OnInputHero;
        public event Action<int, int> OnChangedUseableSpace;
        public event Action<int> OnTakeOutHero;

        private IHeroSummonService _heroSummonService;

        public void Init(int useableBagSpace, IHeroSummonService heroSummonService)
        {
            _heroSummonService = heroSummonService;
            _totalUsableBagSpace = useableBagSpace;
            _currentUsedBagSpace = 0;
            _currentIndex = 0;

            for (int i = 0; i < 5; i++)
            {
                _bagValue[i] = new HeroBagSlotData();
            }
        }

        public void PutInTheBag(Hero hero)
        {
            _bagValue[_currentIndex].Init(hero.UID, hero.EvolutionLevel, hero.Name);

            _currentUsedBagSpace++;
            OnChangedUseableSpace?.Invoke(TotalBagSpace, CurrentUsedBagItem);

            OnInputHero?.Invoke(_currentIndex, _bagValue[_currentIndex]);
            UpdateCurrentIndex();
        }

        public void TakeOutOfTheBag(int index)
        {
            var data = _bagValue[index];

            if (data.IsUseable) return;

            if (_heroSummonService.TrySpawnHero(data.UID, data.Evolution))
            {
                _currentUsedBagSpace--;

                _bagValue[index].Clear();

                OnChangedUseableSpace?.Invoke(TotalBagSpace, CurrentUsedBagItem);

                OnTakeOutHero?.Invoke(index);

                UpdateCurrentIndex();
            }
        }

        private void UpdateCurrentIndex()
        {
            for (int i = 0; i < _totalUsableBagSpace; i++)
            {
                if (_bagValue[i].IsUseable)
                {
                    _currentIndex = i;
                    return;
                }
            }
        }
    }
}
