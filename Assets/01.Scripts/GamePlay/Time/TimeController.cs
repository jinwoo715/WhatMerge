using System;
using UnityEngine;

public interface ITimeService
{
    void SpeedUp();
    void SetPause(bool isPause);
    event Action<int> OnChangeGameSpeed; 
}

public interface IFatalStopService
{
    bool IsFatalStopped { get; }
    void FatalStop(Exception exception, string context);
}

public class TimeController : ITimeService, IFatalStopService, IDisposable
{
    private readonly int[] _gameTimes = { 1, 2, 3 };

    private int _speedIndex;
    private int _gameSpeed = 1;

    private bool _isPause;

    public bool IsFatalStopped { get; private set; }
    public Exception FatalException { get; private set; }
    public string FatalContext { get; private set; }

    public event Action<int> OnChangeGameSpeed;

    public void SetPause(bool pause)
    {
        _isPause = pause;
        ApplyTimeScale();
        OnChangeGameSpeed?.Invoke(_gameSpeed);
    }

    public void SpeedUp()
    {
        if (IsFatalStopped)
            return;

        _speedIndex++;
        
        _speedIndex = _speedIndex % _gameTimes.Length;

        _gameSpeed = _gameTimes[_speedIndex];
        ApplyTimeScale();
        OnChangeGameSpeed?.Invoke(_gameSpeed);
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = _isPause || IsFatalStopped ? 0 : _gameSpeed;
    }

    public void FatalStop(Exception exception, string context)
    {
        if (IsFatalStopped)
            return;

        FatalException = exception ?? new ArgumentNullException(nameof(exception));
        FatalContext = string.IsNullOrWhiteSpace(context) ? "Unknown fatal context" : context;
        IsFatalStopped = true;
        ApplyTimeScale();
        Debug.LogException(new InvalidOperationException(FatalContext, FatalException));
    }

    public void Dispose()
    {
        OnChangeGameSpeed = null;
        Time.timeScale = 1f;
    }
}
