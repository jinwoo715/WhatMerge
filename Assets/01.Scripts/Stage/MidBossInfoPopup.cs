using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MidBossInfoPopup : MonoBehaviour
{
    [SerializeField] private Image _bossImage;

    [SerializeField] private TMP_Text _bossName;
    [SerializeField] private TMP_Text _bossDescription;

    [SerializeField] private Image _rewardImage;
    [SerializeField] private TMP_Text _rewardCount;

    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _tryButton;

    public event Action OnClickTryButton;
    public event Action OnCloseButton;

    private void Awake()
    {
        _tryButton.onClick.AddListener(() => OnClickTryButton?.Invoke());
        _closeButton.onClick.AddListener(() => OnCloseButton?.Invoke());
    }

    public void SetData(Sprite bossSprite, string name, string description, string rewardCount)
    {
        _bossImage.sprite = bossSprite;
        _bossName.text = name;
        _bossDescription.text = description;
        _rewardCount.text = rewardCount;
    }
}
