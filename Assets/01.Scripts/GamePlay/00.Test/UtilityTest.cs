using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityTest : MonoBehaviour
{
    public int BaseValue;
    public int Level;
    public float Growth;
    public float TierBonus;

    [ContextMenu("ATK Damage")]
    public void ATKTest()
    {
        Debug.Log(StatCalculator.ATK(Level, BaseValue, Growth, TierBonus));
    }
}
