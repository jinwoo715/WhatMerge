using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimePresenter
{
    public void Init(ITimeService service, ITimeView view)
    {
        service.OnChangeGameSpeed += view.SetTime;
        view.OnClickChangeButton += service.SpeedUp;
        service.SetPause(false);
    }
}
