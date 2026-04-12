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
    private int[] _gameTimes = { 1, 2 };

    private int _speedIndex;
    private int _gameSpeed;

    private bool _isPause;

    public event Action<int> OnChangeGameSpeed;

    public void SetPause(bool pause)
    {
        _isPause = pause;

        int speed = _isPause ? 0 : _gameSpeed;

        SetSpeed(speed);
    }

    public void SpeedUp()
    {
        _speedIndex++;
        
        _speedIndex = _speedIndex % _gameTimes.Length;

        _gameSpeed = _gameTimes[_speedIndex];

        SetSpeed(_gameSpeed);
    }

    private void SetSpeed(int speed)
    {
        Time.timeScale = speed;
        OnChangeGameSpeed?.Invoke(speed);
    }
}
