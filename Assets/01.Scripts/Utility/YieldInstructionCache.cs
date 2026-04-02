using System.Collections.Generic;
using UnityEngine;

public static class YieldInstructionCache
{
    public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
    public static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();
    private static readonly Dictionary<float, WaitForSeconds> waitForSeconds = new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds WaitForSeconds(float _seconds)
    {
        WaitForSeconds wfs;
        if (!waitForSeconds.TryGetValue(_seconds, out wfs))
            waitForSeconds.Add(_seconds, wfs = new WaitForSeconds(_seconds));
        return wfs;
    }
}