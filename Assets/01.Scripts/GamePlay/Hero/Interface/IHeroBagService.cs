using Entity;
using System;

namespace Heros
{
    public interface IHeroBagService
    {
        bool IsUsableBag { get; }
        int TotalBagSpace { get; }
        int CurrentUsedBagItem { get; }

        event Action<int, HeroBagSlotData> OnInputHero;
        event Action<int> OnTakeOutHero;
        event Action<int, int> OnChangedUseableSpace;
        void PutInTheBag(Hero hero);
        void TakeOutOfTheBag(int index);
    }
}