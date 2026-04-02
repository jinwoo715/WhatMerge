using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureBase : MonoBehaviour
{
    public void TurnLeft()
    {
        this.transform.localScale = new Vector3(1, 1, 1);
    }
    public void TurnRight()
    {
        this.transform.localScale = new Vector3(-1, 1, 1);
    }
}
