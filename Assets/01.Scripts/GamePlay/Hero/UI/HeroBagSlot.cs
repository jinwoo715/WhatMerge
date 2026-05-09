using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeroBagSlot : MonoBehaviour
{
    [SerializeField] private Image _profileImage;
    [SerializeField] private Button _takeOutButton;
    private int _index;

    public event Action<int> OnClickTakeOut;

    private void Awake()
    {
        _takeOutButton.onClick.AddListener(ClickEvent);
    }

    public void Init(int index)
    {
        _index = index;
    }

    public void SetImage(Sprite sprite)
    {
        _profileImage.sprite = sprite;
        _profileImage.gameObject.SetActive(true);
    }

    public void Clear()
    {
        _profileImage.sprite = null;
        _profileImage.gameObject.SetActive(false);
    }

    public void ClickEvent()
    {
        OnClickTakeOut?.Invoke(_index);
    }
}
