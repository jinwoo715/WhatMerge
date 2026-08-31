using System;
using UnityEngine;

public interface ITimeService
{
    void SpeedUp();
    void SetPause(bool isPause);
    event Action<int> OnChangeGameSpeed; 
}

public class TimeController : ITimeService
{
    private readonly int[] _gameTimes = { 1, 2, 3 };

    private int _speedIndex;
    private int _gameSpeed = 1;

    private bool _isPause;

    public event Action<int> OnChangeGameSpeed;

    public void SetPause(bool pause)
    {
        _isPause = pause;
        ApplyTimeScale();
        OnChangeGameSpeed?.Invoke(_gameSpeed);
    }

    public void SpeedUp()
    {
        _speedIndex++;
        
        _speedIndex = _speedIndex % _gameTimes.Length;

        _gameSpeed = _gameTimes[_speedIndex];
        ApplyTimeScale();
        OnChangeGameSpeed?.Invoke(_gameSpeed);
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = _isPause ? 0 : _gameSpeed;
    }
}
