using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ITimeView
{
    event Action OnClickChangeButton;
    void SetTime(int time);
}

public class TimeViewer : MonoBehaviour, ITimeView
{
    [SerializeField] private Button _speedChangeButton;
    [SerializeField] private Sprite[] _speedSprites;

    public event Action OnClickChangeButton;

    public void Awake()
    {
        _speedChangeButton.onClick.AddListener(() => OnClickChangeButton?.Invoke());
    }

    public void SetTime(int time)
    {
        _speedChangeButton.image.sprite = _speedSprites[time-1];
    }
}
