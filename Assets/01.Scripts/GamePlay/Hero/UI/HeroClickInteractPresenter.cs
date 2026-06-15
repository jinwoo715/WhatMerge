using Entity;
using Heros;
using UnityEngine;
using UnityEngine.UI;

public class HeroClickInteractPresenter : MonoBehaviour
{
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Button _sellButton;
    [SerializeField] private Button _insertButton;

    private Hero _selectHero;

    private IHeroBagService _heroBagService;
    private IFieldHeroService _fieldHeroService;
    
    public void Init(IHeroBagService heroBagService, IFieldHeroService fieldHeroService)
    {
        _heroBagService = heroBagService;
        _fieldHeroService = fieldHeroService;

        _heroBagService.OnInputHero += (_,_) => HideInteractUI();

        _sellButton.onClick.AddListener(ClickSellButton);
        _insertButton.onClick.AddListener(ClickInsertButton);
    }

    private void ClickSellButton()
    {
        _fieldHeroService.SellHero(_selectHero);
    }
    private void ClickInsertButton()
    {
        _fieldHeroService.ClearHero(_selectHero);
        _heroBagService.PutInTheBag(_selectHero);
    }

    public void ShowInteractUI(Hero hero)
    {
        _selectHero = hero;

        _sellButton.gameObject.SetActive(true);

        if (_heroBagService.IsUsableBag)
            _insertButton.gameObject.SetActive(true);

        _buttonParent.position = Camera.main.WorldToScreenPoint(_selectHero.transform.position);
    }
    public void HideInteractUI()
    {
        _selectHero = null;
        _sellButton.gameObject.SetActive(false);
        _insertButton.gameObject.SetActive(false);
    }
}
