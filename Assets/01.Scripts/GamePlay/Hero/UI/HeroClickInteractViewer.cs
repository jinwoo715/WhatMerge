using Entity;
using Heros;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeroClickInteractViewer : MonoBehaviour
{
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Button _sellButton;
    [SerializeField] private Button _insertButton;

    public event Action OnClickSellButton;
    public event Action OnClickInsertButton;

    private IHeroBagService _heroBagService;
    public void Init(IHeroBagService heroBagService)
    {
        _heroBagService = heroBagService;

        _sellButton.onClick.AddListener(ClickSellButton);
        _insertButton.onClick.AddListener(ClickInsertButton);
    }

    private void ClickSellButton()
    {
        OnClickSellButton?.Invoke();
    }
    private void ClickInsertButton()
    {
        OnClickInsertButton?.Invoke();
    }

    public void ShowInteractUI(Hero hero)
    {
        _sellButton.gameObject.SetActive(true);

        if (_heroBagService.IsUsableBag)
            _insertButton.gameObject.SetActive(true);

        _buttonParent.position = Camera.main.WorldToScreenPoint(hero.transform.position);
    }
}
