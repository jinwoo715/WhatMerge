using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;

namespace WhatMerge.Heros
{
    public class HeroBagPresenter
    {
        private HeroBagViewer _bagViewer;
        private IHeroBagService _bagService;
        private IResourcesReader _heroAtlasReader;
        public void Init(IHeroBagService heroBagService, HeroBagViewer bagViewer, IResourcesReader resourcesReader)
        {
            _bagService = heroBagService;
            _bagViewer = bagViewer;
            _heroAtlasReader = resourcesReader;

            _bagViewer.OnClickTakeOut += _bagService.TakeOutOfTheBag;

            _bagService.OnInputHero += SetBag;
            _bagService.OnTakeOutHero += _bagViewer.Clear;
            _bagService.OnChangedUseableSpace += UpdateSpaceState;
        }

        public void SetBag(int index, HeroBagSlotData data)
        {
            SpriteAtlas heroAtlas = _heroAtlasReader.GetAtlas(data.SpriteName);
            string spriteName = $"{data.SpriteName}_{data.Evolution + 1}_Idle";
            Sprite image = heroAtlas.GetSprite(spriteName);

            _bagViewer.SetHero(index, image);
        }

        private void UpdateSpaceState(int total, int current)
        {
            _bagViewer.UpdateSpaceText($"{current} / {total}");
        }
    }
}
