using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroBagViewer : MonoBehaviour
{
    [SerializeField] private HeroBagSlot[] _slots;
    [SerializeField] private Button _bagButton;
    [SerializeField] private TMP_Text _bagSpaceCountText;
    [SerializeField] private GameObject _bagViewObject;

    private bool _isBagOpened = false;
    public event Action<int> OnClickTakeOut;

    private void Start()
    {
        Init();
    }
    private void Init()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            int index = i;
            _slots[i].Init(index);
            _slots[i].OnClickTakeOut += OnTakeOut;
        }

        _bagButton.onClick.AddListener(OnClickBagButton);
    }
    private void OnClickBagButton()
    {
        _isBagOpened = !_isBagOpened;
        _bagViewObject.SetActive(_isBagOpened);
    }

    public void OnTakeOut(int index)
    {
        OnClickTakeOut?.Invoke(index);
    }
    public void Clear(int index)
    {
        _slots[index].Clear();
    }
    public void UpdateSpaceText(string spaceText)
    {
        _bagSpaceCountText.text = spaceText;
    }
    public void SetHero(int index, Sprite sprite)
    {
        _slots[index].SetImage(sprite);
    }
    
}
