using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingBallTarget : MonoBehaviour
{
    public delegate void MovingBallTargetAction();
    public event MovingBallTargetAction OnCollidedWithTail;

    private void OnCollisionEnter(Collision collision)
    {
        if (OnCollidedWithTail != null) OnCollidedWithTail();
    }
}
